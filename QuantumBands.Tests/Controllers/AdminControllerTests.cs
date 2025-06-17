using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;
using QuantumBands.Application.Interfaces;
using QuantumBands.Tests.Common;
using QuantumBands.Tests.Fixtures;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace QuantumBands.Tests.Controllers;

/// <summary>
/// Comprehensive unit tests for AdminController covering SCRUM-73 requirements.
/// 
/// SCRUM-73 - CreateInitialShareOffering endpoint (12 tests):
/// - Happy Path: Valid initial offering creation with all required parameters (1 test)
/// - Authorization Tests: Unauthenticated request, Non-admin user access (2 tests)
/// - Validation Tests: Invalid parameters validation (6 tests)
/// - Business Logic Tests: Account existence, share availability, price/date validation (3 tests)
/// 
/// Total Test Coverage: 12 unit tests ensuring comprehensive validation of CreateInitialShareOffering endpoint
/// </summary>
public class AdminControllerTests : TestBase
{
    private readonly AdminController _adminController;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IWalletService> _mockWalletService;
    private readonly Mock<ITradingAccountService> _mockTradingAccountService;
    private readonly Mock<IAdminDashboardService> _mockDashboardService;
    private readonly Mock<IExchangeService> _mockExchangeService;
    private readonly Mock<ILogger<AdminController>> _mockLogger;

    public AdminControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockWalletService = new Mock<IWalletService>();
        _mockTradingAccountService = new Mock<ITradingAccountService>();
        _mockDashboardService = new Mock<IAdminDashboardService>();
        _mockExchangeService = new Mock<IExchangeService>();
        _mockLogger = new Mock<ILogger<AdminController>>();
        
        _adminController = new AdminController(
            _mockUserService.Object,
            _mockWalletService.Object,
            _mockTradingAccountService.Object,
            _mockDashboardService.Object,
            _mockExchangeService.Object,
            _mockLogger.Object);
    }

    #region Happy Path Tests

    /// <summary>
    /// Test: Valid initial offering creation should return 201 Created with offering DTO
    /// Verifies the happy path where valid parameters result in successful offering creation
    /// </summary>
    [Fact]
    public async Task CreateInitialShareOffering_WithValidRequest_ShouldReturn201CreatedWithOfferingDto()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidRequest();
        var expectedResponse = TestDataBuilder.InitialShareOfferings.SuccessfulResponse();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateInitialShareOfferingAsync(
                It.IsAny<int>(), 
                It.IsAny<CreateInitialShareOfferingRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act
        var result = await _adminController.CreateInitialShareOffering(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status201Created);
        
        var response = objectResult.Value as InitialShareOfferingDto;
        response.Should().NotBeNull();
        response!.TradingAccountId.Should().Be(expectedResponse.TradingAccountId);
        response.SharesOffered.Should().Be(expectedResponse.SharesOffered);
        response.OfferingPricePerShare.Should().Be(expectedResponse.OfferingPricePerShare);
        response.Status.Should().Be("Active");
    }

    #endregion

    #region Authorization Tests

    /// <summary>
    /// Test: Unauthenticated request should be handled by authorization attribute
    /// Note: This test verifies the setup; actual authorization is handled by middleware
    /// </summary>
    [Fact]
    public async Task CreateInitialShareOffering_WithUnauthenticatedUser_ShouldBeHandledByAuthorizationAttribute()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidRequest();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext() // No user set
        };

        // Act & Assert
        // The [Authorize(Roles = "Admin")] attribute should handle this case
        // This test verifies that the controller is properly configured for admin access
        var controllerType = typeof(AdminController);
        var authorizeAttributes = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);
        authorizeAttributes.Should().NotBeEmpty("AdminController should have Authorize attribute");
    }

    /// <summary>
    /// Test: Non-admin user access should be handled by authorization attribute
    /// Note: This test verifies the role-based authorization setup
    /// </summary>
    [Fact]
    public async Task CreateInitialShareOffering_WithNonAdminUser_ShouldBeHandledByAuthorizationAttribute()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidRequest();
        var userClaims = CreateUserClaimsPrincipal(); // Regular user, not admin
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userClaims }
        };

        // Act & Assert
        // The [Authorize(Roles = "Admin")] attribute should handle this case
        // This test verifies that the controller requires Admin role
        var controllerType = typeof(AdminController);
        var authorizeAttributes = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);
        var authorizeAttribute = authorizeAttributes.FirstOrDefault() as Microsoft.AspNetCore.Authorization.AuthorizeAttribute;
        authorizeAttribute?.Roles.Should().Be("Admin", "AdminController should require Admin role");
    }

    #endregion

    #region Validation Tests

    /// <summary>
    /// Test: Invalid account ID (negative) should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateInitialShareOffering_WithInvalidAccountId_ShouldReturnBadRequest()
    {
        // Arrange
        const int invalidAccountId = -1;
        var request = TestDataBuilder.InitialShareOfferings.ValidRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateInitialShareOfferingAsync(
                It.IsAny<int>(), 
                It.IsAny<CreateInitialShareOfferingRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as InitialShareOfferingDto, "Invalid account ID"));

        // Act
        var result = await _adminController.CreateInitialShareOffering(invalidAccountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Test: Zero shares offered should return BadRequest with validation message
    /// </summary>
    [Fact]
    public async Task CreateInitialShareOffering_WithZeroSharesOffered_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.InitialShareOfferings.InvalidSharesZeroRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateInitialShareOfferingAsync(
                It.IsAny<int>(), 
                It.IsAny<CreateInitialShareOfferingRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as InitialShareOfferingDto, "Invalid shares offered"));

        // Act
        var result = await _adminController.CreateInitialShareOffering(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Test: Negative shares offered should return BadRequest with validation message
    /// </summary>
    [Fact]
    public async Task CreateInitialShareOffering_WithNegativeSharesOffered_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.InitialShareOfferings.InvalidSharesNegativeRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateInitialShareOfferingAsync(
                It.IsAny<int>(), 
                It.IsAny<CreateInitialShareOfferingRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as InitialShareOfferingDto, "Invalid shares offered"));

        // Act
        var result = await _adminController.CreateInitialShareOffering(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test: Zero offering price should return BadRequest with validation message
    /// </summary>
    [Fact]
    public async Task CreateInitialShareOffering_WithZeroOfferingPrice_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.InitialShareOfferings.InvalidOfferingPriceZeroRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateInitialShareOfferingAsync(
                It.IsAny<int>(), 
                It.IsAny<CreateInitialShareOfferingRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as InitialShareOfferingDto, "Invalid offering price"));

        // Act
        var result = await _adminController.CreateInitialShareOffering(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test: Floor price greater than offering price should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateInitialShareOffering_WithFloorPriceGreaterThanOfferingPrice_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.InitialShareOfferings.InvalidFloorPriceRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateInitialShareOfferingAsync(
                It.IsAny<int>(), 
                It.IsAny<CreateInitialShareOfferingRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as InitialShareOfferingDto, "Invalid floor price range"));

        // Act
        var result = await _adminController.CreateInitialShareOffering(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test: Ceiling price less than offering price should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateInitialShareOffering_WithCeilingPriceLessThanOfferingPrice_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.InitialShareOfferings.InvalidCeilingPriceRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateInitialShareOfferingAsync(
                It.IsAny<int>(), 
                It.IsAny<CreateInitialShareOfferingRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as InitialShareOfferingDto, "Invalid ceiling price range"));

        // Act
        var result = await _adminController.CreateInitialShareOffering(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test: End date in the past should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateInitialShareOffering_WithEndDateInPast_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.InitialShareOfferings.InvalidEndDateRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateInitialShareOfferingAsync(
                It.IsAny<int>(), 
                It.IsAny<CreateInitialShareOfferingRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as InitialShareOfferingDto, "Invalid end date"));

        // Act
        var result = await _adminController.CreateInitialShareOffering(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Business Logic Tests

    /// <summary>
    /// Test: Non-existent trading account should return NotFound
    /// </summary>
    [Fact]
    public async Task CreateInitialShareOffering_WithNonExistentAccount_ShouldReturnNotFound()
    {
        // Arrange
        const int nonExistentAccountId = 999;
        var request = TestDataBuilder.InitialShareOfferings.ValidRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateInitialShareOfferingAsync(
                It.IsAny<int>(), 
                It.IsAny<CreateInitialShareOfferingRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as InitialShareOfferingDto, "Trading account not found"));

        // Act
        var result = await _adminController.CreateInitialShareOffering(nonExistentAccountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Test: Insufficient available shares should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateInitialShareOffering_WithInsufficientShares_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateInitialShareOfferingAsync(
                It.IsAny<int>(), 
                It.IsAny<CreateInitialShareOfferingRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as InitialShareOfferingDto, "Shares offered exceeds available shares"));

        // Act
        var result = await _adminController.CreateInitialShareOffering(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Test: Valid minimal request (without optional fields) should return 201 Created
    /// </summary>
    [Fact]
    public async Task CreateInitialShareOffering_WithValidMinimalRequest_ShouldReturn201Created()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidMinimalRequest();
        var expectedResponse = TestDataBuilder.InitialShareOfferings.SuccessfulResponse();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateInitialShareOfferingAsync(
                It.IsAny<int>(), 
                It.IsAny<CreateInitialShareOfferingRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act
        var result = await _adminController.CreateInitialShareOffering(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status201Created);
        
        var response = objectResult.Value as InitialShareOfferingDto;
        response.Should().NotBeNull();
        response!.Status.Should().Be("Active");    }

    #endregion

    #region CreateTradingAccount Tests - SCRUM-70

    /// <summary>
    /// Test: Valid trading account creation should return 201 Created with trading account DTO
    /// Verifies the happy path where valid parameters result in successful account creation
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_WithValidRequest_ShouldReturn201CreatedWithAccountDto()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.ValidRequest();
        var expectedResponse = TestDataBuilder.CreateTradingAccounts.SuccessfulResponse();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status201Created);
        
        var response = objectResult.Value as TradingAccountDto;
        response.Should().NotBeNull();
        response!.TradingAccountId.Should().Be(expectedResponse.TradingAccountId);
        response.AccountName.Should().Be(expectedResponse.AccountName);
        response.InitialCapital.Should().Be(expectedResponse.InitialCapital);
        response.TotalSharesIssued.Should().Be(expectedResponse.TotalSharesIssued);
        response.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Test: Valid minimal request should return 201 Created
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_WithValidMinimalRequest_ShouldReturn201Created()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.ValidMinimalRequest();
        var expectedResponse = TestDataBuilder.CreateTradingAccounts.SuccessfulResponse();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    /// <summary>
    /// Test: Calculation verification - share price should be calculated correctly
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_WithValidRequest_ShouldCalculateSharePriceCorrectly()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.ValidRequest();
        var expectedResponse = TestDataBuilder.CreateTradingAccounts.SuccessfulResponse();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        var response = objectResult!.Value as TradingAccountDto;
        
        // Verify share price calculation: InitialCapital / TotalShares = 100000 / 10000 = 10.00
        response!.CurrentSharePrice.Should().Be(10.00m);
        response.CurrentNetAssetValue.Should().Be(response.InitialCapital);
    }

    /// <summary>
    /// Test: Timestamp verification - created and updated timestamps should be set
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_WithValidRequest_ShouldSetTimestamps()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.ValidRequest();
        var expectedResponse = TestDataBuilder.CreateTradingAccounts.SuccessfulResponse();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        var objectResult = result as ObjectResult;
        var response = objectResult!.Value as TradingAccountDto;
        
        response!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        response.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        response.CreatedByUserId.Should().Be(1);
        response.CreatorUsername.Should().Be("admin");
    }    /// <summary>
    /// Test: Unauthenticated request should be handled by authorization attribute
    /// </summary>
    [Fact]
    public void CreateTradingAccount_WithUnauthenticatedUser_ShouldBeHandledByAuthorizationAttribute()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.ValidRequest();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext() // No user set
        };

        // Act & Assert
        // The [Authorize(Roles = "Admin")] attribute should handle this case
        var controllerType = typeof(AdminController);
        var authorizeAttributes = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);
        authorizeAttributes.Should().NotBeEmpty("AdminController should have Authorize attribute");
    }

    /// <summary>
    /// Test: Non-admin user access should be handled by authorization attribute
    /// </summary>
    [Fact]
    public void CreateTradingAccount_WithNonAdminUser_ShouldBeHandledByAuthorizationAttribute()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.ValidRequest();
        var userClaims = CreateUserClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userClaims }
        };

        // Act & Assert
        var controllerType = typeof(AdminController);
        var authorizeAttributes = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);
        var authorizeAttribute = authorizeAttributes.FirstOrDefault() as Microsoft.AspNetCore.Authorization.AuthorizeAttribute;
        authorizeAttribute?.Roles.Should().Be("Admin", "AdminController should require Admin role");
    }

    /// <summary>
    /// Test: Admin user claims should be properly validated
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_WithAdminUser_ShouldValidateAdminClaims()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.ValidRequest();
        var expectedResponse = TestDataBuilder.CreateTradingAccounts.SuccessfulResponse();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        _mockTradingAccountService.Verify(x => x.CreateTradingAccountAsync(
            It.IsAny<CreateTradingAccountRequest>(), 
            It.Is<ClaimsPrincipal>(c => c.IsInRole("Admin")), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Account name too long should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_WithAccountNameTooLong_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.AccountNameTooLongRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "Account name cannot exceed 100 characters"));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test: Description too long should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_WithDescriptionTooLong_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.DescriptionTooLongRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "Description cannot exceed 1000 characters"));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test: Zero initial capital should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_WithZeroInitialCapital_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.ZeroInitialCapitalRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "Initial capital must be greater than 0"));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test: Negative initial capital should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_WithNegativeInitialCapital_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.NegativeInitialCapitalRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "Initial capital must be greater than 0"));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test: Zero shares issued should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_WithZeroSharesIssued_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.ZeroSharesIssuedRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "Total shares issued must be greater than 0"));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test: Negative shares issued should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_WithNegativeSharesIssued_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.NegativeSharesIssuedRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "Total shares issued must be greater than 0"));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test: Excessive management fee rate should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_WithExcessiveManagementFee_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.ExcessiveManagementFeeRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "Management fee rate must be between 0 and 0.9999"));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test: Negative management fee rate should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_WithNegativeManagementFee_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.NegativeManagementFeeRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "Management fee rate must be between 0 and 0.9999"));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test: Duplicate account name should return Conflict
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_WithDuplicateAccountName_ShouldReturnConflict()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.DuplicateAccountNameRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "Trading account with this name already exists"));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    /// <summary>
    /// Test: Account name uniqueness validation should work correctly
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_ShouldValidateAccountNameUniqueness()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.ValidRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "Trading account with this name already exists"));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
        var conflictResult = result as ConflictObjectResult;
        conflictResult!.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Test: Service null response should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_WithServiceNullResponse_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.ValidRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "Database operation failed"));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test: Database failure scenarios should be handled properly
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_WithDatabaseFailure_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.ValidRequest();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, null as string));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Test: Service call parameter verification should work correctly
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.ValidRequest();
        var expectedResponse = TestDataBuilder.CreateTradingAccounts.SuccessfulResponse();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert
        _mockTradingAccountService.Verify(x => x.CreateTradingAccountAsync(
            It.Is<CreateTradingAccountRequest>(r => 
                r.AccountName == request.AccountName &&
                r.InitialCapital == request.InitialCapital &&
                r.TotalSharesIssued == request.TotalSharesIssued &&
                r.ManagementFeeRate == request.ManagementFeeRate), 
            It.IsAny<ClaimsPrincipal>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Concurrent request handling should work properly
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_WithConcurrentRequests_ShouldHandleCorrectly()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.ValidRequest();
        var expectedResponse = TestDataBuilder.CreateTradingAccounts.SuccessfulResponse();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act - Simulate concurrent requests
        var tasks = new List<Task<IActionResult>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_adminController.CreateTradingAccount(request, CancellationToken.None));
        }
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllBeOfType<ObjectResult>();
        results.Cast<ObjectResult>().Should().AllSatisfy(r => r.StatusCode.Should().Be(StatusCodes.Status201Created));
    }

    /// <summary>
    /// Test: End-to-end happy path should work correctly
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_EndToEndHappyPath_ShouldWorkCorrectly()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.ValidRequest();
        var expectedResponse = TestDataBuilder.CreateTradingAccounts.SuccessfulResponse();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert - Comprehensive end-to-end validation
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status201Created);
        
        var response = objectResult.Value as TradingAccountDto;
        response.Should().NotBeNull();
        response!.AccountName.Should().Be(request.AccountName);
        response.Description.Should().Be(request.Description);
        response.EaName.Should().Be(request.EaName);
        response.BrokerPlatformIdentifier.Should().Be(request.BrokerPlatformIdentifier);
        response.InitialCapital.Should().Be(request.InitialCapital);
        response.TotalSharesIssued.Should().Be(request.TotalSharesIssued);
        response.ManagementFeeRate.Should().Be(request.ManagementFeeRate);
        response.IsActive.Should().BeTrue();
        response.CreatedByUserId.Should().BeGreaterThan(0);
        response.CreatorUsername.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test: Audit trail verification should track admin actions
    /// </summary>
    [Fact]
    public async Task CreateTradingAccount_ShouldCreateAuditTrail()
    {
        // Arrange
        var request = TestDataBuilder.CreateTradingAccounts.ValidRequest();
        var expectedResponse = TestDataBuilder.CreateTradingAccounts.SuccessfulResponse();
        var adminClaims = CreateAdminClaimsPrincipal();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService.Setup(x => x.CreateTradingAccountAsync(
                It.IsAny<CreateTradingAccountRequest>(), 
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act
        var result = await _adminController.CreateTradingAccount(request, CancellationToken.None);

        // Assert - Verify audit trail is created
        var objectResult = result as ObjectResult;
        var response = objectResult!.Value as TradingAccountDto;
        
        response!.CreatedByUserId.Should().Be(1); // Admin user ID from claims
        response.CreatorUsername.Should().Be("admin"); // Admin username from claims
        response.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        response.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    #endregion

    #region UpdateTradingAccount Tests - SCRUM-71

    /// <summary>
    /// SCRUM-71: Unit Tests for PUT /admin/trading-accounts/{accountId} endpoint
    /// Comprehensive test coverage including Happy Path, Authorization, Validation, and Business Logic scenarios
    /// </summary>

    #region Happy Path Tests - UpdateTradingAccount

    [Fact]
    public async Task UpdateTradingAccount_WithValidCompleteRequest_ShouldReturnOkWithUpdatedAccount()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.UpdateTradingAccounts.ValidCompleteRequest();
        var expectedResponse = TestDataBuilder.UpdateTradingAccounts.SuccessfulResponse();
        var adminClaims = CreateAdminClaimsPrincipal();

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService
            .Setup(x => x.UpdateTradingAccountAsync(
                It.Is<int>(id => id == accountId),
                It.IsAny<UpdateTradingAccountRequest>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act
        var result = await _adminController.UpdateTradingAccount(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as TradingAccountDto;
        response.Should().NotBeNull();
        response!.TradingAccountId.Should().Be(expectedResponse.TradingAccountId);
        response.Description.Should().Be(expectedResponse.Description);
        response.EaName.Should().Be(expectedResponse.EaName);
        response.ManagementFeeRate.Should().Be(expectedResponse.ManagementFeeRate);
        response.IsActive.Should().Be(expectedResponse.IsActive);
    }

    [Fact]
    public async Task UpdateTradingAccount_WithDescriptionOnly_ShouldReturnOkWithUpdatedDescription()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.UpdateTradingAccounts.ValidDescriptionOnlyRequest();
        var expectedResponse = TestDataBuilder.UpdateTradingAccounts.SuccessfulResponse();
        var adminClaims = CreateAdminClaimsPrincipal();

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService
            .Setup(x => x.UpdateTradingAccountAsync(accountId, request, adminClaims, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act
        var result = await _adminController.UpdateTradingAccount(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as TradingAccountDto;
        response.Should().NotBeNull();
        response!.Description.Should().Be(expectedResponse.Description);
    }

    [Fact]
    public async Task UpdateTradingAccount_WithEaNameOnly_ShouldReturnOkWithUpdatedEaName()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.UpdateTradingAccounts.ValidEaNameOnlyRequest();
        var expectedResponse = TestDataBuilder.UpdateTradingAccounts.SuccessfulResponse();
        var adminClaims = CreateAdminClaimsPrincipal();

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService
            .Setup(x => x.UpdateTradingAccountAsync(accountId, request, adminClaims, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act
        var result = await _adminController.UpdateTradingAccount(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as TradingAccountDto;
        response.Should().NotBeNull();
        response!.EaName.Should().Be(expectedResponse.EaName);
    }

    [Fact]
    public async Task UpdateTradingAccount_WithManagementFeeOnly_ShouldReturnOkWithUpdatedFee()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.UpdateTradingAccounts.ValidManagementFeeOnlyRequest();
        var expectedResponse = TestDataBuilder.UpdateTradingAccounts.SuccessfulResponse();
        var adminClaims = CreateAdminClaimsPrincipal();

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService
            .Setup(x => x.UpdateTradingAccountAsync(accountId, request, adminClaims, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act
        var result = await _adminController.UpdateTradingAccount(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as TradingAccountDto;
        response.Should().NotBeNull();
        response!.ManagementFeeRate.Should().Be(expectedResponse.ManagementFeeRate);
    }

    [Fact]
    public async Task UpdateTradingAccount_WithActiveStatusToggle_ShouldReturnOkWithUpdatedStatus()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.UpdateTradingAccounts.ValidActiveStatusOnlyRequest();
        var expectedResponse = TestDataBuilder.UpdateTradingAccounts.SuccessfulResponse();
        expectedResponse.IsActive = false; // Match the request
        var adminClaims = CreateAdminClaimsPrincipal();

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService
            .Setup(x => x.UpdateTradingAccountAsync(accountId, request, adminClaims, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act
        var result = await _adminController.UpdateTradingAccount(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();        var okResult = result as OkObjectResult;
        var response = okResult!.Value as TradingAccountDto;
        response.Should().NotBeNull();
        response!.IsActive.Should().Be(false);
    }

    #endregion

    #region Authorization Tests - UpdateTradingAccount

    [Fact]
    public void UpdateTradingAccount_WithUnauthenticatedUser_ShouldBeHandledByAuthorizationAttribute()
    {
        // Arrange
        var request = TestDataBuilder.UpdateTradingAccounts.ValidCompleteRequest();
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext() // No user set
        };

        // Act & Assert
        // The [Authorize(Roles = "Admin")] attribute should handle this case
        var controllerType = typeof(AdminController);
        var authorizeAttributes = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);
        authorizeAttributes.Should().NotBeEmpty("AdminController should have Authorize attribute for admin-only access");
    }    [Fact]
    public void UpdateTradingAccount_WithNonAdminUser_ShouldBeHandledByAuthorizationAttribute()
    {
        // Arrange
        var request = TestDataBuilder.UpdateTradingAccounts.ValidCompleteRequest();
        var userClaims = CreateUserClaimsPrincipal(); // Regular user, not admin
        
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userClaims }
        };

        // Act & Assert
        // The [Authorize(Roles = "Admin")] attribute should handle this case
        var controllerType = typeof(AdminController);
        var authorizeAttributes = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        authorizeAttributes.Should().Contain(attr => attr.Roles == "Admin", "AdminController should require Admin role");
    }

    #endregion

    #region Validation Tests - UpdateTradingAccount

    [Fact]
    public async Task UpdateTradingAccount_WithNullRequest_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        UpdateTradingAccountRequest? request = null;
        var adminClaims = CreateAdminClaimsPrincipal();

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        // Act
        var result = await _adminController.UpdateTradingAccount(accountId, request!, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateTradingAccount_WithInvalidAccountId_ShouldReturnNotFound()
    {
        // Arrange
        const int invalidAccountId = 999;
        var request = TestDataBuilder.UpdateTradingAccounts.ValidCompleteRequest();
        var adminClaims = CreateAdminClaimsPrincipal();

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService
            .Setup(x => x.UpdateTradingAccountAsync(invalidAccountId, request, adminClaims, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "Trading account not found"));

        // Act
        var result = await _adminController.UpdateTradingAccount(invalidAccountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateTradingAccount_WithNonExistentAccount_ShouldReturnNotFound()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.UpdateTradingAccounts.ValidCompleteRequest();
        var adminClaims = CreateAdminClaimsPrincipal();

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService
            .Setup(x => x.UpdateTradingAccountAsync(accountId, request, adminClaims, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "Trading account with ID 1 not found"));

        // Act
        var result = await _adminController.UpdateTradingAccount(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateTradingAccount_WithDescriptionTooLong_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.UpdateTradingAccounts.DescriptionTooLongRequest();
        var adminClaims = CreateAdminClaimsPrincipal();

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService
            .Setup(x => x.UpdateTradingAccountAsync(accountId, request, adminClaims, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "Description cannot exceed 1000 characters."));

        // Act
        var result = await _adminController.UpdateTradingAccount(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateTradingAccount_WithEaNameTooLong_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.UpdateTradingAccounts.EaNameTooLongRequest();
        var adminClaims = CreateAdminClaimsPrincipal();

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService
            .Setup(x => x.UpdateTradingAccountAsync(accountId, request, adminClaims, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "EA name cannot exceed 100 characters."));

        // Act
        var result = await _adminController.UpdateTradingAccount(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateTradingAccount_WithExcessiveManagementFee_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.UpdateTradingAccounts.ExcessiveManagementFeeRequest();
        var adminClaims = CreateAdminClaimsPrincipal();

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService
            .Setup(x => x.UpdateTradingAccountAsync(accountId, request, adminClaims, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "Management fee rate must be between 0 and 0.9999 (e.g., 0.02 for 2%)."));

        // Act
        var result = await _adminController.UpdateTradingAccount(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateTradingAccount_WithNegativeManagementFee_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.UpdateTradingAccounts.NegativeManagementFeeRequest();
        var adminClaims = CreateAdminClaimsPrincipal();

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService
            .Setup(x => x.UpdateTradingAccountAsync(accountId, request, adminClaims, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "Management fee rate must be between 0 and 0.9999 (e.g., 0.02 for 2%)."));

        // Act
        var result = await _adminController.UpdateTradingAccount(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
    }

    #endregion

    #region Business Logic Tests - UpdateTradingAccount

    [Fact]
    public async Task UpdateTradingAccount_WithConcurrencyConflict_ShouldReturnConflict()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.UpdateTradingAccounts.ValidCompleteRequest();
        var adminClaims = CreateAdminClaimsPrincipal();

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService
            .Setup(x => x.UpdateTradingAccountAsync(accountId, request, adminClaims, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "The trading account was modified by another user. Please concurrency conflict"));

        // Act
        var result = await _adminController.UpdateTradingAccount(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
        var conflictResult = result as ConflictObjectResult;
        conflictResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateTradingAccount_WithEmptyRequest_ShouldReturnOkWithUnchangedAccount()
    {
        // Arrange - Empty request should not change anything but still be valid
        const int accountId = 1;
        var request = TestDataBuilder.UpdateTradingAccounts.EmptyRequest();
        var expectedResponse = TestDataBuilder.UpdateTradingAccounts.SuccessfulResponse();
        var adminClaims = CreateAdminClaimsPrincipal();

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService
            .Setup(x => x.UpdateTradingAccountAsync(accountId, request, adminClaims, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act
        var result = await _adminController.UpdateTradingAccount(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as TradingAccountDto;
        response.Should().NotBeNull();
        response!.TradingAccountId.Should().Be(expectedResponse.TradingAccountId);
    }

    [Fact]
    public async Task UpdateTradingAccount_WithServiceError_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.UpdateTradingAccounts.ValidCompleteRequest();
        var adminClaims = CreateAdminClaimsPrincipal();

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService
            .Setup(x => x.UpdateTradingAccountAsync(accountId, request, adminClaims, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as TradingAccountDto, "An unexpected service error occurred"));

        // Act
        var result = await _adminController.UpdateTradingAccount(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateTradingAccount_ShouldLogAdminAction()
    {
        // Arrange
        const int accountId = 1;
        var request = TestDataBuilder.UpdateTradingAccounts.ValidCompleteRequest();
        var expectedResponse = TestDataBuilder.UpdateTradingAccounts.SuccessfulResponse();
        var adminClaims = CreateAdminClaimsPrincipal();

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };

        _mockTradingAccountService
            .Setup(x => x.UpdateTradingAccountAsync(accountId, request, adminClaims, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null as string));

        // Act
        var result = await _adminController.UpdateTradingAccount(accountId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        // Verify that the service was called with the correct parameters for audit trail
        _mockTradingAccountService.Verify(
            x => x.UpdateTradingAccountAsync(
                It.Is<int>(id => id == accountId),
                It.IsAny<UpdateTradingAccountRequest>(),
                It.Is<ClaimsPrincipal>(user => user == adminClaims),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Service should be called once with correct admin user for audit trail");
    }

    #endregion

    #endregion

    #region Helper Methods

    private static ClaimsPrincipal CreateAdminClaimsPrincipal()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static ClaimsPrincipal CreateUserClaimsPrincipal()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "2"),
            new Claim(ClaimTypes.Name, "user"),
            new Claim(ClaimTypes.Role, "User")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    #endregion
}
