// QuantumBands.Application/Services/AuthService.cs
using QuantumBands.Application.Features.Authentication;
using QuantumBands.Application.Features.Authentication.Commands.RegisterUser;
using QuantumBands.Application.Interfaces;
using QuantumBands.Domain.Entities; // User, UserRole, Wallet entities
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration; // Thêm using này
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Security.Cryptography;
using System.Web; // For DateTime
using QuantumBands.Application.Features.Authentication.Commands.VerifyEmail; // Thêm using
using QuantumBands.Application.Features.Authentication.Commands.ResendVerificationEmail; // Thêm using
using QuantumBands.Application.Features.Authentication.Commands.Login; // Thêm using
using QuantumBands.Application.Features.Authentication.Commands.RefreshToken; // Thêm using
using QuantumBands.Application.Features.Authentication.Commands.ForgotPassword; // Thêm using
using QuantumBands.Application.Features.Authentication.Commands.ResetPassword; // Thêm using
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // Thêm using
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt; // Thêm using cho Include


namespace QuantumBands.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration; // Inject IConfiguration
    private readonly IJwtTokenGenerator _jwtTokenGenerator; // Inject JWT generator
    private readonly JwtSettings _jwtSettings; // Inject JwtSettings để lấy expiry

    private const string DefaultUserRole = "Investor"; // Tên vai trò mặc định


    // Update the constructor to accept IConfiguration and initialize the field  
    public AuthService(
       IUnitOfWork unitOfWork,
       ILogger<AuthService> logger,
       IEmailService emailService,
       IConfiguration configuration,
        IJwtTokenGenerator jwtTokenGenerator, // Thêm vào constructor
        IOptions<JwtSettings> jwtSettingsOptions) // Thêm vào constructor
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
        _jwtTokenGenerator = jwtTokenGenerator; // Gán
        _jwtSettings = jwtSettingsOptions.Value; // Gán
    }

    public async Task<(UserDto? User, string? ErrorMessage)> RegisterUserAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to register user with username: {Username} and email: {Email}", command.Username, command.Email);

        // 1. Kiểm tra Username hoặc Email đã tồn tại chưa
        var existingUserByUsername = (await _unitOfWork.Users.FindAsync(u => u.Username == command.Username, cancellationToken)).FirstOrDefault();
        if (existingUserByUsername != null)
        {
            _logger.LogWarning("Username {Username} already exists.", command.Username);
            return (null, $"Username '{command.Username}' already exists.");
        }

        var existingUserByEmail = (await _unitOfWork.Users.FindAsync(u => u.Email == command.Email, cancellationToken)).FirstOrDefault();
        if (existingUserByEmail != null)
        {
            _logger.LogWarning("Email {Email} already exists.", command.Email);
            return (null, $"Email '{command.Email}' already exists.");
        }

        // 2. Hash mật khẩu
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(command.Password);
        _logger.LogDebug("Password hashed for user {Username}.", command.Username);

        // 3. Lấy vai trò mặc định 'Investor'
        var investorRole = await _unitOfWork.UserRoles.GetRoleByNameAsync(DefaultUserRole); // Giả sử GetRoleByNameAsync tồn tại trong IUserRoleRepository
        if (investorRole == null)
        {
            _logger.LogError("Default role '{DefaultUserRole}' not found in the database.", DefaultUserRole);
            // Có thể tạo role nếu không tìm thấy, hoặc throw lỗi nghiêm trọng
            // For now, we'll return an error.
            return (null, $"System configuration error: Default role '{DefaultUserRole}' not found.");
        }
        _logger.LogDebug("Default role '{DefaultUserRole}' (ID: {RoleId}) found.", DefaultUserRole, investorRole.RoleId);


        // --- BẮT ĐẦU THAY ĐỔI ---
        // 4. Tạo Token và Thời gian hết hạn
        string verificationToken = GenerateSecureToken();
        DateTime tokenExpiry = DateTime.UtcNow.AddHours(24); // Token hết hạn sau 24 giờ
        _logger.LogDebug("Generated verification token for {Username}. Expires at {ExpiryTime}", command.Username, tokenExpiry);

        // 5. Tạo User entity (bao gồm token và expiry)
        var newUser = new User
        {
            Username = command.Username,
            Email = command.Email,
            PasswordHash = hashedPassword,
            FullName = command.FullName,
            RoleId = investorRole.RoleId,
            IsActive = true, // Kích hoạt tài khoản ngay, nhưng cần xác thực email để đăng nhập (nếu logic đăng nhập yêu cầu)
            IsEmailVerified = false,
            EmailVerificationToken = verificationToken, // Lưu token
            EmailVerificationTokenExpiry = tokenExpiry, // Lưu thời gian hết hạn
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 5. Thêm User vào DbContext (chưa lưu vào DB)
        await _unitOfWork.Users.AddAsync(newUser, cancellationToken);
        _logger.LogInformation("User entity created for {Username}. Attempting to save to DB to get UserID.", command.Username);

        // *QUAN TRỌNG*: Cần lưu User để có UserID cho Wallet
        // Nếu không có UserID, không thể tạo Wallet với khóa ngoại chính xác
        // Chúng ta sẽ lưu User trước, sau đó tạo Wallet
        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken); // Lưu User để có UserID
            _logger.LogInformation("User {Username} (ID: {UserId}) saved to database.", newUser.Username, newUser.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save initial user {Username} to database.", newUser.Username);
            return (null, "An error occurred while saving user information.");
        }

        // 6. Tạo Wallet cho người dùng mới
        var newWallet = new Wallet
        {
            UserId = newUser.UserId, // Sử dụng UserID vừa được tạo
            Balance = 0.00m,
            CurrencyCode = "USD", // Mặc định
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Wallets.AddAsync(newWallet, cancellationToken); // Giả sử có IGenericRepository<Wallet> trong IUnitOfWork
        _logger.LogInformation("Wallet entity created for UserID {UserId}.", newUser.UserId);

        // 7. Lưu tất cả thay đổi (bao gồm Wallet) vào database
        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Wallet for UserID {UserId} saved. Registration process complete for {Username}.", newUser.UserId, newUser.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save wallet for user {Username} (ID: {UserId}).", newUser.Username, newUser.UserId);
            // Cân nhắc rollback việc tạo user nếu tạo wallet thất bại, hoặc xử lý khác
            // Hiện tại, user đã được tạo, nhưng wallet có thể chưa.
            return (null, "An error occurred while finalizing account setup.");
        }


        // 9. Gửi email xác thực với Link
        try
        {
            string? frontendBaseUrl = _configuration["AppSettings:FrontendBaseUrl"]; // Lấy URL frontend từ config
            if (string.IsNullOrEmpty(frontendBaseUrl))
            {
                _logger.LogWarning("FrontendBaseUrl is not configured in AppSettings. Cannot generate verification link.");
                // Quyết định xem có nên báo lỗi hay không. Hiện tại chỉ log warning.
            }
            else
            {
                // Mã hóa token để an toàn khi truyền qua URL
                string encodedToken = HttpUtility.UrlEncode(verificationToken);
                string verificationLink = $"{frontendBaseUrl.TrimEnd('/')}/auth/verify-email?userId={newUser.UserId}&token={encodedToken}";

                string subject = "Verify Your Email for QuantumBands AI";
                string htmlMessage = $@"
                    <h1>Welcome, {newUser.Username}!</h1>
                    <p>Thank you for registering at QuantumBands AI. Please verify your email address by clicking the link below:</p>
                    <p><a href='{verificationLink}'>Verify My Email</a></p>
                    <p>If you cannot click the link, please copy and paste the following URL into your browser:</p>
                    <p>{verificationLink}</p>
                    <p>This link will expire in 24 hours.</p>
                    <p>If you did not create this account, please ignore this email.</p>";

                // Gửi email ngầm
                _ = _emailService.SendEmailAsync(newUser.Email, subject, htmlMessage, cancellationToken);
                _logger.LogInformation("Verification email initiated for {Email} with link: {Link}", newUser.Email, verificationLink);
            }
        }
        catch (Exception emailEx)
        {
            _logger.LogError(emailEx, "Failed to initiate verification email for {Email}, but user registration was successful.", newUser.Email);
            // Không nên fail cả quá trình đăng ký chỉ vì gửi mail lỗi
        }

        // 9. Tạo UserDto để trả về
        var userDto = new UserDto
        {
            UserId = newUser.UserId,
            Username = newUser.Username,
            Email = newUser.Email,
            FullName = newUser.FullName,
            CreatedAt = newUser.CreatedAt
        };

        return (userDto, null);
    }
    public async Task<(bool Success, string Message)> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to verify email for UserID: {UserId}", request.UserId);

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId); // Giả sử GetByIdAsync tồn tại

        if (user == null)
        {
            _logger.LogWarning("VerifyEmail: User with ID {UserId} not found.", request.UserId);
            return (false, "Invalid user or token."); // Không tiết lộ user không tồn tại
        }

        if (user.IsEmailVerified)
        {
            _logger.LogInformation("Email for UserID {UserId} is already verified.", request.UserId);
            return (true, "Email is already verified.");
        }

        if (user.EmailVerificationToken != request.Token)
        {
            _logger.LogWarning("VerifyEmail: Invalid token provided for UserID {UserId}.", request.UserId);
            return (false, "Invalid user or token.");
        }
        // Fix for CS1061: 'DateTime' does not contain a definition for 'HasValue' and 'Value'
        // Explanation: The `DateTime` type is a non-nullable struct and does not have `HasValue` or `Value` properties. 
        // These properties are available on `Nullable<DateTime>` (or `DateTime?`). 
        // To fix this, we need to ensure that the `EmailVerificationTokenExpiry` property is nullable (`DateTime?`) in the `User` class.

        if (user.EmailVerificationTokenExpiry.HasValue && user.EmailVerificationTokenExpiry.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("VerifyEmail: Token expired for UserID {UserId}. Expiry: {ExpiryTime}", request.UserId, user.EmailVerificationTokenExpiry.Value);
            return (false, "Verification token has expired. Please request a new one.");
        }

        // Xác thực thành công
        user.IsEmailVerified = true;
        user.EmailVerificationToken = null; // Xóa token sau khi sử dụng
        user.EmailVerificationTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user); // Đánh dấu user là đã thay đổi
        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Email successfully verified for UserID {UserId}.", request.UserId);
            return (true, "Email successfully verified.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user verification status for UserID {UserId}.", request.UserId);
            return (false, "An error occurred while verifying email.");
        }
    }

    public async Task<(bool Success, string Message)> ResendVerificationEmailAsync(ResendVerificationEmailRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to resend verification email for Email: {Email}", request.Email);

        var user = (await _unitOfWork.Users.FindAsync(u => u.Email == request.Email, cancellationToken)).FirstOrDefault();

        if (user == null)
        {
            _logger.LogWarning("ResendVerificationEmail: User with email {Email} not found.", request.Email);
            // Vẫn trả về thông báo chung để không tiết lộ email tồn tại
            return (true, "If an account with this email exists and is not verified, a new verification email has been sent.");
        }

        if (user.IsEmailVerified)
        {
            _logger.LogInformation("ResendVerificationEmail: Email {Email} is already verified for UserID {UserId}.", request.Email, user.UserId);
            return (true, "This email address is already verified.");
        }

        // Tạo token mới và thời gian hết hạn mới
        string newVerificationToken = GenerateSecureToken();
        DateTime newTokenExpiry = DateTime.UtcNow.AddHours(24);

        user.EmailVerificationToken = newVerificationToken;
        user.EmailVerificationTokenExpiry = newTokenExpiry;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Generated and saved new verification token for UserID {UserId}.", user.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save new verification token for UserID {UserId}.", user.UserId);
            return (false, "An error occurred while preparing the verification email.");
        }

        // Gửi email mới
        try
        {
            string? frontendBaseUrl = _configuration["AppSettings:FrontendBaseUrl"];
            if (string.IsNullOrEmpty(frontendBaseUrl))
            {
                _logger.LogWarning("FrontendBaseUrl is not configured. Cannot generate verification link for resend.");
                return (false, "Email server configuration error."); // Báo lỗi nếu không gửi được link
            }
            else
            {
                string encodedToken = HttpUtility.UrlEncode(newVerificationToken);
                string verificationLink = $"{frontendBaseUrl.TrimEnd('/')}/auth/verify-email?userId={user.UserId}&token={encodedToken}";

                string subject = "Verify Your Email for QuantumBands AI (New Link)";
                string htmlMessage = $@"
                    <h1>Verify Your Email Address</h1>
                    <p>You requested a new email verification link for your QuantumBands AI account.</p>
                    <p>Please click the link below to verify your email:</p>
                    <p><a href='{verificationLink}'>Verify My Email</a></p>
                    <p>If you cannot click the link, please copy and paste the following URL into your browser:</p>
                    <p>{verificationLink}</p>
                    <p>This link will expire in 24 hours.</p>
                    <p>If you did not request this, please ignore this email.</p>";

                _ = _emailService.SendEmailAsync(user.Email, subject, htmlMessage, cancellationToken);
                _logger.LogInformation("New verification email initiated for {Email} with link: {Link}", user.Email, verificationLink);
                return (true, "If an account with this email exists and is not verified, a new verification email has been sent.");
            }
        }
        catch (Exception emailEx)
        {
            _logger.LogError(emailEx, "Failed to initiate resend verification email for {Email}.", user.Email);
            return (false, "An error occurred while sending the verification email.");
        }
    }
    public async Task<(LoginResponse? Response, string? ErrorMessage)> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting login for user: {UsernameOrEmail}", request.UsernameOrEmail);

        // 1. Tìm user bằng Username hoặc Email, bao gồm cả thông tin Role
        var user = await _unitOfWork.Users.Query() // Giả sử có phương thức Query() trả về IQueryable<User>
                                    .Include(u => u.Role) // Nạp thông tin Role liên quan
                                    .FirstOrDefaultAsync(u => u.Username == request.UsernameOrEmail || u.Email == request.UsernameOrEmail, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User {UsernameOrEmail} not found.", request.UsernameOrEmail);
            return (null, "Invalid username/email or password."); // Thông báo chung
        }

        // 2. Kiểm tra tài khoản có active không
        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: User {Username} account is inactive.", user.Username);
            return (null, "Your account is inactive. Please contact support.");
        }

        // 3. (Tùy chọn) Kiểm tra email đã xác thực chưa (nếu là yêu cầu)
        // if (!user.IsEmailVerified)
        // {
        //     _logger.LogWarning("Login failed: User {Username} email is not verified.", user.Username);
        //     // Có thể gửi lại email xác thực ở đây
        //     return (null, "Please verify your email address before logging in.");
        // }

        // 4. Xác thực mật khẩu
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Login failed: Invalid password for user {Username}.", user.Username);
            return (null, "Invalid username/email or password."); // Thông báo chung
        }

        // 5. Lấy tên Role (đã Include ở trên)
        string roleName = user.Role?.RoleName ?? "Unknown"; // Lấy tên role, xử lý nếu null
        if (user.Role == null)
        {
            _logger.LogError("User {Username} (ID: {UserId}) does not have a valid Role associated.", user.Username, user.UserId);
            return (null, "User role configuration error."); // Lỗi hệ thống
        }

        // 6. Tạo JWT và Refresh Token
        string jwtToken = _jwtTokenGenerator.GenerateJwtToken(user, roleName);
        string refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        DateTime refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);

        _logger.LogDebug("Generated JWT and Refresh Token for user {Username}.", user.Username);

        // 7. Cập nhật thông tin user trong DB (RefreshToken, Expiry, LastLoginDate)
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = refreshTokenExpiry;
        user.LastLoginDate = DateTime.UtcNow; // Cập nhật thời gian đăng nhập cuối
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("User {Username} logged in successfully. Tokens updated.", user.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user tokens and last login date for {Username}.", user.Username);
            // Vẫn có thể trả về token vì đã tạo được, nhưng DB chưa cập nhật
            // Hoặc trả về lỗi tùy thuộc vào yêu cầu nghiệp vụ
            return (null, "An error occurred while finalizing login.");
        }

        // 8. Tạo Response DTO
        var loginResponse = new LoginResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Role = roleName,
            JwtToken = jwtToken,
            RefreshToken = refreshToken,
            RefreshTokenExpiry = refreshTokenExpiry
        };

        return (loginResponse, null); // Đăng nhập thành công
    }
    public async Task<(LoginResponse? Response, string? ErrorMessage)> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to refresh token.");

        // 1. Lấy principal từ JWT đã hết hạn (nhưng vẫn validate chữ ký)
        var principal = _jwtTokenGenerator.GetPrincipalFromExpiredToken(request.ExpiredJwtToken);
        if (principal == null)
        {
            _logger.LogWarning("Refresh token failed: Invalid expired JWT token provided.");
            return (null, "Invalid token or refresh token.");
        }

        // 2. Lấy UserID từ claims
        var userIdString = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "uid" || c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdString, out var userId))
        {
            _logger.LogWarning("Refresh token failed: Could not extract UserID from expired token.");
            return (null, "Invalid token or refresh token.");
        }

        _logger.LogDebug("UserID {UserId} extracted from expired token.", userId);

        // 3. Lấy user từ DB, bao gồm cả Role
        var user = await _unitOfWork.Users.Query()
                                    .Include(u => u.Role)
                                    .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Refresh token failed: User with ID {UserId} not found.", userId);
            return (null, "Invalid token or refresh token.");
        }

        // 4. Kiểm tra Refresh Token có khớp và còn hạn không
        if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiry <= DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token failed for UserID {UserId}: Stored token mismatch or token expired. DB Token: {DbToken}, Request Token: {ReqToken}, DB Expiry: {DbExpiry}",
                               userId, user.RefreshToken, request.RefreshToken, user.RefreshTokenExpiry);
            // Có thể xem xét việc thu hồi tất cả refresh token của user nếu có dấu hiệu lạm dụng
            // user.RefreshToken = null;
            // user.RefreshTokenExpiry = null;
            // _unitOfWork.Users.Update(user);
            // await _unitOfWork.CompleteAsync(cancellationToken);
            return (null, "Invalid token or refresh token.");
        }

        _logger.LogDebug("Refresh token validated for UserID {UserId}.", userId);

        // 5. Tạo JWT mới và Refresh Token mới
        string roleName = user.Role?.RoleName ?? "Unknown";
        if (user.Role == null)
        {
            _logger.LogError("User {Username} (ID: {UserId}) does not have a valid Role associated during token refresh.", user.Username, user.UserId);
            return (null, "User role configuration error during token refresh.");
        }

        string newJwtToken = _jwtTokenGenerator.GenerateJwtToken(user, roleName);
        string newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        DateTime newRefreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);

        _logger.LogDebug("Generated new JWT and Refresh Token for UserID {UserId}.", userId);

        // 6. Cập nhật Refresh Token và thời gian hết hạn trong DB
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = newRefreshTokenExpiry;
        user.UpdatedAt = DateTime.UtcNow; // Cập nhật thời gian
        // Không cần cập nhật LastLoginDate ở đây, vì đây là refresh, không phải login mới

        _unitOfWork.Users.Update(user);
        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Successfully refreshed tokens for UserID {UserId}.", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update new refresh token for UserID {UserId}.", userId);
            return (null, "An error occurred while refreshing token.");
        }

        // 7. Tạo Response
        var loginResponse = new LoginResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Role = roleName,
            JwtToken = newJwtToken,
            RefreshToken = newRefreshToken,
            RefreshTokenExpiry = newRefreshTokenExpiry
        };

        return (loginResponse, null);
    }
    public async Task<(bool Success, string Message)> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Forgot password request received for email: {Email}", request.Email);

        var user = (await _unitOfWork.Users.FindAsync(u => u.Email == request.Email, cancellationToken)).FirstOrDefault();

        // Luôn trả về thông báo thành công chung để tránh user enumeration
        string successMessage = "If an account with this email exists, a password reset link has been sent.";

        if (user == null)
        {
            _logger.LogWarning("Forgot password: User with email {Email} not found. Sending generic success message.", request.Email);
            return (true, successMessage); // Không tiết lộ email không tồn tại
        }

        // (Tùy chọn) Bạn có thể muốn kiểm tra xem email đã được xác thực chưa trước khi cho reset
        // if (!user.IsEmailVerified)
        // {
        //     _logger.LogWarning("Forgot password: Email {Email} for UserID {UserId} is not verified. Sending generic success message.", request.Email, user.UserId);
        //     return (true, successMessage);
        // }

        // Tạo token và thời gian hết hạn
        string resetToken = GenerateSecureToken(); // Sử dụng lại hàm đã có
        DateTime tokenExpiry = DateTime.UtcNow.AddHours(1); // Token hết hạn sau 1 giờ

        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpiry = tokenExpiry;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Generated and saved password reset token for UserID {UserId}.", user.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save password reset token for UserID {UserId}.", user.UserId);
            // Vẫn trả về thông báo chung, nhưng log lỗi nghiêm trọng
            return (true, successMessage);
        }

        // Gửi email với link đặt lại mật khẩu
        try
        {
            string? frontendBaseUrl = _configuration["AppSettings:FrontendBaseUrl"];
            if (string.IsNullOrEmpty(frontendBaseUrl))
            {
                _logger.LogError("FrontendBaseUrl is not configured. Cannot generate password reset link for UserID {UserId}.", user.UserId);
                // Vẫn trả về thông báo chung
                return (true, successMessage);
            }

            string encodedToken = HttpUtility.UrlEncode(resetToken);
            string encodedEmail = HttpUtility.UrlEncode(user.Email); // Mã hóa email để truyền qua URL
            string resetLink = $"{frontendBaseUrl.TrimEnd('/')}/auth/reset-password?email={encodedEmail}&token={encodedToken}";

            string subject = "Reset Your QuantumBands AI Password";
            string htmlMessage = $@"
                <h1>Password Reset Request</h1>
                <p>Hello {user.Username ?? user.FullName ?? "User"},</p>
                <p>We received a request to reset the password for your QuantumBands AI account associated with this email address.</p>
                <p>Please click the link below to set a new password:</p>
                <p><a href='{resetLink}'>Reset My Password</a></p>
                <p>If you cannot click the link, please copy and paste the following URL into your browser:</p>
                <p>{resetLink}</p>
                <p>This link will expire in 1 hour. If you did not request a password reset, please ignore this email.</p>";

            _ = _emailService.SendEmailAsync(user.Email, subject, htmlMessage, cancellationToken);
            _logger.LogInformation("Password reset email initiated for {Email} with link: {Link}", user.Email, resetLink);
        }
        catch (Exception emailEx)
        {
            _logger.LogError(emailEx, "Failed to initiate password reset email for {Email}, but token was generated for UserID {UserId}.", user.Email, user.UserId);
            // Vẫn trả về thông báo chung
        }

        return (true, successMessage);
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to reset password for email: {Email}", request.Email);

        var user = (await _unitOfWork.Users.FindAsync(u => u.Email == request.Email, cancellationToken)).FirstOrDefault();

        if (user == null)
        {
            _logger.LogWarning("ResetPassword: User with email {Email} not found.", request.Email);
            return (false, "Invalid email or reset token."); // Thông báo chung
        }

        if (user.PasswordResetToken != request.ResetToken)
        {
            _logger.LogWarning("ResetPassword: Invalid reset token provided for UserID {UserId}.", user.UserId);
            return (false, "Invalid email or reset token.");
        }

        if (user.PasswordResetTokenExpiry.HasValue && user.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("ResetPassword: Token expired for UserID {UserId}. Expiry: {ExpiryTime}", user.UserId, user.PasswordResetTokenExpiry.Value);
            return (false, "Password reset token has expired. Please request a new one.");
        }

        // (Validator đã kiểm tra NewPassword và ConfirmNewPassword khớp nhau)

        // Hash mật khẩu mới
        string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        // Cập nhật mật khẩu, xóa token và thời gian
        user.PasswordHash = newPasswordHash;
        user.PasswordResetToken = null; // Vô hiệu hóa token sau khi sử dụng
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        // (Tùy chọn) Có thể muốn cập nhật RefreshToken ở đây để vô hiệu hóa các session cũ
        // user.RefreshToken = null;
        // user.RefreshTokenExpiry = null;

        _unitOfWork.Users.Update(user);
        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Password reset successfully for UserID {UserId}.", user.UserId);

            // (Tùy chọn) Gửi email thông báo mật khẩu đã được thay đổi
            string subject = "Your QuantumBands AI Password Has Been Changed";
            string htmlMessage = $@"
                <h1>Password Changed</h1>
                <p>Hello {user.Username ?? user.FullName ?? "User"},</p>
                <p>The password for your QuantumBands AI account was recently changed.</p>
                <p>If you did not make this change, please contact our support team immediately.</p>";
            _ = _emailService.SendEmailAsync(user.Email, subject, htmlMessage, cancellationToken);


            return (true, "Password reset successfully. You can now log in with your new password.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for UserID {UserId}.", user.UserId);
            return (false, "An error occurred while resetting password.");
        }
    }
    // Hàm helper để tạo token ngẫu nhiên, an toàn
    private string GenerateSecureToken(int length = 32)
    {
        // Tạo một chuỗi byte ngẫu nhiên
        byte[] randomNumber = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
        }
        // Chuyển đổi sang chuỗi Base64 URL-safe
        return Convert.ToBase64String(randomNumber)
            .TrimEnd('=') // Loại bỏ padding '='
            .Replace('+', '-') // Thay thế ký tự không an toàn cho URL
            .Replace('/', '_');
    }
    private int? GetUserIdFromPrincipal(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }
        var userIdString = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "uid" || c.Type == JwtRegisteredClaimNames.Sub)?.Value;

        if (int.TryParse(userIdString, out var userId))
        {
            return userId;
        }
        return null;
    }
    public async Task<(bool Success, string Message)> LogoutAsync(ClaimsPrincipal currentUser, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromPrincipal(currentUser); // Sử dụng lại hàm helper đã có
        if (!userId.HasValue)
        {
            // Điều này không nên xảy ra nếu endpoint được bảo vệ bởi [Authorize]
            _logger.LogWarning("LogoutAsync: Attempted logout for a non-authenticated user or missing UserId claim.");
            return (false, "User not authenticated.");
        }

        _logger.LogInformation("User {UserId} attempting to logout.", userId.Value);

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
        if (user == null)
        {
            _logger.LogWarning("LogoutAsync: User with ID {UserId} not found in database, though authenticated.", userId.Value);
            // Dù user không tìm thấy, vẫn nên coi như logout thành công từ phía client
            return (true, "Logout processed. Client should clear tokens.");
        }

        // Vô hiệu hóa Refresh Token bằng cách xóa nó và thời gian hết hạn
        // Giả định User entity có các trường RefreshToken và RefreshTokenExpiry
        if (!string.IsNullOrEmpty(user.RefreshToken))
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);

            try
            {
                await _unitOfWork.CompleteAsync(cancellationToken);
                _logger.LogInformation("Refresh token invalidated for UserID {UserId}.", userId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating refresh token for UserID {UserId} during logout.", userId.Value);
                // Không nên block quá trình logout chỉ vì lỗi này, client vẫn nên xóa token
                // nhưng đây là một lỗi server cần được xem xét.
                return (false, "Logout processed with server-side error during token invalidation. Client should still clear tokens.");
            }
        }
        else
        {
            _logger.LogInformation("No active refresh token found to invalidate for UserID {UserId}.", userId.Value);
        }

        return (true, "Logout successful. Please clear tokens on the client-side.");
    }

}