using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer; // Thêm using
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens; // Thêm using
using QuantumBands.API.Middleware;
using QuantumBands.API.Workers;
using QuantumBands.Application.Features.Admin.ExchangeMonitor.Queries;
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
using QuantumBands.Application.Features.Admin.Users.Commands.UpdateUserRole;
using QuantumBands.Application.Features.Admin.Users.Commands.UpdateUserStatus;
using QuantumBands.Application.Features.Admin.Users.Queries;
using QuantumBands.Application.Features.Authentication;
using QuantumBands.Application.Features.Authentication.Commands.ForgotPassword; // Thêm using
using QuantumBands.Application.Features.Authentication.Commands.Login; // For LoginRequestValidator
using QuantumBands.Application.Features.Authentication.Commands.RefreshToken;
using QuantumBands.Application.Features.Authentication.Commands.RegisterUser;
using QuantumBands.Application.Features.Authentication.Commands.ResendVerificationEmail; // For RegisterUserCommandValidator
using QuantumBands.Application.Features.Authentication.Commands.ResetPassword;
using QuantumBands.Application.Features.Authentication.Commands.VerifyEmail;
using QuantumBands.Application.Features.EAIntegration.Commands.PushClosedTrades;
using QuantumBands.Application.Features.EAIntegration.Commands.PushLiveData;
using QuantumBands.Application.Features.Exchange.Queries;
using QuantumBands.Application.Features.Roles.Commands.CreateRole; // Namespace của validator
using QuantumBands.Application.Features.TradingAccounts.Queries;
using QuantumBands.Application.Features.Users.Commands.ChangePassword;
using QuantumBands.Application.Features.Users.Commands.Disable2FA;
using QuantumBands.Application.Features.Users.Commands.Enable2FA;
using QuantumBands.Application.Features.Users.Commands.UpdateProfile;
using QuantumBands.Application.Features.Users.Commands.Verify2FA; // Thêm using cho validator
using QuantumBands.Application.Features.Admin.SystemSettings.Commands.CreateSystemSetting;
using QuantumBands.Application.Features.Admin.SystemSettings.Commands.UpdateSystemSetting;
using QuantumBands.Application.Features.Admin.SystemSettings.Commands.DeleteSystemSetting;
using QuantumBands.Application.Features.Admin.SystemSettings.Queries;
using QuantumBands.Application.Features.Wallets.Commands.AdminActions;
using QuantumBands.Application.Features.Wallets.Commands.AdminDeposit;
using QuantumBands.Application.Features.Wallets.Commands.BankDeposit; // Cho validator
using QuantumBands.Application.Features.Wallets.Commands.CreateWithdrawal;
using QuantumBands.Application.Features.Wallets.Commands.InternalTransfer;
using QuantumBands.Application.Features.Wallets.Queries.GetTransactions;
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Interfaces.Repositories;
using QuantumBands.Application.Services; // For AuthService
using QuantumBands.Infrastructure.Authentication; // For JwtSettings, JwtTokenGenerator
using QuantumBands.Infrastructure.Caching;
using QuantumBands.Infrastructure.Email; // For EmailSettings, EmailService
using QuantumBands.Infrastructure.Persistence.DataContext;
using QuantumBands.Infrastructure.Persistence.Repositories; // Cho validator
using Serilog; // Thêm using Serilog
using System.Security.Claims;
using System.Text;

// Cấu hình logger ban đầu để bắt log sớm nhất có thể (bootstrap logger)
Log.Logger = new LoggerConfiguration()
.MinimumLevel.Debug()
.WriteTo.Console()
    .WriteTo.File("logs/apibootstrap-.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

Log.Information("Starting up the API host (bootstrap).");

try // Thêm try-catch để log lỗi nghiêm trọng khi khởi động
{
    var builder = WebApplication.CreateBuilder(args);
    // --- ĐỊNH NGHĨA TÊN CHO CORS POLICY ---
    var MyAllowSpecificOrigins = "_myAllowSpecificOrigins"; // Bạn có thể đặt tên bất kỳ

    // --- Cấu hình Serilog ---
    // Đọc cấu hình Serilog từ appsettings.json và các nguồn khác
    // Ghi đè bootstrap logger bằng logger đã được cấu hình đầy đủ
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration) // Đọc từ appsettings.json
        .ReadFrom.Services(services) // Cho phép DI services tham gia vào cấu hình log
        .Enrich.FromLogContext()
        .WriteTo.Console() // Vẫn ghi ra Console (có thể cấu hình format khác trong appsettings)
                           // Bạn có thể thêm nhiều Sinks khác ở đây hoặc trong appsettings.json
    );
    // --- Kết thúc cấu hình Serilog ---


    // Add services to the container.
    builder.Services.AddControllers();
    // --- Cấu hình Settings ---
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
    // --- Cấu hình EmailSettings ---
    // Đọc section "EmailSettings" từ appsettings.json và bind vào class EmailSettings
    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
    
    // --- Đăng ký FluentValidation ---
    builder.Services.AddValidatorsFromAssemblyContaining<CreateRoleCommandValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserCommandValidator>(); // Đảm bảo validator này được đăng ký
    builder.Services.AddValidatorsFromAssemblyContaining<VerifyEmailRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<ResendVerificationEmailRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>(); // Đảm bảo validator login được đăng ký
    builder.Services.AddValidatorsFromAssemblyContaining<RefreshTokenRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<UpdateUserProfileRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<ChangePasswordRequestValidator>(); // Hoặc một validator khác trong cùng assembly
    builder.Services.AddValidatorsFromAssemblyContaining<ForgotPasswordRequestValidator>(); // Hoặc một validator khác trong cùng assembly
    builder.Services.AddValidatorsFromAssemblyContaining<ResetPasswordRequestValidator>(); // Hoặc một validator khác trong cùng assembly
    builder.Services.AddValidatorsFromAssemblyContaining<Disable2FARequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<Enable2FARequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<Verify2FARequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<InitiateBankDepositRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<CancelBankDepositRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<ConfirmBankDepositRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<AdminDirectDepositRequestValidator>(); // Đảm bảo tất cả các validator được bao gồm
    builder.Services.AddValidatorsFromAssemblyContaining<CreateWithdrawalRequestValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<ApproveWithdrawalRequestValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<RejectWithdrawalRequestValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<VerifyRecipientRequestValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<ExecuteInternalTransferRequestValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<GetAdminPendingBankDepositsQueryValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<GetAdminPendingWithdrawalsQueryValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<GetAdminUsersQueryValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<UpdateUserStatusRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<UpdateUserRoleRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<CreateTradingAccountRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<CreateInitialShareOfferingRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<GetPublicTradingAccountsQueryValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<GetTradingAccountDetailsQueryValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<UpdateTradingAccountRequestValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<GetInitialOfferingsQueryValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<GetMyShareOrdersQueryValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<GetOrderBookQueryValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<GetMarketDataQueryValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<GetMyShareTradesQueryValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<PushLiveDataRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<PushClosedTradesRequestValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<UpdateInitialShareOfferingRequestValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<CancelInitialShareOfferingRequestValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<GetAdminAllOrdersQueryValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<GetAdminAllTradesQueryValidator>(); // Hoặc một validator khác trong cùng assembly Application
    builder.Services.AddValidatorsFromAssemblyContaining<CreateSystemSettingRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<UpdateSystemSettingRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<DeleteSystemSettingRequestValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<GetSystemSettingsQueryValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<GetSystemSettingByIdQueryValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<GetSystemSettingByKeyQueryValidator>();
    builder.Services.AddValidatorsFromAssemblyContaining<GetChartDataQueryValidator>(); // Chart data validator

    //builder.Services.AddScoped<IValidator<CreateRoleCommand>, CreateRoleCommandValidator>();
    builder.Services.AddFluentValidationAutoValidation();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options => // Sử dụng 'options' thay vì 'c' cho rõ ràng hơn
    {
        // Đây là phần quan trọng để định nghĩa thông tin tài liệu API
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "QuantumBands.API",
            Version = "v1", // Đây là phiên bản API của bạn, ví dụ: "v1", "v2"
            Description = "API for QuantumBands AI Fund Management Platform.",
            // Contact = new Microsoft.OpenApi.Models.OpenApiContact
            // {
            //     Name = "Your Company Name",
            //     Email = "contact@yourcompany.com",
            //     Url = new Uri("https://yourcompany.com")
            // },
            // License = new Microsoft.OpenApi.Models.OpenApiLicense
            // {
            //     Name = "Your License Type",
            //     Url = new Uri("https://example.com/license")
            // }
        });

        // Tùy chọn: Nếu bạn sử dụng XML comments cho tài liệu API
        // var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        // var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
        // if (File.Exists(xmlPath))
        // {
        //     options.IncludeXmlComments(xmlPath);
        // }

        // Tùy chọn: Thêm định nghĩa Security Scheme cho JWT (nếu bạn đã có JWT)
        // options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        // {
        //     In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        //     Description = "Please enter a valid token",
        //     Name = "Authorization",
        //     Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        //     BearerFormat = "JWT",
        //     Scheme = "Bearer"
        // });
        // options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        // {
        //     {
        //         new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        //         {
        //             Reference = new Microsoft.OpenApi.Models.OpenApiReference
        //             {
        //                 Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
        //                 Id = "Bearer"
        //             }
        //         },
        //         Array.Empty<string>()
        //     }
        // });
    });
    // --- Đăng ký Caching Services ---
    builder.Services.AddMemoryCache(); // Đăng ký service IMemoryCache của .NET Core
    builder.Services.AddSingleton<ICachingService, InMemoryCachingService>(); // Đăng ký triển khai của chúng ta
    // --- Kết thúc đăng ký Caching Services ---

    // --- Cấu hình Dependency Injection ---
    // Đăng ký DbContext
    builder.Services.AddDbContext<FinixAIDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped<QuantumBands.Application.Interfaces.IGreetingService, QuantumBands.Application.Services.GreetingService>();
    // Đăng ký Generic Repository
    builder.Services.AddScoped(typeof(QuantumBands.Application.Interfaces.IGenericRepository<>), typeof(QuantumBands.Infrastructure.Persistence.Repositories.GenericRepository<>));
    builder.Services.AddScoped<QuantumBands.Application.Interfaces.IUserRoleRepository, QuantumBands.Infrastructure.Persistence.Repositories.UserRoleRepository>();
    builder.Services.AddScoped<QuantumBands.Application.Interfaces.IUnitOfWork, QuantumBands.Infrastructure.Persistence.UnitOfWork>();
    builder.Services.AddScoped<QuantumBands.Application.Services.IRoleManagementService, QuantumBands.Application.Services.RoleManagementService>();
    builder.Services.AddScoped<IAuthService, AuthService>(); // Đăng ký AuthService
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IWalletService, WalletService>();
    builder.Services.AddScoped<IEmailService, EmailService>(); // Đăng ký EmailService
    builder.Services.AddScoped<ITwoFactorAuthService, TwoFactorAuthService>(); // Đăng ký service 2FA
    builder.Services.AddScoped<ISystemSettingService, SystemSettingService>(); // Đăng ký SystemSetting service
    builder.Services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
    builder.Services.AddScoped<ITransactionTypeRepository, TransactionTypeRepository>();
    builder.Services.AddScoped<ITradingAccountService, TradingAccountService>(); // Đăng ký service mới
    builder.Services.AddScoped<ChartDataService>(); // Đăng ký ChartDataService
    builder.Services.AddScoped<IClosedTradeService, ClosedTradeService>(); // Đăng ký ClosedTradeService
    builder.Services.AddScoped<ISharePortfolioService, SharePortfolioService>(); // <<< THÊM DÒNG NÀY
    builder.Services.AddScoped<IEAIntegrationService, EAIntegrationService>();
    builder.Services.AddScoped<IDailySnapshotService, DailySnapshotService>();
    builder.Services.AddScoped<IProfitDistributionService, ProfitDistributionService>();
    builder.Services.AddScoped<IShareOrderStatusRepository, ShareOrderStatusRepository>(); // <<< THÊM HOẶC KIỂM TRA DÒNG NÀY
    builder.Services.AddScoped<IExchangeService, ExchangeService>();
    builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>(); // <<< THÊM DÒNG NÀY
    builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>(); // Đăng ký JWT Generator
                                                                         // --- Cấu hình JWT Authentication ---
                                                                         // Lấy cấu hình JWT từ appsettings để sử dụng trong cấu hình middleware
    var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
    if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.Secret))
    {
        throw new InvalidOperationException("JWT Secret not configured.");
    }
    var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);

    // --- Đăng ký Background Workers ---
    builder.Services.AddHostedService<DailyTradingSnapshotWorker>();

    builder.Services.AddAuthentication(options =>
    {
        // Đặt scheme xác thực mặc định là JwtBearer
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true; // Lưu token trong HttpContext sau khi xác thực thành công
        options.RequireHttpsMetadata = false; // Chỉ đặt là false trong môi trường dev. Nên là true trong production.
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true, // Xác thực Issuer
            ValidateAudience = true, // Xác thực Audience
            ValidateLifetime = true, // Xác thực thời gian sống của token
            ValidateIssuerSigningKey = true, // Xác thực khóa ký

            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(key), // Khóa dùng để xác thực chữ ký
            ClockSkew = TimeSpan.Zero, // Không cho phép chênh lệch thời gian
            RoleClaimType = ClaimTypes.Role // Mặc định là ClaimTypes.Role, nhưng đặt rõ ràng cũng tốt

        };
    });
    // --- Kết thúc cấu hình JWT Authentication ---
    // Đăng ký Authorization (cần thiết để dùng [Authorize])
    // --- Đăng ký Authorization ---
    builder.Services.AddAuthorization(options =>
    {
        // (Tùy chọn) Bạn có thể định nghĩa các policies phức tạp ở đây nếu cần
        // Ví dụ:
        // options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("InvestorOrAdmin", policy => policy.RequireRole("Admin", "Investor"));
    });
    // --- Kết thúc đăng ký Authorization ---
    // --- THÊM CẤU HÌNH CORS Ở ĐÂY ---
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: MyAllowSpecificOrigins,
                          policy =>
                          {
                              // Cho phép bất kỳ origin nào (KHÔNG NÊN DÙNG TRONG PRODUCTION)
                              policy.AllowAnyOrigin()
                                    // Cho phép bất kỳ header nào
                                    .AllowAnyHeader()
                                    // Cho phép bất kỳ phương thức HTTP nào (GET, POST, PUT, DELETE, etc.)
                                    .AllowAnyMethod();

                              // Nếu bạn muốn chỉ định cụ thể các origin (khuyến nghị cho production):
                              // policy.WithOrigins("http://localhost:3000", // Địa chỉ React dev
                              //                    "http://localhost:4200", // Địa chỉ Angular dev
                              //                    "https://your-production-frontend.com")
                              //       .AllowAnyHeader()
                              //       .AllowAnyMethod();

                              // Nếu bạn cần cho phép credentials (ví dụ: cookies, authorization headers)
                              // thì không thể dùng AllowAnyOrigin() cùng lúc.
                              // Bạn phải dùng WithOrigins(...) và thêm AllowCredentials().
                              // policy.WithOrigins("http://localhost:3000")
                              //       .AllowAnyHeader()
                              //       .AllowAnyMethod()
                              //       .AllowCredentials();
                          });
    });
    // --- KẾT THÚC CẤU HÌNH CORS ---
    var app = builder.Build();
    // Configure the HTTP request pipeline.

    // Sử dụng Serilog Request Logging trước Global Exception Handler
    // để các request đều được log, kể cả những request gây ra lỗi
    app.UseSerilogRequestLogging();

    // --- Đăng ký Global Exception Handling Middleware ---
    // Nên đặt ở vị trí sớm trong pipeline để bắt được nhiều lỗi nhất có thể
    app.UseGlobalExceptionHandler();
    // --- Kết thúc đăng ký ---
    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker" || app.Environment.EnvironmentName == "PROD")
    {
        app.UseSwagger(); // Middleware để phục vụ file swagger.json
        app.UseSwaggerUI(options => // Middleware để phục vụ Swagger UI
        {
            // Endpoint mặc định cho file JSON
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "QuantumBands.API v1");
            // Tùy chọn: Để Swagger UI là trang chủ của ứng dụng khi ở môi trường Development
            // options.RoutePrefix = string.Empty;
        });
    }
    app.UseHttpsRedirection();
    app.UseCors(MyAllowSpecificOrigins);
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    Log.Information("Application starting up successfully."); // Log thông báo khởi động thành công
                                                              // Đọc ví dụ cấu hình
    var platformName = app.Configuration["PlatformSettings:PlatformName"];
    var platformVersion = app.Configuration.GetValue<string>("PlatformSettings:Version");
    Log.Information("Platform Name from config: {PlatformName}", platformName);
    Log.Information("Platform Version from config: {PlatformVersion}", platformVersion);    Log.Information("Application starting up successfully.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start."); // Log lỗi nghiêm trọng
}
finally
{
    Log.Information("Shut down complete.");
    Log.CloseAndFlush(); // Đảm bảo tất cả log được ghi trước khi ứng dụng thoát
}

// Make Program class public for testing
public partial class Program { }