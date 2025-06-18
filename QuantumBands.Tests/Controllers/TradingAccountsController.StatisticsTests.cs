using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Features.TradingAccounts.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Queries;
using QuantumBands.Application.Features.TradingAccounts.Enums;
using QuantumBands.Application.Interfaces;
using QuantumBands.Tests.Common;
using QuantumBands.Tests.Fixtures;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace QuantumBands.Tests.Controllers;

/// <summary>
/// Unit tests for TradingAccountsController.GetStatistics endpoint covering SCRUM-99 requirements.
/// 
/// SCRUM-99 - GetStatistics endpoint:
/// - Happy Path: Valid requests and successful responses
/// - Authorization: User access control, admin access
/// - Query Parameters: Period filtering, symbol filtering, advanced metrics
/// - Data Structure: Comprehensive statistics response validation
/// </summary>
public partial class TradingAccountsControllerStatisticsTests : TestBase
{
    private readonly Mock<ITradingAccountService> _mockTradingAccountService;
    private readonly Mock<ILogger<TradingAccountsController>> _mockLogger;
    private readonly TradingAccountsController _controller;

    public TradingAccountsControllerStatisticsTests()
    {
        _mockTradingAccountService = new Mock<ITradingAccountService>();
        _mockLogger = new Mock<ILogger<TradingAccountsController>>();
        _controller = new TradingAccountsController(_mockTradingAccountService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetStatistics_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var accountId = 1;
        var query = new GetStatisticsQuery
        {
            Period = TimePeriod.All,
            IncludeAdvanced = false
        };

        var expectedStatistics = CreateSampleStatistics();
        
        _mockTradingAccountService
            .Setup(s => s.GetStatisticsAsync(accountId, query, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedStatistics, null));

        // Set up user claims for authorization
        _controller.ControllerContext = CreateControllerContextWithUser(userId: 1, isAdmin: false);

        // Act
        var result = await _controller.GetStatistics(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedStatistics);
        
        _mockTradingAccountService.Verify(
            s => s.GetStatisticsAsync(accountId, query, 1, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStatistics_UnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        var accountId = 1;
        var query = new GetStatisticsQuery();

        // Set up controller context without valid user claims
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.GetStatistics(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetStatistics_AccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var accountId = 999;
        var query = new GetStatisticsQuery();

        _mockTradingAccountService
            .Setup(s => s.GetStatisticsAsync(accountId, query, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Trading account with ID 999 not found"));

        _controller.ControllerContext = CreateControllerContextWithUser(userId: 1, isAdmin: false);

        // Act
        var result = await _controller.GetStatistics(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetStatistics_WithAdvancedMetrics_ReturnsAdvancedData()
    {
        // Arrange
        var accountId = 1;
        var query = new GetStatisticsQuery
        {
            Period = TimePeriod.OneYear,
            IncludeAdvanced = true,
            Symbols = "EURUSD,GBPUSD",
            Benchmark = "SPY"
        };

        var expectedStatistics = CreateSampleStatistics();
        expectedStatistics.AdvancedMetrics = new AdvancedMetricsDto
        {
            SharpeRatio = 1.5m,
            SortinoRatio = 2.1m,
            InformationRatio = 0.8m,
            TreynorRatio = 0.12m,
            Alpha = 0.05m,
            Beta = 1.2m,
            RSquared = 0.85m,
            TrackingError = 0.15m,
            ValueAtRisk95 = 0.02m,
            ValueAtRisk99 = 0.05m,
            ConditionalVaR = 0.08m,
            MaxLeverageUsed = 2.5m,
            AverageLeverage = 1.8m
        };

        _mockTradingAccountService
            .Setup(s => s.GetStatisticsAsync(accountId, query, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedStatistics, null));

        _controller.ControllerContext = CreateControllerContextWithUser(userId: 1, isAdmin: true);

        // Act
        var result = await _controller.GetStatistics(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var statistics = okResult!.Value as TradingStatisticsDto;
        
        statistics!.AdvancedMetrics.Should().NotBeNull();
        statistics.AdvancedMetrics!.SharpeRatio.Should().Be(1.5m);
    }

    [Theory]
    [InlineData(TimePeriod.OneMonth)]
    [InlineData(TimePeriod.ThreeMonths)]
    [InlineData(TimePeriod.SixMonths)]
    [InlineData(TimePeriod.OneYear)]
    [InlineData(TimePeriod.All)]
    public async Task GetStatistics_DifferentPeriods_ReturnsCorrectPeriod(TimePeriod period)
    {
        // Arrange
        var accountId = 1;
        var query = new GetStatisticsQuery { Period = period };

        var expectedStatistics = CreateSampleStatistics();
        expectedStatistics.Period = period.ToString();

        _mockTradingAccountService
            .Setup(s => s.GetStatisticsAsync(accountId, query, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedStatistics, null));

        _controller.ControllerContext = CreateControllerContextWithUser(userId: 1, isAdmin: false);

        // Act
        var result = await _controller.GetStatistics(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var statistics = okResult!.Value as TradingStatisticsDto;
        
        statistics!.Period.Should().Be(period.ToString());
    }

    private static TradingStatisticsDto CreateSampleStatistics()
    {
        return new TradingStatisticsDto
        {
            Period = "All",
            DateRange = new DateRangeDto
            {
                StartDate = DateTime.UtcNow.AddYears(-1),
                EndDate = DateTime.UtcNow,
                TotalDays = 365,
                TradingDays = 260
            },
            TradingStats = new TradingStatsDto
            {
                TotalTrades = 100,
                ProfitableTrades = new TradeCountDto { Count = 60, Percentage = 60m },
                LosingTrades = new TradeCountDto { Count = 35, Percentage = 35m },
                BreakEvenTrades = new TradeCountDto { Count = 5, Percentage = 5m },
                BestTrade = 500m,
                WorstTrade = -200m,
                AverageProfit = 150m,
                AverageLoss = -80m,
                LargestProfitTrade = 500m,
                LargestLossTrade = -200m,
                MaxConsecutiveWins = 8,
                MaxConsecutiveLosses = 4,
                AverageTradeDuration = "2:30:00",
                TradesPerDay = 0.27m,
                TradesPerWeek = 1.9m,
                TradesPerMonth = 8.3m
            },
            FinancialStats = new FinancialStatsDto
            {
                GrossProfit = 9000m,
                GrossLoss = 2800m,
                TotalNetProfit = 6200m,
                ProfitFactor = 3.21m,
                ExpectedPayoff = 62m,
                AverageTradeNetProfit = 62m,
                ReturnOnInvestment = 62m,
                AnnualizedReturn = 62m,
                TotalCommission = 50m,
                TotalSwap = -15m,
                NetProfitAfterCosts = 6135m
            },
            RiskMetrics = new RiskMetricsDto
            {
                MaxDrawdown = new MaxDrawdownInfoDto
                {
                    Amount = 800m,
                    Percentage = 8.5m,
                    Date = DateTime.UtcNow.AddDays(-30),
                    Duration = "7 days",
                    RecoveryTime = "14 days"
                },
                AverageDrawdown = 2.1m,
                CalmarRatio = 7.3m,
                MaxDailyLoss = -150m,
                MaxDailyProfit = 220m,
                AverageDailyPL = 17m,
                Volatility = 0.18m,
                StandardDeviation = 0.15m,
                DownsideDeviation = 0.12m,
                RiskOfRuin = 0.02m,
                WinLossRatio = 1.71m,
                PayoffRatio = 1.88m
            },
            AdvancedMetrics = null,
            SymbolBreakdown = new List<SymbolBreakdownDto>
            {
                new SymbolBreakdownDto
                {
                    Symbol = "EURUSD",
                    Trades = 45,
                    NetProfit = 3200m,
                    WinRate = 64.4m,
                    ProfitFactor = 2.8m,
                    AverageHoldTime = "1:45:00"
                }
            },
            MonthlyPerformance = new List<MonthlyPerformanceDto>
            {
                new MonthlyPerformanceDto
                {
                    Year = 2024,
                    Month = 6,
                    Trades = 25,
                    NetProfit = 1500m,
                    WinRate = 68m,
                    MaxDrawdown = 3.2m
                }
            }
        };
    }

    private static ControllerContext CreateControllerContextWithUser(int userId, bool isAdmin)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("uid", userId.ToString())
        };

        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }
}