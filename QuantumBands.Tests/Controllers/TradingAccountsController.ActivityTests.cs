using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Features.TradingAccounts.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Queries;
using QuantumBands.Application.Interfaces;
using QuantumBands.Tests.Common;
using QuantumBands.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace QuantumBands.Tests.Controllers;

/// <summary>
/// Unit tests for TradingAccountsController.GetActivity endpoint covering SCRUM-100 requirements.
/// 
/// SCRUM-100 - GetActivity endpoint:
/// - Happy Path: Valid requests and successful responses
/// - Authorization: User access control, admin access
/// - Query Parameters: Activity type filtering, date filtering, pagination
/// - Data Structure: Comprehensive activity response validation
/// </summary>
public partial class TradingAccountsControllerActivityTests : TestBase
{
    private readonly Mock<ITradingAccountService> _mockTradingAccountService;
    private readonly Mock<ILogger<TradingAccountsController>> _mockLogger;
    private readonly TradingAccountsController _controller;

    public TradingAccountsControllerActivityTests()
    {
        _mockTradingAccountService = new Mock<ITradingAccountService>();
        _mockLogger = new Mock<ILogger<TradingAccountsController>>();
        _controller = new TradingAccountsController(_mockTradingAccountService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetActivity_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var accountId = 1;
        var query = new GetActivityQuery
        {
            Type = ActivityType.All,
            Page = 1,
            PageSize = 50
        };

        var expectedActivity = CreateSampleActivity();
        
        _mockTradingAccountService
            .Setup(s => s.GetActivityAsync(accountId, query, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedActivity, null));

        // Set up user claims for authorization
        _controller.ControllerContext = CreateControllerContextWithUser(userId: 1, isAdmin: false);

        // Act
        var result = await _controller.GetActivity(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedActivity);
        
        _mockTradingAccountService.Verify(
            s => s.GetActivityAsync(accountId, query, 1, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetActivity_UnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        var accountId = 1;
        var query = new GetActivityQuery();

        // Set up controller context without valid user claims
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.GetActivity(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetActivity_AccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var accountId = 999;
        var query = new GetActivityQuery();

        _mockTradingAccountService
            .Setup(s => s.GetActivityAsync(accountId, query, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Trading account with ID 999 not found"));

        _controller.ControllerContext = CreateControllerContextWithUser(userId: 1, isAdmin: false);

        // Act
        var result = await _controller.GetActivity(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetActivity_NoPermission_ReturnsForbidden()
    {
        // Arrange
        var accountId = 1;
        var query = new GetActivityQuery();

        _mockTradingAccountService
            .Setup(s => s.GetActivityAsync(accountId, query, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "You don't have permission to view this account's activity"));

        _controller.ControllerContext = CreateControllerContextWithUser(userId: 2, isAdmin: false);

        // Act
        var result = await _controller.GetActivity(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Theory]
    [InlineData(ActivityType.Deposits)]
    [InlineData(ActivityType.Withdrawals)]
    [InlineData(ActivityType.Logins)]
    [InlineData(ActivityType.Configs)]
    [InlineData(ActivityType.Trades)]
    [InlineData(ActivityType.All)]
    public async Task GetActivity_DifferentActivityTypes_ReturnsFilteredResults(ActivityType activityType)
    {
        // Arrange
        var accountId = 1;
        var query = new GetActivityQuery { Type = activityType };

        var expectedActivity = CreateSampleActivity();
        expectedActivity.Filters.Type = activityType.ToString().ToLower();

        _mockTradingAccountService
            .Setup(s => s.GetActivityAsync(accountId, query, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedActivity, null));

        _controller.ControllerContext = CreateControllerContextWithUser(userId: 1, isAdmin: false);

        // Act
        var result = await _controller.GetActivity(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var activity = okResult!.Value as AccountActivityDto;
        
        activity!.Filters.Type.Should().Be(activityType.ToString().ToLower());
    }

    [Fact]
    public async Task GetActivity_WithDateRange_ReturnsFilteredResults()
    {
        // Arrange
        var accountId = 1;
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        var query = new GetActivityQuery 
        { 
            StartDate = startDate,
            EndDate = endDate
        };

        var expectedActivity = CreateSampleActivity();
        expectedActivity.Filters.DateRange = new DateRangeDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalDays = 30,
            TradingDays = 22
        };

        _mockTradingAccountService
            .Setup(s => s.GetActivityAsync(accountId, query, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedActivity, null));

        _controller.ControllerContext = CreateControllerContextWithUser(userId: 1, isAdmin: false);

        // Act
        var result = await _controller.GetActivity(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var activity = okResult!.Value as AccountActivityDto;
        
        activity!.Filters.DateRange.Should().NotBeNull();
        activity.Filters.DateRange!.StartDate.Should().BeCloseTo(startDate, TimeSpan.FromSeconds(1));
        activity.Filters.DateRange.EndDate.Should().BeCloseTo(endDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetActivity_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var accountId = 1;
        var query = new GetActivityQuery 
        { 
            Page = 2,
            PageSize = 25
        };

        var expectedActivity = CreateSampleActivity();
        expectedActivity.Pagination.CurrentPage = 2;
        expectedActivity.Pagination.PageSize = 25;
        expectedActivity.Pagination.HasPreviousPage = true;

        _mockTradingAccountService
            .Setup(s => s.GetActivityAsync(accountId, query, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedActivity, null));

        _controller.ControllerContext = CreateControllerContextWithUser(userId: 1, isAdmin: false);

        // Act
        var result = await _controller.GetActivity(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var activity = okResult!.Value as AccountActivityDto;
        
        activity!.Pagination.CurrentPage.Should().Be(2);
        activity.Pagination.PageSize.Should().Be(25);
        activity.Pagination.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetActivity_AdminUser_CanAccessAnyAccount()
    {
        // Arrange
        var accountId = 1;
        var query = new GetActivityQuery { IncludeSystem = true };

        var expectedActivity = CreateSampleActivity();
        expectedActivity.Filters.IncludeSystem = true;

        _mockTradingAccountService
            .Setup(s => s.GetActivityAsync(accountId, query, It.IsAny<int>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedActivity, null));

        _controller.ControllerContext = CreateControllerContextWithUser(userId: 2, isAdmin: true);

        // Act
        var result = await _controller.GetActivity(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var activity = okResult!.Value as AccountActivityDto;
        
        activity!.Filters.IncludeSystem.Should().BeTrue();
        
        _mockTradingAccountService.Verify(
            s => s.GetActivityAsync(accountId, query, 2, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetActivity_WithSorting_ReturnsCorrectOrder()
    {
        // Arrange
        var accountId = 1;
        var query = new GetActivityQuery 
        { 
            SortBy = ActivitySortBy.Amount,
            SortOrder = SortOrder.Desc
        };

        var expectedActivity = CreateSampleActivity();
        // Ensure activities are in descending order by amount
        expectedActivity.Activities = new List<ActivityDto>
        {
            CreateActivityDto("deposit_1", 1000m),
            CreateActivityDto("deposit_2", 500m),
            CreateActivityDto("login_1", null)
        };

        _mockTradingAccountService
            .Setup(s => s.GetActivityAsync(accountId, query, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedActivity, null));

        _controller.ControllerContext = CreateControllerContextWithUser(userId: 1, isAdmin: false);

        // Act
        var result = await _controller.GetActivity(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var activity = okResult!.Value as AccountActivityDto;
        
        activity!.Activities.Should().HaveCount(3);
        activity.Activities[0].Details.Amount.Should().Be(1000m);
        activity.Activities[1].Details.Amount.Should().Be(500m);
        activity.Activities[2].Details.Amount.Should().BeNull();
    }

    [Fact]
    public async Task GetActivity_ServiceError_ReturnsInternalServerError()
    {
        // Arrange
        var accountId = 1;
        var query = new GetActivityQuery();

        _mockTradingAccountService
            .Setup(s => s.GetActivityAsync(accountId, query, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "An unexpected error occurred"));

        _controller.ControllerContext = CreateControllerContextWithUser(userId: 1, isAdmin: false);

        // Act
        var result = await _controller.GetActivity(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    private static AccountActivityDto CreateSampleActivity()
    {
        return new AccountActivityDto
        {
            Pagination = new PaginationDto
            {
                CurrentPage = 1,
                PageSize = 50,
                TotalPages = 1,
                TotalItems = 3,
                HasNextPage = false,
                HasPreviousPage = false
            },
            Filters = new ActivityFiltersDto
            {
                Type = "all",
                IncludeSystem = false
            },
            Activities = new List<ActivityDto>
            {
                CreateActivityDto("deposit_1", 1000m),
                CreateActivityDto("trade_1", 50m),
                CreateActivityDto("login_1", null)
            },
            Summary = new ActivitySummaryDto
            {
                TotalByType = new Dictionary<string, ActivityTypeCountDto>
                {
                    ["DEPOSIT"] = new ActivityTypeCountDto { Count = 1, TotalAmount = 1000m },
                    ["TRADE_CLOSED"] = new ActivityTypeCountDto { Count = 1, TotalAmount = 50m },
                    ["LOGIN"] = new ActivityTypeCountDto { Count = 1, TotalAmount = null }
                },
                FinancialSummary = new FinancialSummaryDto
                {
                    TotalDeposits = 1000m,
                    TotalWithdrawals = 0m,
                    NetFlow = 1000m,
                    PendingTransactions = 0
                },
                SecurityEvents = new SecurityEventsDto
                {
                    LoginAttempts = 1,
                    FailedLogins = 0,
                    PasswordChanges = 0,
                    SuspiciousActivity = 0
                },
                LastActivity = DateTime.UtcNow.AddHours(-1)
            }
        };
    }

    private static ActivityDto CreateActivityDto(string activityId, decimal? amount)
    {
        return new ActivityDto
        {
            ActivityId = activityId,
            Timestamp = DateTime.UtcNow.AddHours(-2),
            Type = activityId.Contains("deposit") ? "DEPOSIT" : 
                   activityId.Contains("trade") ? "TRADE_CLOSED" : "LOGIN",
            Category = activityId.Contains("deposit") ? "FINANCIAL" : 
                      activityId.Contains("trade") ? "TRADING" : "SECURITY",
            Description = $"Activity: {activityId}",
            Details = new ActivityDetailsDto
            {
                Amount = amount,
                Currency = amount.HasValue ? "USD" : null
            },
            Status = "COMPLETED",
            InitiatedBy = new InitiatedByDto
            {
                UserId = 1,
                UserName = "TestUser",
                Role = "USER",
                IsSystemGenerated = false
            },
            RelatedEntities = new RelatedEntitiesDto(),
            Metadata = new ActivityMetadataDto
            {
                Source = "WEB"
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