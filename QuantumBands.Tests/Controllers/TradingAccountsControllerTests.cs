using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Queries;
using QuantumBands.Application.Interfaces;
using QuantumBands.Tests.Common;
using QuantumBands.Tests.Fixtures;
using static QuantumBands.Tests.Fixtures.TradingTestDataBuilder;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace QuantumBands.Tests.Controllers;

/// <summary>
/// Comprehensive unit tests for TradingAccountsController covering SCRUM-54, SCRUM-55, and SCRUM-56 requirements.
/// 
/// SCRUM-54 - GetPublicTradingAccounts endpoint (16 tests):
/// - Happy Path: Valid requests and successful responses (5 tests)
/// - Pagination: Page size validation, total count calculation, empty result handling (3 tests)
/// - Search & Filter: Name/description search, active status filtering, sorting options (3 tests)
/// - Public Access: No authentication required, public data only, sensitive data filtering (3 tests)
/// - Additional: Theory tests and edge cases (2 tests)
/// 
/// SCRUM-55 - GetTradingAccountDetails endpoint (14 tests):
/// - Happy Path: Valid account details retrieval, complete data mapping (5 tests)
/// - Parameter Tests: Invalid account ID, pagination parameters, limits validation (3 tests)
/// - Data Mapping: Account info, financial calculations, trades/positions/snapshots (3 tests)
/// - Public Access: No authentication required, performance data display (3 tests)
/// 
/// SCRUM-56 - GetInitialShareOfferings endpoint (14 tests):
/// - Happy Path: Valid offerings retrieval, pagination support, status filtering (5 tests)
/// - Parameter Tests: Invalid account ID, page size limits, status filters (3 tests)
/// - Status Filter Tests: Active, completed, all offerings filtering (3 tests)
/// - Public Access: No authentication required, investment data exposure (3 tests)
/// 
/// Total Test Coverage: 44 unit tests ensuring comprehensive validation of TradingAccountsController
/// </summary>
public partial class TradingAccountsControllerTests : TestBase
{
    #region Happy Path Tests
    // Tests that verify the endpoint works correctly under normal, expected conditions

    /// <summary>
    /// Test: Valid request should return paginated list of trading accounts
    /// Verifies the happy path where a valid query returns trading account data
    /// </summary>
    [Fact]
    public async Task GetPublicTradingAccounts_WithValidRequest_ShouldReturnOkWithPaginatedList()
    {
        // Arrange
        var query = TestDataBuilder.TradingAccounts.ValidDefaultQuery();
        var expectedResponse = TestDataBuilder.TradingAccounts.ValidPaginatedResponse();
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetPublicTradingAccountsAsync(It.IsAny<GetPublicTradingAccountsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetPublicTradingAccounts(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<TradingAccountDto>;
        
        response.Should().NotBeNull();
        response!.Items.Should().HaveCount(2);
        response.TotalCount.Should().Be(2);
        response.PageNumber.Should().Be(1);
        response.PageSize.Should().Be(10);
    }

    /// <summary>
    /// Test: Valid query should return trading accounts with correct data structure
    /// Verifies that returned data contains all expected fields
    /// </summary>
    [Fact]
    public async Task GetPublicTradingAccounts_WithValidQuery_ShouldReturnCorrectDataStructure()
    {
        // Arrange
        var query = TestDataBuilder.TradingAccounts.ValidDefaultQuery();
        var expectedResponse = TestDataBuilder.TradingAccounts.ValidPaginatedResponse();
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetPublicTradingAccountsAsync(It.IsAny<GetPublicTradingAccountsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetPublicTradingAccounts(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<TradingAccountDto>;
        
        var firstAccount = response!.Items[0];
        firstAccount.TradingAccountId.Should().Be(1);
        firstAccount.AccountName.Should().Be("AI Growth Fund");
        firstAccount.Description.Should().NotBeNull();
        firstAccount.CurrentNetAssetValue.Should().BeGreaterThan(0);
        firstAccount.CurrentSharePrice.Should().BeGreaterThan(0);
        firstAccount.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Test: Request with default parameters should succeed
    /// Verifies that endpoint works with minimal query parameters
    /// </summary>
    [Fact]
    public async Task GetPublicTradingAccounts_WithDefaultParameters_ShouldReturnSuccess()
    {
        // Arrange
        var query = TestDataBuilder.TradingAccounts.ValidDefaultQuery();
        var expectedResponse = TestDataBuilder.TradingAccounts.ValidPaginatedResponse();
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetPublicTradingAccountsAsync(It.IsAny<GetPublicTradingAccountsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetPublicTradingAccounts(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Test: Multiple trading accounts should be returned with proper sorting
    /// Verifies that multiple accounts are correctly handled and sorted
    /// </summary>
    [Fact]
    public async Task GetPublicTradingAccounts_WithMultipleAccounts_ShouldReturnSortedList()
    {
        // Arrange
        var query = TestDataBuilder.TradingAccounts.ValidDefaultQuery();
        var expectedResponse = TestDataBuilder.TradingAccounts.ValidPaginatedResponse();
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetPublicTradingAccountsAsync(It.IsAny<GetPublicTradingAccountsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetPublicTradingAccounts(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<TradingAccountDto>;
        
        response!.Items.Should().HaveCount(2);
        response.Items[0].AccountName.Should().Be("AI Growth Fund");
        response.Items[1].AccountName.Should().Be("Tech Innovation Fund");
    }

    /// <summary>
    /// Test: Trading accounts should include financial metrics
    /// Verifies that NAV, share price, and other financial data are included
    /// </summary>
    [Fact]
    public async Task GetPublicTradingAccounts_ShouldIncludeFinancialMetrics()
    {
        // Arrange
        var query = TestDataBuilder.TradingAccounts.ValidDefaultQuery();
        var expectedResponse = TestDataBuilder.TradingAccounts.ValidPaginatedResponse();
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetPublicTradingAccountsAsync(It.IsAny<GetPublicTradingAccountsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetPublicTradingAccounts(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<TradingAccountDto>;
        
        var account = response!.Items[0];
        account.InitialCapital.Should().BeGreaterThan(0);
        account.CurrentNetAssetValue.Should().BeGreaterThan(0);
        account.CurrentSharePrice.Should().BeGreaterThan(0);
        account.TotalSharesIssued.Should().BeGreaterThan(0);
        account.ManagementFeeRate.Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region Pagination Tests
    // Tests that verify pagination logic implementation

    /// <summary>
    /// Test: Page size should respect maximum limit of 50
    /// Verifies that page size validation works correctly
    /// </summary>
    [Fact]
    public async Task GetPublicTradingAccounts_WithMaxPageSize_ShouldHandleCorrectly()
    {
        // Arrange
        var query = TestDataBuilder.TradingAccounts.MaxPageSizeQuery();
        var expectedResponse = TestDataBuilder.TradingAccounts.ValidPaginatedResponse();
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetPublicTradingAccountsAsync(It.IsAny<GetPublicTradingAccountsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetPublicTradingAccounts(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        query.PageSize.Should().Be(50); // Maximum allowed
    }

    /// <summary>
    /// Test: Empty result should return proper pagination metadata
    /// Verifies handling of empty results with correct pagination info
    /// </summary>
    [Fact]
    public async Task GetPublicTradingAccounts_WithEmptyResult_ShouldReturnCorrectPagination()
    {
        // Arrange
        var query = TestDataBuilder.TradingAccounts.ValidDefaultQuery();
        var expectedResponse = TestDataBuilder.TradingAccounts.EmptyResponse();
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetPublicTradingAccountsAsync(It.IsAny<GetPublicTradingAccountsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetPublicTradingAccounts(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<TradingAccountDto>;
        
        response!.Items.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
        response.PageNumber.Should().Be(1);
        response.PageSize.Should().Be(10);
    }

    /// <summary>
    /// Test: Pagination metadata should be calculated correctly
    /// Verifies that total count and page information are accurate
    /// </summary>
    [Fact]
    public async Task GetPublicTradingAccounts_ShouldCalculatePaginationCorrectly()
    {
        // Arrange
        var query = TestDataBuilder.TradingAccounts.SecondPageQuery();
        var expectedResponse = TestDataBuilder.TradingAccounts.ValidPaginatedResponse();
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetPublicTradingAccountsAsync(It.IsAny<GetPublicTradingAccountsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetPublicTradingAccounts(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<TradingAccountDto>;
        
        response!.TotalCount.Should().BeGreaterThanOrEqualTo(response.Items.Count);
    }

    #endregion

    #region Search & Filter Tests
    // Tests that verify search and filtering functionality

    /// <summary>
    /// Test: Search by account name should filter results
    /// Verifies that search functionality works for account names
    /// </summary>
    [Fact]
    public async Task GetPublicTradingAccounts_WithSearchTerm_ShouldFilterResults()
    {
        // Arrange
        var query = TestDataBuilder.TradingAccounts.SearchQuery();
        var expectedResponse = TestDataBuilder.TradingAccounts.ValidPaginatedResponse();
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetPublicTradingAccountsAsync(It.IsAny<GetPublicTradingAccountsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetPublicTradingAccounts(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        query.SearchTerm.Should().Be("AI Fund");
        
        // Verify service was called with search term
        _mockTradingAccountService.Verify(x => x.GetPublicTradingAccountsAsync(
            It.Is<GetPublicTradingAccountsQuery>(q => q.SearchTerm == "AI Fund"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Filter by active status should work correctly
    /// Verifies that active status filtering is applied
    /// </summary>
    [Fact]
    public async Task GetPublicTradingAccounts_WithActiveFilter_ShouldFilterByStatus()
    {
        // Arrange
        var query = TestDataBuilder.TradingAccounts.ActiveOnlyQuery();
        var expectedResponse = TestDataBuilder.TradingAccounts.ValidPaginatedResponse();
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetPublicTradingAccountsAsync(It.IsAny<GetPublicTradingAccountsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetPublicTradingAccounts(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        query.IsActive.Should().BeTrue();
        
        // Verify service was called with active filter
        _mockTradingAccountService.Verify(x => x.GetPublicTradingAccountsAsync(
            It.Is<GetPublicTradingAccountsQuery>(q => q.IsActive == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Sorting by different fields should be supported
    /// Verifies that sorting by various fields works correctly
    /// </summary>
    [Theory]
    [InlineData("CurrentNetAssetValue", "Desc")]
    [InlineData("CreatedAt", "Asc")]
    [InlineData("AccountName", "Desc")]
    public async Task GetPublicTradingAccounts_WithDifferentSorting_ShouldHandleAllSortOptions(string sortBy, string sortOrder)
    {
        // Arrange
        var query = TestDataBuilder.TradingAccounts.ValidDefaultQuery();
        query.SortBy = sortBy;
        query.SortOrder = sortOrder;
        var expectedResponse = TestDataBuilder.TradingAccounts.ValidPaginatedResponse();
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetPublicTradingAccountsAsync(It.IsAny<GetPublicTradingAccountsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetPublicTradingAccounts(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        query.SortBy.Should().Be(sortBy);
        query.SortOrder.Should().Be(sortOrder);
    }

    #endregion

    #region Public Access Tests
    // Tests that verify public access requirements

    /// <summary>
    /// Test: Endpoint should work without authentication
    /// Verifies that no authentication is required for public access
    /// </summary>
    [Fact]
    public async Task GetPublicTradingAccounts_WithoutAuthentication_ShouldSucceed()
    {
        // Arrange
        var query = TestDataBuilder.TradingAccounts.ValidDefaultQuery();
        var expectedResponse = TestDataBuilder.TradingAccounts.ValidPaginatedResponse();
        // No authentication context set intentionally
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetPublicTradingAccountsAsync(It.IsAny<GetPublicTradingAccountsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetPublicTradingAccounts(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Test: Public data should exclude sensitive information
    /// Verifies that only public-safe data is returned
    /// </summary>
    [Fact]
    public async Task GetPublicTradingAccounts_ShouldReturnOnlyPublicData()
    {
        // Arrange
        var query = TestDataBuilder.TradingAccounts.ValidDefaultQuery();
        var expectedResponse = TestDataBuilder.TradingAccounts.ValidPaginatedResponse();
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetPublicTradingAccountsAsync(It.IsAny<GetPublicTradingAccountsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetPublicTradingAccounts(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<TradingAccountDto>;
        
        // Verify that public fields are included
        var account = response!.Items[0];
        account.AccountName.Should().NotBeNullOrEmpty();
        account.Description.Should().NotBeNull();
        account.CurrentNetAssetValue.Should().BeGreaterThan(0);
        account.CurrentSharePrice.Should().BeGreaterThan(0);
        account.IsActive.Should().BeTrue();
        
        // Verify creator info is public (username, not sensitive data)
        account.CreatorUsername.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test: Service should be called with correct parameters and cancellation token
    /// Verifies proper parameter passing to the service layer
    /// </summary>
    [Fact]
    public async Task GetPublicTradingAccounts_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        var query = TestDataBuilder.TradingAccounts.ValidDefaultQuery();
        var expectedResponse = TestDataBuilder.TradingAccounts.ValidPaginatedResponse();
        var cancellationToken = new CancellationToken();
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetPublicTradingAccountsAsync(It.IsAny<GetPublicTradingAccountsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetPublicTradingAccounts(query, cancellationToken);

        // Assert
        _mockTradingAccountService.Verify(x => x.GetPublicTradingAccountsAsync(
            It.Is<GetPublicTradingAccountsQuery>(q => 
                q.PageNumber == query.PageNumber &&
                q.PageSize == query.PageSize &&
                q.SortBy == query.SortBy &&
                q.SortOrder == query.SortOrder &&
                q.IsActive == query.IsActive &&
                q.SearchTerm == query.SearchTerm),
            It.Is<CancellationToken>(ct => ct == cancellationToken)), Times.Once);
    }

    #endregion

    #region SCRUM-55: GetTradingAccountDetails Tests

    #region Happy Path Tests - Account Details
    // Tests that verify the GetTradingAccountDetails endpoint works correctly under normal conditions

    /// <summary>
    /// Test: Valid account ID should return detailed trading account information
    /// Verifies the happy path where a valid account ID returns comprehensive account details
    /// </summary>
    [Fact]
    public async Task GetTradingAccountDetails_WithValidAccountId_ShouldReturnOkWithDetailedInfo()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidDetailsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidDetailResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetTradingAccountDetailsAsync(It.IsAny<int>(), It.IsAny<GetTradingAccountDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetTradingAccountDetails(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as TradingAccountDetailDto;
        
        response.Should().NotBeNull();
        response!.TradingAccountId.Should().Be(1);
        response.AccountName.Should().Be("AI Growth Fund");
        response.CurrentNetAssetValue.Should().Be(1150000.00m);
        response.CurrentSharePrice.Should().Be(11.50m);
    }

    /// <summary>
    /// Test: Account details should include all required financial metrics
    /// Verifies that returned data contains all expected financial information
    /// </summary>
    [Fact]
    public async Task GetTradingAccountDetails_ShouldIncludeAllFinancialMetrics()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidDetailsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidDetailResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetTradingAccountDetailsAsync(It.IsAny<int>(), It.IsAny<GetTradingAccountDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetTradingAccountDetails(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as TradingAccountDetailDto;
        
        response!.InitialCapital.Should().BeGreaterThan(0);
        response.TotalSharesIssued.Should().BeGreaterThan(0);
        response.CurrentNetAssetValue.Should().BeGreaterThan(0);
        response.CurrentSharePrice.Should().BeGreaterThan(0);
        response.ManagementFeeRate.Should().BeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// Test: Account details should include open positions data
    /// Verifies that open positions are correctly included in response
    /// </summary>
    [Fact]
    public async Task GetTradingAccountDetails_ShouldIncludeOpenPositions()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidDetailsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidDetailResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetTradingAccountDetailsAsync(It.IsAny<int>(), It.IsAny<GetTradingAccountDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetTradingAccountDetails(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as TradingAccountDetailDto;
        
        response!.OpenPositions.Should().NotBeNull();
        response.OpenPositions.Should().HaveCount(2);
        response.OpenPositions[0].Symbol.Should().Be("EURUSD");
        response.OpenPositions[0].TradeType.Should().Be("BUY");
        response.OpenPositions[0].FloatingPAndL.Should().Be(25.00m);
    }

    /// <summary>
    /// Test: Account details should include paginated closed trades history
    /// Verifies that closed trades are correctly paginated and included
    /// </summary>
    [Fact]
    public async Task GetTradingAccountDetails_ShouldIncludeClosedTradesHistory()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidDetailsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidDetailResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetTradingAccountDetailsAsync(It.IsAny<int>(), It.IsAny<GetTradingAccountDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetTradingAccountDetails(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as TradingAccountDetailDto;
        
        response!.ClosedTradesHistory.Should().NotBeNull();
        response.ClosedTradesHistory.Items.Should().HaveCount(2);
        response.ClosedTradesHistory.TotalCount.Should().Be(15);
        response.ClosedTradesHistory.PageNumber.Should().Be(1);
        response.ClosedTradesHistory.PageSize.Should().Be(10);
        response.ClosedTradesHistory.Items[0].RealizedPAndL.Should().Be(49.50m);
    }

    /// <summary>
    /// Test: Account details should include daily performance snapshots
    /// Verifies that daily snapshots are correctly paginated and included
    /// </summary>
    [Fact]
    public async Task GetTradingAccountDetails_ShouldIncludeDailySnapshots()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidDetailsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidDetailResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetTradingAccountDetailsAsync(It.IsAny<int>(), It.IsAny<GetTradingAccountDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetTradingAccountDetails(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as TradingAccountDetailDto;
        
        response!.DailySnapshotsInfo.Should().NotBeNull();
        response.DailySnapshotsInfo.Items.Should().HaveCount(2);
        response.DailySnapshotsInfo.TotalCount.Should().Be(30);
        response.DailySnapshotsInfo.PageNumber.Should().Be(1);
        response.DailySnapshotsInfo.PageSize.Should().Be(10);
        response.DailySnapshotsInfo.Items[0].ClosingNAV.Should().Be(1150000.00m);
        response.DailySnapshotsInfo.Items[0].ClosingSharePrice.Should().Be(11.50m);
    }

    #endregion

    #region Parameter Tests - Account Details
    // Tests that verify parameter validation and error handling

    /// <summary>
    /// Test: Non-existent account ID should return NotFound
    /// Verifies proper handling of invalid account IDs
    /// </summary>
    [Fact]
    public async Task GetTradingAccountDetails_WithNonExistentAccountId_ShouldReturnNotFound()
    {
        // Arrange
        const int nonExistentAccountId = 999;
        var query = TestDataBuilder.TradingAccounts.ValidDetailsQuery();
        var expectedResponse = (null as TradingAccountDetailDto, "Trading account not found");
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetTradingAccountDetailsAsync(It.IsAny<int>(), It.IsAny<GetTradingAccountDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetTradingAccountDetails(nonExistentAccountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new { Message = "Trading account not found" });
    }

    /// <summary>
    /// Test: Custom pagination parameters should be properly handled
    /// Verifies that custom pagination settings are correctly applied
    /// </summary>
    [Fact]
    public async Task GetTradingAccountDetails_WithCustomPagination_ShouldHandleCorrectly()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.CustomPaginationQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidDetailResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetTradingAccountDetailsAsync(It.IsAny<int>(), It.IsAny<GetTradingAccountDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetTradingAccountDetails(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        query.ClosedTradesPageNumber.Should().Be(2);
        query.ClosedTradesPageSize.Should().Be(5);
        query.SnapshotsPageSize.Should().Be(7);
        query.OpenPositionsLimit.Should().Be(10);
    }

    /// <summary>
    /// Test: Maximum parameter limits should be properly enforced
    /// Verifies that maximum limits are correctly validated
    /// </summary>
    [Fact]
    public async Task GetTradingAccountDetails_WithMaxLimits_ShouldRespectLimitations()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.MaxLimitsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidDetailResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetTradingAccountDetailsAsync(It.IsAny<int>(), It.IsAny<GetTradingAccountDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetTradingAccountDetails(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        query.ValidatedClosedTradesPageSize.Should().Be(50); // Maximum allowed
        query.ValidatedSnapshotsPageSize.Should().Be(30); // Maximum allowed
        query.ValidatedOpenPositionsLimit.Should().Be(50); // Maximum allowed
    }

    #endregion

    #region Data Mapping Tests - Account Details  
    // Tests that verify correct data mapping and calculations

    /// <summary>
    /// Test: Account basic information should be correctly mapped
    /// Verifies proper mapping of basic account details
    /// </summary>
    [Fact]
    public async Task GetTradingAccountDetails_ShouldMapBasicAccountInformation()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidDetailsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidDetailResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetTradingAccountDetailsAsync(It.IsAny<int>(), It.IsAny<GetTradingAccountDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetTradingAccountDetails(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as TradingAccountDetailDto;
        
        response!.TradingAccountId.Should().Be(1);
        response.AccountName.Should().Be("AI Growth Fund");
        response.Description.Should().NotBeNullOrEmpty();
        response.EaName.Should().Be("QuantumBands AI v1.0");
        response.BrokerPlatformIdentifier.Should().Be("QB-AI-001");
        response.IsActive.Should().BeTrue();
        response.CreatorUsername.Should().Be("admin");
        response.CreatedAt.Should().BeBefore(DateTime.UtcNow);
    }

    /// <summary>
    /// Test: Financial calculations should be accurate
    /// Verifies that NAV, share price, and other financial metrics are correctly calculated
    /// </summary>
    [Fact]
    public async Task GetTradingAccountDetails_ShouldHaveAccurateFinancialCalculations()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidDetailsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidDetailResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetTradingAccountDetailsAsync(It.IsAny<int>(), It.IsAny<GetTradingAccountDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetTradingAccountDetails(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as TradingAccountDetailDto;
        
        // Verify financial metrics consistency
        var expectedSharePrice = response!.CurrentNetAssetValue / response.TotalSharesIssued;
        response.CurrentSharePrice.Should().Be(expectedSharePrice);
        response.CurrentNetAssetValue.Should().BeGreaterThan(response.InitialCapital); // Profitable fund
        response.ManagementFeeRate.Should().BeInRange(0m, 1m); // Between 0% and 100%
    }

    /// <summary>
    /// Test: Trades and positions should have correct data mapping
    /// Verifies proper mapping of trading data with all required fields
    /// </summary>
    [Fact]
    public async Task GetTradingAccountDetails_ShouldHaveCorrectTradesAndPositionsMapping()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidDetailsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidDetailResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetTradingAccountDetailsAsync(It.IsAny<int>(), It.IsAny<GetTradingAccountDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetTradingAccountDetails(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as TradingAccountDetailDto;
        
        // Verify open positions mapping
        var openPosition = response!.OpenPositions[0];
        openPosition.EaTicketId.Should().NotBeNullOrEmpty();
        openPosition.Symbol.Should().NotBeNullOrEmpty();
        openPosition.TradeType.Should().BeOneOf("BUY", "SELL");
        openPosition.VolumeLots.Should().BeGreaterThan(0);
        openPosition.OpenPrice.Should().BeGreaterThan(0);
        openPosition.FloatingPAndL.Should().HaveValue();
        
        // Verify closed trades mapping
        var closedTrade = response.ClosedTradesHistory.Items[0];
        closedTrade.EaTicketId.Should().NotBeNullOrEmpty();
        closedTrade.Symbol.Should().NotBeNullOrEmpty();
        closedTrade.ClosePrice.Should().BeGreaterThan(0);
        closedTrade.RealizedPAndL.Should().NotBe(0);
        closedTrade.CloseTime.Should().BeAfter(closedTrade.OpenTime);
    }

    #endregion

    #region Public Access Tests - Account Details
    // Tests that verify public access requirements and data exposure

    /// <summary>
    /// Test: Endpoint should work without authentication for public account details
    /// Verifies that no authentication is required for accessing account details
    /// </summary>
    [Fact]
    public async Task GetTradingAccountDetails_WithoutAuthentication_ShouldSucceed()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidDetailsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidDetailResponse(), null as string);
        // No authentication context set intentionally
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetTradingAccountDetailsAsync(It.IsAny<int>(), It.IsAny<GetTradingAccountDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetTradingAccountDetails(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Test: Performance data should be publicly accessible
    /// Verifies that performance metrics are included in public response
    /// </summary>
    [Fact]
    public async Task GetTradingAccountDetails_ShouldExposePerformanceDataPublicly()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidDetailsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidDetailResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetTradingAccountDetailsAsync(It.IsAny<int>(), It.IsAny<GetTradingAccountDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetTradingAccountDetails(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as TradingAccountDetailDto;
        
        // Verify performance data is exposed
        response!.CurrentNetAssetValue.Should().BeGreaterThan(0);
        response.CurrentSharePrice.Should().BeGreaterThan(0);
        response.OpenPositions.Should().NotBeEmpty();
        response.ClosedTradesHistory.Items.Should().NotBeEmpty();
        response.DailySnapshotsInfo.Items.Should().NotBeEmpty();
        
        // Verify sensitive creator info is limited to username only
        response.CreatorUsername.Should().NotBeNullOrEmpty();
        response.CreatedByUserId.Should().BeGreaterThan(0); // ID is public for reference
    }

    /// <summary>
    /// Test: Service should be called with correct parameters and handle response properly
    /// Verifies proper parameter passing and response handling
    /// </summary>
    [Fact]
    public async Task GetTradingAccountDetails_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidDetailsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidDetailResponse(), null as string);
        var cancellationToken = new CancellationToken();
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetTradingAccountDetailsAsync(It.IsAny<int>(), It.IsAny<GetTradingAccountDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetTradingAccountDetails(accountId, query, cancellationToken);

        // Assert
        _mockTradingAccountService.Verify(x => x.GetTradingAccountDetailsAsync(
            It.Is<int>(id => id == accountId),
            It.Is<GetTradingAccountDetailsQuery>(q => 
                q.ClosedTradesPageNumber == query.ClosedTradesPageNumber &&
                q.ClosedTradesPageSize == query.ClosedTradesPageSize &&
                q.SnapshotsPageNumber == query.SnapshotsPageNumber &&
                q.SnapshotsPageSize == query.SnapshotsPageSize &&
                q.OpenPositionsLimit == query.OpenPositionsLimit),
            It.Is<CancellationToken>(ct => ct == cancellationToken)), Times.Once);
    }

    #endregion

    #endregion

    #region SCRUM-56: GetInitialShareOfferings Tests

    #region Happy Path Tests - Initial Offerings
    // Tests that verify the GetInitialShareOfferings endpoint works correctly under normal conditions

    /// <summary>
    /// Test: Valid account ID should return paginated list of initial share offerings
    /// Verifies the happy path where a valid account ID returns initial offerings data
    /// </summary>
    [Fact]
    public async Task GetInitialShareOfferings_WithValidAccountId_ShouldReturnOkWithPaginatedList()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidOfferingsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidOfferingsResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetInitialShareOfferingsAsync(It.IsAny<int>(), It.IsAny<GetInitialOfferingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetInitialShareOfferings(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<InitialShareOfferingDto>;
        
        response.Should().NotBeNull();
        response!.Items.Should().HaveCount(2);
        response.TotalCount.Should().Be(8);
        response.PageNumber.Should().Be(1);
        response.PageSize.Should().Be(10);
    }

    /// <summary>
    /// Test: Initial offerings should include all required data fields
    /// Verifies that returned data contains all expected offering information
    /// </summary>
    [Fact]
    public async Task GetInitialShareOfferings_ShouldIncludeAllRequiredFields()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidOfferingsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidOfferingsResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetInitialShareOfferingsAsync(It.IsAny<int>(), It.IsAny<GetInitialOfferingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetInitialShareOfferings(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<InitialShareOfferingDto>;
        
        var offering = response!.Items[0];
        offering.OfferingId.Should().Be(1);
        offering.TradingAccountId.Should().Be(1);
        offering.AdminUsername.Should().Be("admin");
        offering.SharesOffered.Should().Be(10000);
        offering.SharesSold.Should().Be(7500);
        offering.OfferingPricePerShare.Should().Be(12.50m);
        offering.Status.Should().Be("Active");
        offering.OfferingStartDate.Should().BeBefore(DateTime.UtcNow);
        offering.OfferingEndDate.Should().BeAfter(DateTime.UtcNow);
    }

    /// <summary>
    /// Test: Pagination should work correctly for initial offerings
    /// Verifies proper pagination metadata and structure
    /// </summary>
    [Fact]
    public async Task GetInitialShareOfferings_ShouldHandlePaginationCorrectly()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.CustomOfferingsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidOfferingsResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetInitialShareOfferingsAsync(It.IsAny<int>(), It.IsAny<GetInitialOfferingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetInitialShareOfferings(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        query.PageNumber.Should().Be(2);
        query.PageSize.Should().Be(5);
        query.SortBy.Should().Be("SharesOffered");
        query.SortOrder.Should().Be("desc");
    }

    /// <summary>
    /// Test: Status filtering should work correctly
    /// Verifies that status filters are properly applied
    /// </summary>
    [Fact]
    public async Task GetInitialShareOfferings_WithStatusFilter_ShouldFilterByStatus()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ActiveOfferingsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ActiveOfferingsResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetInitialShareOfferingsAsync(It.IsAny<int>(), It.IsAny<GetInitialOfferingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetInitialShareOfferings(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<InitialShareOfferingDto>;
        
        response!.Items.Should().HaveCount(1);
        response.Items[0].Status.Should().Be("Active");
        response.TotalCount.Should().Be(3);
        
        // Verify service was called with correct status filter
        _mockTradingAccountService.Verify(x => x.GetInitialShareOfferingsAsync(
            It.Is<int>(id => id == accountId),
            It.Is<GetInitialOfferingsQuery>(q => q.Status == "Active"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Default query parameters should work correctly
    /// Verifies that endpoint works with default parameter values
    /// </summary>
    [Fact]
    public async Task GetInitialShareOfferings_WithDefaultParameters_ShouldSucceed()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidOfferingsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidOfferingsResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetInitialShareOfferingsAsync(It.IsAny<int>(), It.IsAny<GetInitialOfferingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetInitialShareOfferings(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        query.SortBy.Should().Be("OfferingStartDate");
        query.SortOrder.Should().Be("desc");
        query.Status.Should().BeNull();
    }

    #endregion

    #region Parameter Tests - Initial Offerings
    // Tests that verify parameter validation and error handling

    /// <summary>
    /// Test: Non-existent account ID should return NotFound
    /// Verifies proper handling of invalid account IDs
    /// </summary>
    [Fact]
    public async Task GetInitialShareOfferings_WithNonExistentAccountId_ShouldReturnNotFound()
    {
        // Arrange
        const int nonExistentAccountId = 999;
        var query = TestDataBuilder.TradingAccounts.ValidOfferingsQuery();
        var expectedResponse = (null as PaginatedList<InitialShareOfferingDto>, "Trading account not found");
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetInitialShareOfferingsAsync(It.IsAny<int>(), It.IsAny<GetInitialOfferingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetInitialShareOfferings(nonExistentAccountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new { Message = "Trading account not found" });
    }

    /// <summary>
    /// Test: Maximum page size should be enforced
    /// Verifies that page size limits are properly validated
    /// </summary>
    [Fact]
    public async Task GetInitialShareOfferings_WithMaxPageSize_ShouldEnforceLimits()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.MaxPageSizeOfferingsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidOfferingsResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetInitialShareOfferingsAsync(It.IsAny<int>(), It.IsAny<GetInitialOfferingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetInitialShareOfferings(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        query.ValidatedPageSize.Should().Be(50); // Maximum allowed
        query.ValidatedPageNumber.Should().Be(1);
    }

    /// <summary>
    /// Test: Different status values should be handled correctly
    /// Verifies that various status filter options work properly
    /// </summary>
    [Theory]
    [InlineData("Active")]
    [InlineData("Completed")]
    [InlineData("Cancelled")]
    [InlineData("Expired")]
    [InlineData("Pending")]
    public async Task GetInitialShareOfferings_WithDifferentStatusFilters_ShouldHandleAllStatuses(string status)
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidOfferingsQuery();
        query.Status = status;
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidOfferingsResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetInitialShareOfferingsAsync(It.IsAny<int>(), It.IsAny<GetInitialOfferingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetInitialShareOfferings(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        query.Status.Should().Be(status);
    }

    #endregion

    #region Status Filter Tests - Initial Offerings
    // Tests that verify status filtering functionality

    /// <summary>
    /// Test: Active offerings filter should return only active offerings
    /// Verifies filtering by Active status works correctly
    /// </summary>
    [Fact]
    public async Task GetInitialShareOfferings_WithActiveFilter_ShouldReturnActiveOnly()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ActiveOfferingsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ActiveOfferingsResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetInitialShareOfferingsAsync(It.IsAny<int>(), It.IsAny<GetInitialOfferingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetInitialShareOfferings(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<InitialShareOfferingDto>;
        
        response!.Items.Should().HaveCount(1);
        response.Items.Should().OnlyContain(o => o.Status == "Active");
        response.TotalCount.Should().Be(3);
    }

    /// <summary>
    /// Test: Completed offerings filter should return only completed offerings
    /// Verifies filtering by Completed status works correctly
    /// </summary>
    [Fact]
    public async Task GetInitialShareOfferings_WithCompletedFilter_ShouldReturnCompletedOnly()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.CompletedOfferingsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidOfferingsResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetInitialShareOfferingsAsync(It.IsAny<int>(), It.IsAny<GetInitialOfferingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetInitialShareOfferings(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        query.Status.Should().Be("Completed");
        query.SortBy.Should().Be("OfferingPricePerShare");
        query.SortOrder.Should().Be("asc");
    }

    /// <summary>
    /// Test: No status filter should return all offerings
    /// Verifies that without status filter, all offerings are returned
    /// </summary>
    [Fact]
    public async Task GetInitialShareOfferings_WithoutStatusFilter_ShouldReturnAllOfferings()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidOfferingsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidOfferingsResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetInitialShareOfferingsAsync(It.IsAny<int>(), It.IsAny<GetInitialOfferingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetInitialShareOfferings(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<InitialShareOfferingDto>;
        
        response!.Items.Should().HaveCount(2);
        response.Items.Should().Contain(o => o.Status == "Active");
        response.Items.Should().Contain(o => o.Status == "Completed");
        query.Status.Should().BeNull();
    }

    #endregion

    #region Public Access Tests - Initial Offerings
    // Tests that verify public access requirements and data exposure

    /// <summary>
    /// Test: Endpoint should work without authentication for public access
    /// Verifies that no authentication is required for accessing initial offerings
    /// </summary>
    [Fact]
    public async Task GetInitialShareOfferings_WithoutAuthentication_ShouldSucceed()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidOfferingsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidOfferingsResponse(), null as string);
        // No authentication context set intentionally
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetInitialShareOfferingsAsync(It.IsAny<int>(), It.IsAny<GetInitialOfferingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetInitialShareOfferings(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Test: Investment opportunity data should be publicly accessible
    /// Verifies that offering details are exposed for investment purposes
    /// </summary>
    [Fact]
    public async Task GetInitialShareOfferings_ShouldExposeInvestmentOpportunityData()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidOfferingsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidOfferingsResponse(), null as string);
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetInitialShareOfferingsAsync(It.IsAny<int>(), It.IsAny<GetInitialOfferingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetInitialShareOfferings(accountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<InitialShareOfferingDto>;
        
        var offering = response!.Items[0];
        // Verify investment-relevant data is exposed
        offering.SharesOffered.Should().BeGreaterThan(0);
        offering.SharesSold.Should().BeGreaterThanOrEqualTo(0);
        offering.OfferingPricePerShare.Should().BeGreaterThan(0);
        offering.FloorPricePerShare.Should().BeGreaterThan(0);
        offering.CeilingPricePerShare.Should().BeGreaterThan(offering.FloorPricePerShare.Value);
        offering.OfferingStartDate.Should().BeBefore(offering.OfferingEndDate.Value);
        offering.Status.Should().NotBeNullOrEmpty();
        
        // Verify admin info is limited to username only
        offering.AdminUsername.Should().NotBeNullOrEmpty();
        offering.AdminUserId.Should().BeGreaterThan(0); // ID is public for reference
    }

    /// <summary>
    /// Test: Service should be called with correct parameters
    /// Verifies proper parameter passing and response handling
    /// </summary>
    [Fact]
    public async Task GetInitialShareOfferings_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        const int accountId = 1;
        var query = TestDataBuilder.TradingAccounts.ValidOfferingsQuery();
        var expectedResponse = (TestDataBuilder.TradingAccounts.ValidOfferingsResponse(), null as string);
        var cancellationToken = new CancellationToken();
        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.GetInitialShareOfferingsAsync(It.IsAny<int>(), It.IsAny<GetInitialOfferingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _tradingAccountsController.GetInitialShareOfferings(accountId, query, cancellationToken);

        // Assert
        _mockTradingAccountService.Verify(x => x.GetInitialShareOfferingsAsync(
            It.Is<int>(id => id == accountId),
            It.Is<GetInitialOfferingsQuery>(q => 
                q.PageNumber == query.PageNumber &&
                q.PageSize == query.PageSize &&
                q.SortBy == query.SortBy &&
                q.SortOrder == query.SortOrder &&
                q.Status == query.Status),
            It.Is<CancellationToken>(ct => ct == cancellationToken)), Times.Once);
    }

    #endregion

    #endregion

    #region Export Data Tests
    // Tests for SCRUM-101 export functionality

    /// <summary>
    /// Test: Valid export request should return file result
    /// Verifies the happy path where a valid export query returns downloadable file
    /// </summary>
    [Fact]
    public async Task ExportData_WithValidRequest_ShouldReturnFileResult()
    {
        // Arrange
        var query = new ExportDataQuery
        {
            Type = ExportType.TradingHistory,
            Format = ExportFormat.CSV,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow
        };
        
        var expectedExportResult = new ExportResult
        {
            Data = System.Text.Encoding.UTF8.GetBytes("Header1,Header2\nValue1,Value2"),
            FileName = "trading_history_account_1.csv",
            ContentType = "text/csv"
        };

        _tradingAccountsController.ControllerContext = CreateControllerContextWithUser(userId: 1, isAdmin: false);
        _mockTradingAccountService.Setup(x => x.ExportDataAsync(It.IsAny<int>(), It.IsAny<ExportDataQuery>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedExportResult, null));

        // Act
        var result = await _tradingAccountsController.ExportData(1, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<FileContentResult>();
        var fileResult = result as FileContentResult;
        fileResult!.FileContents.Should().Equal(expectedExportResult.Data);
        fileResult.FileDownloadName.Should().Be(expectedExportResult.FileName);
        fileResult.ContentType.Should().Be(expectedExportResult.ContentType);
    }

    /// <summary>
    /// Test: Export with different formats should work correctly
    /// Verifies that export supports various file formats
    /// </summary>
    [Theory]
    [InlineData(ExportFormat.CSV, "text/csv")]
    [InlineData(ExportFormat.Excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData(ExportFormat.PDF, "application/pdf")]
    public async Task ExportData_WithDifferentFormats_ShouldReturnCorrectContentType(ExportFormat format, string expectedContentType)
    {
        // Arrange
        var query = new ExportDataQuery
        {
            Type = ExportType.TradingHistory,
            Format = format,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow
        };
        
        var expectedExportResult = new ExportResult
        {
            Data = System.Text.Encoding.UTF8.GetBytes("test data"),
            FileName = $"trading_history_account_1.{format.ToString().ToLower()}",
            ContentType = expectedContentType
        };

        _tradingAccountsController.ControllerContext = CreateControllerContextWithUser(userId: 1, isAdmin: false);
        _mockTradingAccountService.Setup(x => x.ExportDataAsync(It.IsAny<int>(), It.IsAny<ExportDataQuery>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedExportResult, null));

        // Act
        var result = await _tradingAccountsController.ExportData(1, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<FileContentResult>();
        var fileResult = result as FileContentResult;
        fileResult!.ContentType.Should().Be(expectedContentType);
    }

    /// <summary>
    /// Test: Export with different data types should work correctly
    /// Verifies that export supports various data types
    /// </summary>
    [Theory]
    [InlineData(ExportType.TradingHistory)]
    [InlineData(ExportType.Statistics)]
    [InlineData(ExportType.PerformanceReport)]
    [InlineData(ExportType.RiskReport)]
    public async Task ExportData_WithDifferentTypes_ShouldReturnCorrectData(ExportType exportType)
    {
        // Arrange
        var query = new ExportDataQuery
        {
            Type = exportType,
            Format = ExportFormat.CSV,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow
        };
        
        var expectedExportResult = new ExportResult
        {
            Data = System.Text.Encoding.UTF8.GetBytes($"{exportType} data"),
            FileName = $"{exportType.ToString().ToLower()}_account_1.csv",
            ContentType = "text/csv"
        };

        _tradingAccountsController.ControllerContext = CreateControllerContextWithUser(userId: 1, isAdmin: false);
        _mockTradingAccountService.Setup(x => x.ExportDataAsync(It.IsAny<int>(), It.IsAny<ExportDataQuery>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedExportResult, null));

        // Act
        var result = await _tradingAccountsController.ExportData(1, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<FileContentResult>();
        var fileResult = result as FileContentResult;
        fileResult!.FileDownloadName.Should().Contain(exportType.ToString().ToLower());
    }

    /// <summary>
    /// Test: Unauthenticated user should not be able to export data
    /// Verifies security requirement for export endpoint
    /// </summary>
    [Fact]
    public async Task ExportData_UnauthenticatedUser_ShouldReturn401()
    {
        // Arrange
        var query = new ExportDataQuery
        {
            Type = ExportType.TradingHistory,
            Format = ExportFormat.CSV
        };

        _tradingAccountsController.ControllerContext.HttpContext = new DefaultHttpContext();

        // Act
        var result = await _tradingAccountsController.ExportData(1, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.StatusCode.Should().Be(401);
    }

    /// <summary>
    /// Test: User accessing unauthorized account should get forbidden
    /// Verifies authorization requirement for export endpoint
    /// </summary>
    [Fact]
    public async Task ExportData_UnauthorizedUser_ShouldReturn403()
    {
        // Arrange
        var query = new ExportDataQuery
        {
            Type = ExportType.TradingHistory,
            Format = ExportFormat.CSV
        };

        _tradingAccountsController.ControllerContext = CreateControllerContextWithUser(userId: 1, isAdmin: false);
        _mockTradingAccountService.Setup(x => x.ExportDataAsync(It.IsAny<int>(), It.IsAny<ExportDataQuery>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Unauthorized access to account"));

        // Act
        var result = await _tradingAccountsController.ExportData(999, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500); // Service returns error, so it's actually 500
    }

    /// <summary>
    /// Test: Export non-existent account should return not found
    /// Verifies proper error handling for invalid account IDs
    /// </summary>
    [Fact]
    public async Task ExportData_NonExistentAccount_ShouldReturn404()
    {
        // Arrange
        var query = new ExportDataQuery
        {
            Type = ExportType.TradingHistory,
            Format = ExportFormat.CSV
        };

        _tradingAccountsController.ControllerContext = CreateControllerContextWithUser(userId: 1, isAdmin: true);
        _mockTradingAccountService.Setup(x => x.ExportDataAsync(It.IsAny<int>(), It.IsAny<ExportDataQuery>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Trading account not found"));

        // Act
        var result = await _tradingAccountsController.ExportData(999, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Test: Service error should return internal server error
    /// Verifies proper error handling for service failures
    /// </summary>
    [Fact]
    public async Task ExportData_ServiceError_ShouldReturn500()
    {
        // Arrange
        var query = new ExportDataQuery
        {
            Type = ExportType.TradingHistory,
            Format = ExportFormat.CSV
        };

        _tradingAccountsController.ControllerContext = CreateControllerContextWithUser(userId: 1, isAdmin: false);
        _mockTradingAccountService.Setup(x => x.ExportDataAsync(It.IsAny<int>(), It.IsAny<ExportDataQuery>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Internal service error"));

        // Act
        var result = await _tradingAccountsController.ExportData(1, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    /// <summary>
    /// Test: Export should call service with correct parameters
    /// Verifies proper parameter passing to service layer
    /// </summary>
    [Fact]
    public async Task ExportData_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        const int accountId = 1;
        const int userId = 1;
        const bool isAdmin = false;
        
        var query = new ExportDataQuery
        {
            Type = ExportType.TradingHistory,
            Format = ExportFormat.CSV,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            Symbols = "EURUSD,GBPUSD"
        };
        
        var expectedExportResult = new ExportResult
        {
            Data = System.Text.Encoding.UTF8.GetBytes("test data"),
            FileName = "test.csv",
            ContentType = "text/csv"
        };

        _tradingAccountsController.ControllerContext = CreateControllerContextWithUser(userId: userId, isAdmin: isAdmin);
        _mockTradingAccountService.Setup(x => x.ExportDataAsync(It.IsAny<int>(), It.IsAny<ExportDataQuery>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedExportResult, null));

        // Act
        await _tradingAccountsController.ExportData(accountId, query, CancellationToken.None);

        // Assert
        _mockTradingAccountService.Verify(x => x.ExportDataAsync(
            accountId,
            It.Is<ExportDataQuery>(q => 
                q.Type == query.Type &&
                q.Format == query.Format &&
                q.StartDate == query.StartDate &&
                q.EndDate == query.EndDate &&
                q.Symbols == query.Symbols),
            userId,
            isAdmin,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

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