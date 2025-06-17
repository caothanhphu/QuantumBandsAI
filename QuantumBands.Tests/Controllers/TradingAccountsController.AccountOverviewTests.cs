// QuantumBands.Tests/Controllers/TradingAccountsController.AccountOverviewTests.cs
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Features.TradingAccounts.Dtos;
using QuantumBands.Application.Interfaces;
using QuantumBands.Tests.Common;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace QuantumBands.Tests.Controllers;

public partial class TradingAccountsControllerTests : TestBase
{
    private readonly TradingAccountsController _tradingAccountsController;
    private readonly Mock<ITradingAccountService> _mockTradingAccountService;
    private readonly Mock<ILogger<TradingAccountsController>> _mockLogger;

    public TradingAccountsControllerTests()
    {
        _mockTradingAccountService = new Mock<ITradingAccountService>();
        _mockLogger = new Mock<ILogger<TradingAccountsController>>();
        _tradingAccountsController = new TradingAccountsController(_mockTradingAccountService.Object, _mockLogger.Object);
    }

    /// <summary>
    /// Test: Valid account ID with user authorization should return account overview
    /// </summary>
    [Fact]
    public async Task GetAccountOverview_WithValidAccountIdAndUserAuth_ShouldReturnOkWithOverview()
    {
        // Arrange
        const int accountId = 1;
        const int userId = 1;
        
        var expectedOverview = new AccountOverviewDto
        {
            AccountInfo = new AccountInfoDto
            {
                AccountId = "1",
                AccountName = "Test Account",
                Login = "TEST-001",
                Server = "MT5-Server",
                AccountType = "Real",
                TradingPlatform = "MT5",
                HedgingAllowed = true,
                Leverage = 100,
                RegistrationDate = DateTime.UtcNow.AddDays(-30),
                LastActivity = DateTime.UtcNow,
                Status = "Active"
            },
            BalanceInfo = new BalanceInfoDto
            {
                CurrentBalance = 10000m,
                CurrentEquity = 10500m,
                FreeMargin = 8000m,
                MarginLevel = 150m,
                TotalDeposits = 10000m,
                TotalWithdrawals = 0m,
                TotalProfit = 500m,
                InitialDeposit = 10000m
            },
            PerformanceKPIs = new PerformanceKPIsDto
            {
                TotalTrades = 50,
                WinRate = 65.5m,
                ProfitFactor = 1.8m,
                MaxDrawdown = 5.2m,
                MaxDrawdownAmount = 520m,
                GrowthPercent = 5.0m,
                ActiveDays = 30
            }
        };

        var expectedResponse = (expectedOverview, null as string);
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = principal
        };

        _mockTradingAccountService.Setup(x => x.GetAccountOverviewAsync(accountId, userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetAccountOverview(accountId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as AccountOverviewDto;
        
        response.Should().NotBeNull();
        response!.AccountInfo.AccountId.Should().Be("1");
        response.AccountInfo.AccountName.Should().Be("Test Account");
        response.BalanceInfo.CurrentBalance.Should().Be(10000m);
        response.PerformanceKPIs.TotalTrades.Should().Be(50);
    }

    /// <summary>
    /// Test: Admin user should be able to access any account
    /// </summary>
    [Fact]
    public async Task GetAccountOverview_WithAdminUser_ShouldReturnOkWithOverview()
    {
        // Arrange
        const int accountId = 1;
        const int adminUserId = 2;
        
        var expectedOverview = new AccountOverviewDto
        {
            AccountInfo = new AccountInfoDto
            {
                AccountId = "1",
                AccountName = "Test Account",
                Login = "TEST-001",
                Server = "MT5-Server",
                AccountType = "Real",
                TradingPlatform = "MT5",
                HedgingAllowed = true,
                Leverage = 100,
                RegistrationDate = DateTime.UtcNow.AddDays(-30),
                LastActivity = DateTime.UtcNow,
                Status = "Active"
            },
            BalanceInfo = new BalanceInfoDto(),
            PerformanceKPIs = new PerformanceKPIsDto()
        };

        var expectedResponse = (expectedOverview, null as string);
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, adminUserId.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = principal
        };

        _mockTradingAccountService.Setup(x => x.GetAccountOverviewAsync(accountId, adminUserId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetAccountOverview(accountId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as AccountOverviewDto;
        
        response.Should().NotBeNull();
        response!.AccountInfo.AccountId.Should().Be("1");
    }

    /// <summary>
    /// Test: User trying to access another user's account should return Forbidden
    /// </summary>
    [Fact]
    public async Task GetAccountOverview_WithUnauthorizedUser_ShouldReturnForbidden()
    {
        // Arrange
        const int accountId = 1;
        const int userId = 2; // Different user trying to access account 1
        
        var expectedResponse = (null as AccountOverviewDto, "Unauthorized access to this trading account");
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = principal
        };

        _mockTradingAccountService.Setup(x => x.GetAccountOverviewAsync(accountId, userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetAccountOverview(accountId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    /// <summary>
    /// Test: Non-existent account should return NotFound
    /// </summary>
    [Fact]
    public async Task GetAccountOverview_WithNonExistentAccount_ShouldReturnNotFound()
    {
        // Arrange
        const int accountId = 999;
        const int userId = 1;
        
        var expectedResponse = (null as AccountOverviewDto, "Trading account with ID 999 not found");
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = principal
        };

        _mockTradingAccountService.Setup(x => x.GetAccountOverviewAsync(accountId, userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetAccountOverview(accountId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value;
        response.Should().NotBeNull();
    }

    /// <summary>
    /// Test: Invalid user authentication should return Unauthorized
    /// </summary>
    [Fact]
    public async Task GetAccountOverview_WithInvalidAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        const int accountId = 1;
        
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal() // No claims
        };

        // Act
        var result = await _tradingAccountsController.GetAccountOverview(accountId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}
