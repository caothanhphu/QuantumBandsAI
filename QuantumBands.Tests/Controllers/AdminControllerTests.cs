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
        response!.Status.Should().Be("Active");
    }

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
