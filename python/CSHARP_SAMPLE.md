# 💻 C# IMPLEMENTATION SAMPLE

> **Lưu ý**: Đây là sample code preview để minh họa implementation approach. 
> Code thực tế sẽ cần research MT5 .NET API chi tiết hơn.

---

## 🏗️ **Project Structure**

```
MT5DataPusher/
├── src/
│   ├── MT5DataPusher.Core/
│   │   ├── Interfaces/
│   │   │   ├── IMT5Service.cs
│   │   │   └── IApiService.cs
│   │   ├── Models/
│   │   │   ├── AccountInfo.cs
│   │   │   ├── Position.cs
│   │   │   └── ClosedTrade.cs
│   │   ├── Services/
│   │   │   ├── MT5Service.cs
│   │   │   └── ApiService.cs
│   │   └── Configuration/
│   │       └── MT5Configuration.cs
│   ├── MT5DataPusher.Service/
│   │   ├── Program.cs
│   │   ├── Worker.cs
│   │   └── appsettings.json
│   └── MT5DataPusher.Console/
│       └── Program.cs
└── tests/
    ├── MT5DataPusher.Core.Tests/
    └── MT5DataPusher.Integration.Tests/
```

---

## 📋 **1. Configuration Models**

### **MT5Configuration.cs**
```csharp
using System.ComponentModel.DataAnnotations;

namespace MT5DataPusher.Core.Configuration;

public class MT5Configuration
{
    [Required]
    public string ApiBaseUrl { get; set; } = string.Empty;
    
    [Required]
    public string ApiKey { get; set; } = string.Empty;
    
    [Range(1, 300)]
    public int TimeIntervalSeconds { get; set; } = 3;
    
    [Range(1, 168)]
    public int LookbackHoursClosedTrades { get; set; } = 24;
    
    public bool DisableSSLVerification { get; set; } = false;
    
    [Range(5, 300)]
    public int RequestTimeoutSeconds { get; set; } = 30;
    
    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;
    
    [Range(1, 60)]
    public int RetryDelaySeconds { get; set; } = 5;
    
    public List<MT5Account> Accounts { get; set; } = new();
}

public class MT5Account
{
    [Required]
    public int TradingAccountIdSystem { get; set; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int Login { get; set; }
    
    [Required]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    public string Server { get; set; } = string.Empty;
    
    public bool Enabled { get; set; } = true;
}
```

---

## 📊 **2. Domain Models**

### **AccountInfo.cs**
```csharp
namespace MT5DataPusher.Core.Models;

public record AccountInfo
{
    public decimal AccountEquity { get; init; }
    public decimal AccountBalance { get; init; }
    public string Currency { get; init; } = string.Empty;
    public int Leverage { get; init; }
    public decimal Margin { get; init; }
    public decimal FreeMargin { get; init; }
}
```

### **Position.cs**
```csharp
namespace MT5DataPusher.Core.Models;

public record Position
{
    public string EaTicketId { get; init; } = string.Empty;
    public string Symbol { get; init; } = string.Empty;
    public TradeType TradeType { get; init; }
    public decimal VolumeLots { get; init; }
    public decimal OpenPrice { get; init; }
    public DateTimeOffset OpenTime { get; init; }
    public decimal CurrentMarketPrice { get; init; }
    public decimal Swap { get; init; }
    public decimal Commission { get; init; }
    public decimal FloatingPAndL { get; init; }
}

public enum TradeType
{
    Unknown = 0,
    Buy = 1,
    Sell = 2
}
```

### **ClosedTrade.cs**
```csharp
namespace MT5DataPusher.Core.Models;

public record ClosedTrade
{
    public string EaTicketId { get; init; } = string.Empty;
    public string Symbol { get; init; } = string.Empty;
    public TradeType TradeType { get; init; }
    public decimal VolumeLots { get; init; }
    public decimal OpenPrice { get; init; }
    public DateTimeOffset OpenTime { get; init; }
    public decimal ClosePrice { get; init; }
    public DateTimeOffset CloseTime { get; init; }
    public decimal Swap { get; init; }
    public decimal Commission { get; init; }
    public decimal RealizedPAndL { get; init; }
}
```

---

## 🔌 **3. Service Interfaces**

### **IMT5Service.cs**
```csharp
namespace MT5DataPusher.Core.Interfaces;

public interface IMT5Service : IDisposable
{
    /// <summary>
    /// Khởi tạo kết nối đến MT5 terminal
    /// </summary>
    Task<bool> InitializeAsync(int login, string password, string server, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lấy thông tin tài khoản hiện tại
    /// </summary>
    Task<AccountInfo?> GetAccountInfoAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lấy danh sách các lệnh đang mở
    /// </summary>
    Task<List<Position>> GetOpenPositionsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lấy lịch sử các lệnh đã đóng
    /// </summary>
    Task<List<ClosedTrade>> GetClosedTradesAsync(DateTimeOffset fromUtc, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kiểm tra kết nối còn hoạt động không
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Đóng kết nối MT5
    /// </summary>
    void Shutdown();
}
```

### **IApiService.cs**
```csharp
namespace MT5DataPusher.Core.Interfaces;

public interface IApiService
{
    /// <summary>
    /// Gửi live data (account info + open positions) lên API
    /// </summary>
    Task<bool> PushLiveDataAsync(int tradingAccountId, AccountInfo accountInfo, 
        List<Position> openPositions, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gửi closed trades data lên API
    /// </summary>
    Task<bool> PushClosedTradesAsync(int tradingAccountId, 
        List<ClosedTrade> closedTrades, CancellationToken cancellationToken = default);
}
```

---

## 🚀 **4. Windows Service Worker**

### **Worker.cs**
```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MT5DataPusher.Core.Configuration;
using MT5DataPusher.Core.Interfaces;

namespace MT5DataPusher.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly MT5Configuration _config;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ILogger<Worker> logger, IOptions<MT5Configuration> config, IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MT5 Data Pusher Service đã bắt đầu. Interval: {IntervalSeconds}s", _config.TimeIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAllAccountsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý accounts: {Error}", ex.Message);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_config.TimeIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
        }

        _logger.LogInformation("MT5 Data Pusher Service đã dừng");
    }

    private async Task ProcessAllAccountsAsync(CancellationToken cancellationToken)
    {
        var enabledAccounts = _config.Accounts.Where(a => a.Enabled).ToList();
        
        if (!enabledAccounts.Any())
        {
            _logger.LogWarning("Không có tài khoản nào được kích hoạt");
            return;
        }

        _logger.LogDebug("Đang xử lý {Count} tài khoản", enabledAccounts.Count);

        foreach (var account in enabledAccounts)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessSingleAccountAsync(account, cancellationToken);
        }
    }

    private async Task ProcessSingleAccountAsync(MT5Account account, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var mt5Service = scope.ServiceProvider.GetRequiredService<IMT5Service>();
        var apiService = scope.ServiceProvider.GetRequiredService<IApiService>();

        try
        {
            _logger.LogDebug("Đang xử lý tài khoản SystemID={SystemId}, Login={Login}", 
                account.TradingAccountIdSystem, account.Login);

            // 1. Kết nối MT5
            if (!await mt5Service.InitializeAsync(account.Login, account.Password, account.Server, cancellationToken))
            {
                _logger.LogError("Không thể kết nối MT5 cho tài khoản {Login}", account.Login);
                return;
            }

            // 2. Lấy và gửi Live Data
            await ProcessLiveDataAsync(mt5Service, apiService, account.TradingAccountIdSystem, cancellationToken);

            // 3. Lấy và gửi Closed Trades
            await ProcessClosedTradesAsync(mt5Service, apiService, account.TradingAccountIdSystem, cancellationToken);

            _logger.LogDebug("Đã xử lý xong tài khoản {Login}", account.Login);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý tài khoản {Login}: {Error}", account.Login, ex.Message);
        }
        finally
        {
            mt5Service.Shutdown();
        }
    }

    private async Task ProcessLiveDataAsync(IMT5Service mt5Service, IApiService apiService, 
        int tradingAccountId, CancellationToken cancellationToken)
    {
        try
        {
            var accountInfo = await mt5Service.GetAccountInfoAsync(cancellationToken);
            if (accountInfo == null)
            {
                _logger.LogWarning("Không lấy được thông tin tài khoản cho SystemID={SystemId}", tradingAccountId);
                return;
            }

            var openPositions = await mt5Service.GetOpenPositionsAsync(cancellationToken);

            var success = await apiService.PushLiveDataAsync(tradingAccountId, accountInfo, openPositions, cancellationToken);
            if (!success)
            {
                _logger.LogWarning("Không thể gửi live data cho SystemID={SystemId}", tradingAccountId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý live data cho SystemID={SystemId}: {Error}", tradingAccountId, ex.Message);
        }
    }

    private async Task ProcessClosedTradesAsync(IMT5Service mt5Service, IApiService apiService, 
        int tradingAccountId, CancellationToken cancellationToken)
    {
        try
        {
            var fromUtc = DateTimeOffset.UtcNow.AddHours(-_config.LookbackHoursClosedTrades);
            var closedTrades = await mt5Service.GetClosedTradesAsync(fromUtc, cancellationToken);

            if (closedTrades.Any())
            {
                var success = await apiService.PushClosedTradesAsync(tradingAccountId, closedTrades, cancellationToken);
                if (!success)
                {
                    _logger.LogWarning("Không thể gửi closed trades cho SystemID={SystemId}", tradingAccountId);
                }
            }
            else
            {
                _logger.LogDebug("Không có closed trades mới cho SystemID={SystemId}", tradingAccountId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý closed trades cho SystemID={SystemId}: {Error}", tradingAccountId, ex.Message);
        }
    }
}
```

---

## ⚙️ **5. Service Configuration**

### **Program.cs**
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using MT5DataPusher.Core.Configuration;
using MT5DataPusher.Core.Interfaces;
using MT5DataPusher.Core.Services;
using MT5DataPusher.Service;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();

// Configure MT5 settings
builder.Services.Configure<MT5Configuration>(
    builder.Configuration.GetSection("MT5Configuration"));

// Configure HTTP Client for API calls
builder.Services.AddHttpClient<IApiService, ApiService>();

// Register services
builder.Services.AddScoped<IMT5Service, MT5Service>();
builder.Services.AddScoped<IApiService, ApiService>();

// Register background service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

try
{
    Log.Information("Starting MT5 Data Pusher Service");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

### **appsettings.json**
```json
{
  "MT5Configuration": {
    "ApiBaseUrl": "http://localhost:5047/api/v1",
    "ApiKey": "YOUR_API_KEY_HERE",
    "TimeIntervalSeconds": 3,
    "LookbackHoursClosedTrades": 24,
    "DisableSSLVerification": true,
    "RequestTimeoutSeconds": 30,
    "MaxRetries": 3,
    "RetryDelaySeconds": 5,
    "Accounts": [
      {
        "TradingAccountIdSystem": 1,
        "Login": 12345678,
        "Password": "your_mt5_password",
        "Server": "your_mt5_server",
        "Enabled": true
      }
    ]
  },
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/mt5-pusher-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

---

## 🧪 **6. Sample Unit Test**

### **MT5ServiceTests.cs**
```csharp
using Microsoft.Extensions.Logging;
using Moq;
using MT5DataPusher.Core.Services;
using Xunit;

namespace MT5DataPusher.Core.Tests.Services;

public class MT5ServiceTests
{
    private readonly Mock<ILogger<MT5Service>> _mockLogger;
    private readonly MT5Service _mt5Service;

    public MT5ServiceTests()
    {
        _mockLogger = new Mock<ILogger<MT5Service>>();
        _mt5Service = new MT5Service(_mockLogger.Object);
    }

    [Fact]
    public async Task InitializeAsync_ValidCredentials_ReturnsTrue()
    {
        // Arrange
        var login = 12345678;
        var password = "test_password";
        var server = "test_server";

        // Act
        var result = await _mt5Service.InitializeAsync(login, password, server);

        // Assert
        Assert.True(result);
        Assert.True(_mt5Service.IsConnected);
    }

    [Fact]
    public async Task GetAccountInfoAsync_WhenNotConnected_ReturnsNull()
    {
        // Act
        var result = await _mt5Service.GetAccountInfoAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetOpenPositionsAsync_WhenNotConnected_ReturnsEmptyList()
    {
        // Act
        var result = await _mt5Service.GetOpenPositionsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
```

---

## 📦 **7. Project File Example**

### **MT5DataPusher.Core.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
    <PackageReference Include="System.ComponentModel.Annotations" Version="5.0.0" />
    
    <!-- TODO: Add actual MT5 .NET library reference -->
    <!-- <PackageReference Include="MT5NETLibrary" Version="X.X.X" /> -->
  </ItemGroup>
</Project>
```

---

## 🚀 **Next Steps**

### **Immediate Actions:**
1. **🔍 Research MT5 .NET APIs** - Tìm hiểu MT5 COM API hoặc existing wrappers
2. **🏗️ Setup Project** - Tạo solution structure như sample trên
3. **🧪 Create PoC** - Implement basic MT5 connection test
4. **📝 Replace Placeholders** - Thay thế placeholder code bằng actual MT5 API calls

### **Key Areas Cần Research:**
- **MT5 COM API Documentation** - Official Microsoft COM interop
- **Existing .NET Wrappers** - Community libraries available
- **Deal History Logic** - Complex pairing algorithm for closed trades
- **Performance Optimization** - Memory và CPU efficiency

---

**Sample Code Created**: {current_date}  
**Status**: Ready for PoC Development  
**Note**: Đây là architectural preview - actual implementation sẽ depend vào MT5 .NET API research results. 