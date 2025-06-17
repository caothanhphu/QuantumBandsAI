// QuantumBands.Tests/Controllers/TradingAccountsChartDataControllerTests.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Features.TradingAccounts.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Enums;
using QuantumBands.Application.Features.TradingAccounts.Queries;
using QuantumBands.Application.Interfaces;
using Xunit;

namespace QuantumBands.Tests.Controllers;

/// <summary>
/// Unit tests for TradingAccountsController charts functionality
/// </summary>
public class TradingAccountsChartDataControllerTests
{
    private readonly Mock<ITradingAccountService> _mockTradingAccountService;
    private readonly Mock<ILogger<TradingAccountsController>> _mockLogger;
    private readonly TradingAccountsController _controller;

    public TradingAccountsChartDataControllerTests()
    {
        _mockTradingAccountService = new Mock<ITradingAccountService>();
        _mockLogger = new Mock<ILogger<TradingAccountsController>>();
        _controller = new TradingAccountsController(_mockTradingAccountService.Object, _mockLogger.Object);

        // Setup controller context with authenticated user
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "User")
        }, "test"));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
    }

    [Fact]
    public async Task GetChartsData_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var accountId = 1;
        var query = new GetChartDataQuery
        {
            Type = ChartType.Balance,
            Period = TimePeriod.OneMonth,
            Interval = DataInterval.Daily
        };

        var expectedChartData = new ChartDataDto
        {
            ChartType = ChartType.Balance,
            Period = TimePeriod.OneMonth,
            Interval = DataInterval.Daily,
            DataPoints = new List<ChartDataPointDto>
            {
                new ChartDataPointDto
                {
                    Timestamp = DateTime.UtcNow.AddDays(-30),
                    Value = 1000.00m,
                    Metadata = new ChartDataMetadataDto
                    {
                        Balance = 1000.00m,
                        Equity = 1000.00m,
                        OpenPositions = 0,
                        DailyProfit = 0.00m
                    }
                }
            },
            Summary = new ChartSummaryDto
            {
                StartValue = 1000.00m,
                EndValue = 1200.00m,
                ChangeAbsolute = 200.00m,
                ChangePercent = 20.00m,
                MaxValue = 1250.00m,
                MinValue = 950.00m,
                TotalDataPoints = 30
            }
        };

        _mockTradingAccountService
            .Setup(x => x.GetChartDataAsync(accountId, query, 1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedChartData, null));

        // Act
        var result = await _controller.GetChartsData(accountId, query, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualChartData = Assert.IsType<ChartDataDto>(okResult.Value);
        
        Assert.Equal(expectedChartData.ChartType, actualChartData.ChartType);
        Assert.Equal(expectedChartData.Period, actualChartData.Period);
        Assert.Equal(expectedChartData.Interval, actualChartData.Interval);
        Assert.Equal(expectedChartData.DataPoints.Count, actualChartData.DataPoints.Count);
        Assert.Equal(expectedChartData.Summary.TotalDataPoints, actualChartData.Summary.TotalDataPoints);
    }

    [Fact]
    public async Task GetChartsData_ServiceReturnsError_ReturnsInternalServerError()
    {
        // Arrange
        var accountId = 1;
        var query = new GetChartDataQuery
        {
            Type = ChartType.Balance,
            Period = TimePeriod.OneMonth,
            Interval = DataInterval.Daily
        };

        _mockTradingAccountService
            .Setup(x => x.GetChartDataAsync(accountId, query, 1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "An error occurred while retrieving chart data"));

        // Act
        var result = await _controller.GetChartsData(accountId, query, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetChartsData_AccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var accountId = 999;
        var query = new GetChartDataQuery
        {
            Type = ChartType.Balance,
            Period = TimePeriod.OneMonth,
            Interval = DataInterval.Daily
        };

        _mockTradingAccountService
            .Setup(x => x.GetChartDataAsync(accountId, query, 1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Trading account with ID 999 not found"));

        // Act
        var result = await _controller.GetChartsData(accountId, query, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetChartsData_UnauthorizedAccess_ReturnsForbid()
    {
        // Arrange
        var accountId = 1;
        var query = new GetChartDataQuery
        {
            Type = ChartType.Balance,
            Period = TimePeriod.OneMonth,
            Interval = DataInterval.Daily
        };

        _mockTradingAccountService
            .Setup(x => x.GetChartDataAsync(accountId, query, 1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Unauthorized access to this trading account"));

        // Act
        var result = await _controller.GetChartsData(accountId, query, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetChartsData_InvalidUser_ReturnsUnauthorized()
    {
        // Arrange
        var accountId = 1;
        var query = new GetChartDataQuery
        {
            Type = ChartType.Balance,
            Period = TimePeriod.OneMonth,
            Interval = DataInterval.Daily
        };

        // Setup controller context with invalid user
        var invalidUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "invalid"),
            new Claim(ClaimTypes.Role, "User")
        }, "test"));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = invalidUser }
        };

        // Act
        var result = await _controller.GetChartsData(accountId, query, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Theory]
    [InlineData(ChartType.Balance)]
    [InlineData(ChartType.Equity)]
    [InlineData(ChartType.Growth)]
    [InlineData(ChartType.Drawdown)]
    public async Task GetChartsData_AllChartTypes_ReturnsOkResult(ChartType chartType)
    {
        // Arrange
        var accountId = 1;
        var query = new GetChartDataQuery
        {
            Type = chartType,
            Period = TimePeriod.OneMonth,
            Interval = DataInterval.Daily
        };

        var expectedChartData = new ChartDataDto
        {
            ChartType = chartType,
            Period = TimePeriod.OneMonth,
            Interval = DataInterval.Daily,
            DataPoints = new List<ChartDataPointDto>(),
            Summary = new ChartSummaryDto
            {
                StartValue = 0m,
                EndValue = 0m,
                ChangeAbsolute = 0m,
                ChangePercent = 0m,
                MaxValue = 0m,
                MinValue = 0m,
                TotalDataPoints = 0
            }
        };

        _mockTradingAccountService
            .Setup(x => x.GetChartDataAsync(accountId, query, 1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedChartData, null));

        // Act
        var result = await _controller.GetChartsData(accountId, query, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualChartData = Assert.IsType<ChartDataDto>(okResult.Value);
        Assert.Equal(chartType, actualChartData.ChartType);
    }
}