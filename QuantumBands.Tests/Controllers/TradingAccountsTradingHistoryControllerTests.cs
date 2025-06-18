using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Features.TradingAccounts.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Queries;
using QuantumBands.Application.Interfaces;
using System.Security.Claims;
using Xunit;

namespace QuantumBands.Tests.Controllers;

public class TradingAccountsTradingHistoryControllerTests
{
    private readonly Mock<ITradingAccountService> _mockTradingAccountService;
    private readonly Mock<ILogger<TradingAccountsController>> _mockLogger;
    private readonly TradingAccountsController _controller;

    public TradingAccountsTradingHistoryControllerTests()
    {
        _mockTradingAccountService = new Mock<ITradingAccountService>();
        _mockLogger = new Mock<ILogger<TradingAccountsController>>();
        _controller = new TradingAccountsController(_mockTradingAccountService.Object, _mockLogger.Object);

        // Setup user claims for authentication
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "123"),
            new(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    [Fact]
    public async Task GetTradingHistory_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var accountId = 1;
        var query = new GetTradingHistoryQuery { Page = 1, PageSize = 20 };
        var expectedHistory = new PaginatedTradingHistoryDto
        {
            Pagination = new PaginationMetadata
            {
                CurrentPage = 1,
                PageSize = 20,
                TotalPages = 1,
                TotalItems = 5,
                HasNextPage = false,
                HasPreviousPage = false
            },
            Trades = new List<TradingHistoryDto>
            {
                new()
                {
                    ClosedTradeId = 1,
                    EaTicketId = 12345,
                    Symbol = "EURUSD",
                    TradeType = "BUY",
                    VolumeLots = 0.1m,
                    OpenPrice = 1.1000m,
                    ClosePrice = 1.1050m,
                    RealizedPandL = 50.0m,
                    OpenTime = DateTime.UtcNow.AddDays(-1),
                    CloseTime = DateTime.UtcNow,
                    Commission = 1.0m,
                    Swap = 0.5m
                }
            },
            Summary = new TradingHistorySummary
            {
                FilteredTotalTrades = 5,
                FilteredTotalProfit = 250.0m,
                FilteredProfitableTrades = 3,
                FilteredLosingTrades = 2,
                FilteredWinRate = 60.0m
            }
        };

        _mockTradingAccountService
            .Setup(s => s.GetTradingHistoryAsync(accountId, query, 123, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedHistory, null));

        // Act
        var result = await _controller.GetTradingHistory(accountId, query, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedHistory = Assert.IsType<PaginatedTradingHistoryDto>(okResult.Value);
        Assert.Equal(expectedHistory.Pagination.TotalItems, returnedHistory.Pagination.TotalItems);
        Assert.Single(returnedHistory.Trades);
        Assert.Equal("EURUSD", returnedHistory.Trades.First().Symbol);
    }

    [Fact]
    public async Task GetTradingHistory_WithUnauthorizedAccess_ReturnsForbid()
    {
        // Arrange
        var accountId = 1;
        var query = new GetTradingHistoryQuery { Page = 1, PageSize = 20 };

        _mockTradingAccountService
            .Setup(s => s.GetTradingHistoryAsync(accountId, query, 123, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Unauthorized access to this trading account"));

        // Act
        var result = await _controller.GetTradingHistory(accountId, query, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetTradingHistory_WithAccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var accountId = 999;
        var query = new GetTradingHistoryQuery { Page = 1, PageSize = 20 };

        _mockTradingAccountService
            .Setup(s => s.GetTradingHistoryAsync(accountId, query, 123, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Trading account with ID 999 not found"));

        // Act
        var result = await _controller.GetTradingHistory(accountId, query, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task GetTradingHistory_WithServiceError_ReturnsInternalServerError()
    {
        // Arrange
        var accountId = 1;
        var query = new GetTradingHistoryQuery { Page = 1, PageSize = 20 };

        _mockTradingAccountService
            .Setup(s => s.GetTradingHistoryAsync(accountId, query, 123, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "An error occurred while retrieving trading history"));

        // Act
        var result = await _controller.GetTradingHistory(accountId, query, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetTradingHistory_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var accountId = 1;
        var query = new GetTradingHistoryQuery 
        { 
            Page = 1, 
            PageSize = 20,
            Symbol = "EURUSD",
            Type = "BUY",
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            MinProfit = 0,
            SortBy = "profit",
            SortOrder = "desc"
        };

        var expectedHistory = new PaginatedTradingHistoryDto
        {
            Pagination = new PaginationMetadata { CurrentPage = 1, PageSize = 20, TotalItems = 2 },
            Filters = new AppliedFilters
            {
                Symbol = "EURUSD",
                Type = "BUY",
                DateRange = new DateRange 
                { 
                    StartDate = query.StartDate, 
                    EndDate = query.EndDate 
                },
                ProfitRange = new ProfitRange { MinProfit = 0 },
                SortBy = "profit",
                SortOrder = "desc"
            },
            Trades = new List<TradingHistoryDto>(),
            Summary = new TradingHistorySummary()
        };

        _mockTradingAccountService
            .Setup(s => s.GetTradingHistoryAsync(accountId, query, 123, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedHistory, null));

        // Act
        var result = await _controller.GetTradingHistory(accountId, query, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedHistory = Assert.IsType<PaginatedTradingHistoryDto>(okResult.Value);
        Assert.NotNull(returnedHistory.Filters);
        Assert.Equal("EURUSD", returnedHistory.Filters.Symbol);
        Assert.Equal("BUY", returnedHistory.Filters.Type);
        Assert.Equal("profit", returnedHistory.Filters.SortBy);
        Assert.Equal("desc", returnedHistory.Filters.SortOrder);
    }

    [Fact]
    public async Task GetTradingHistory_AsAdmin_CanAccessAnyAccount()
    {
        // Arrange - Setup admin user
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "456"),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext.HttpContext.User = principal;

        var accountId = 1;
        var query = new GetTradingHistoryQuery { Page = 1, PageSize = 20 };
        var expectedHistory = new PaginatedTradingHistoryDto
        {
            Pagination = new PaginationMetadata { CurrentPage = 1, PageSize = 20, TotalItems = 1 },
            Trades = new List<TradingHistoryDto>(),
            Summary = new TradingHistorySummary()
        };

        _mockTradingAccountService
            .Setup(s => s.GetTradingHistoryAsync(accountId, query, 456, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedHistory, null));

        // Act
        var result = await _controller.GetTradingHistory(accountId, query, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<PaginatedTradingHistoryDto>(okResult.Value);
        
        // Verify the service was called with isAdmin = true
        _mockTradingAccountService.Verify(
            s => s.GetTradingHistoryAsync(accountId, query, 456, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}