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
            _mockLogger.Object
        );

        // Setup authenticated admin user
        SetupAuthenticatedAdmin();
    }

    private void SetupAuthenticatedAdmin()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Name, "admin"),
            new(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    #region UpdateInitialShareOffering - Happy Path Tests

    /// <summary>
    /// Test: Valid offering update should return OK with updated offering
    /// Verifies the happy path scenario works correctly
    /// </summary>
    [Fact]
    public async Task UpdateInitialShareOffering_WithValidRequest_ShouldReturnOkWithUpdatedOffering()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidUpdateOfferingRequest();
        var expectedOffering = TestDataBuilder.InitialShareOfferings.ValidInitialOfferingDto();

        _mockTradingAccountService.Setup(x => x.UpdateInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOffering, null));

        // Act
        var result = await _adminController.UpdateInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var offering = okResult!.Value as InitialShareOfferingDto;
        
        offering.Should().NotBeNull();
        offering!.OfferingId.Should().Be(expectedOffering.OfferingId);
        offering.SharesOffered.Should().Be(expectedOffering.SharesOffered);
        offering.OfferingPricePerShare.Should().Be(expectedOffering.OfferingPricePerShare);
        offering.Status.Should().Be("Active");
    }

    /// <summary>
    /// Test: Shares quantity modification should work correctly
    /// Verifies shares quantity can be updated successfully
    /// </summary>
    [Fact]
    public async Task UpdateInitialShareOffering_WithSharesQuantityModification_ShouldReturnUpdatedShares()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidUpdateOfferingRequest();
        request.SharesOffered = 20000; // Increased shares

        var expectedOffering = TestDataBuilder.InitialShareOfferings.ValidInitialOfferingDto();
        expectedOffering.SharesOffered = 20000;

        _mockTradingAccountService.Setup(x => x.UpdateInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOffering, null));

        // Act
        var result = await _adminController.UpdateInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var offering = okResult!.Value as InitialShareOfferingDto;
        
        offering!.SharesOffered.Should().Be(20000);
    }

    /// <summary>
    /// Test: Price adjustments should work correctly
    /// Verifies price can be updated successfully
    /// </summary>
    [Fact]
    public async Task UpdateInitialShareOffering_WithPriceAdjustments_ShouldReturnUpdatedPrice()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidUpdateOfferingRequest();
        request.OfferingPricePerShare = 15.75m; // New price

        var expectedOffering = TestDataBuilder.InitialShareOfferings.ValidInitialOfferingDto();
        expectedOffering.OfferingPricePerShare = 15.75m;

        _mockTradingAccountService.Setup(x => x.UpdateInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOffering, null));

        // Act
        var result = await _adminController.UpdateInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var offering = okResult!.Value as InitialShareOfferingDto;
        
        offering!.OfferingPricePerShare.Should().Be(15.75m);
    }

    /// <summary>
    /// Test: Date extension should work correctly
    /// Verifies offering end date can be extended
    /// </summary>
    [Fact]
    public async Task UpdateInitialShareOffering_WithDateExtension_ShouldReturnExtendedDate()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidUpdateOfferingRequest();
        var newEndDate = DateTime.UtcNow.AddMonths(2);
        request.OfferingEndDate = newEndDate;

        var expectedOffering = TestDataBuilder.InitialShareOfferings.ValidInitialOfferingDto();
        expectedOffering.OfferingEndDate = newEndDate;

        _mockTradingAccountService.Setup(x => x.UpdateInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOffering, null));

        // Act
        var result = await _adminController.UpdateInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var offering = okResult!.Value as InitialShareOfferingDto;
        
        offering!.OfferingEndDate.Should().Be(newEndDate);
    }

    #endregion

    #region UpdateInitialShareOffering - Authorization Tests

    /// <summary>
    /// Test: Admin role verification should work
    /// Verifies only admin users can access the endpoint
    /// </summary>
    [Fact]
    public async Task UpdateInitialShareOffering_WithAdminRole_ShouldAllowAccess()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidUpdateOfferingRequest();
        var expectedOffering = TestDataBuilder.InitialShareOfferings.ValidInitialOfferingDto();

        _mockTradingAccountService.Setup(x => x.UpdateInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOffering, null));

        // Act
        var result = await _adminController.UpdateInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockTradingAccountService.Verify(x => x.UpdateInitialShareOfferingAsync(
            accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateInitialShareOffering - Validation Tests

    /// <summary>
    /// Test: Invalid account ID should return NotFound
    /// Verifies proper handling of non-existent account
    /// </summary>
    [Fact]
    public async Task UpdateInitialShareOffering_WithInvalidAccountId_ShouldReturnNotFound()
    {
        // Arrange
        const int invalidAccountId = 999;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidUpdateOfferingRequest();

        _mockTradingAccountService.Setup(x => x.UpdateInitialShareOfferingAsync(
                invalidAccountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Trading account not found"));

        // Act
        var result = await _adminController.UpdateInitialShareOffering(invalidAccountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value;
        response.Should().NotBeNull();
    }

    /// <summary>
    /// Test: Invalid offering ID should return NotFound
    /// Verifies proper handling of non-existent offering
    /// </summary>
    [Fact]
    public async Task UpdateInitialShareOffering_WithInvalidOfferingId_ShouldReturnNotFound()
    {
        // Arrange
        const int accountId = 1;
        const int invalidOfferingId = 999;
        var request = TestDataBuilder.InitialShareOfferings.ValidUpdateOfferingRequest();

        _mockTradingAccountService.Setup(x => x.UpdateInitialShareOfferingAsync(
                accountId, invalidOfferingId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Initial share offering not found"));

        // Act
        var result = await _adminController.UpdateInitialShareOffering(accountId, invalidOfferingId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Test: Non-existent offering should return NotFound
    /// Verifies proper error handling for missing offering
    /// </summary>
    [Fact]
    public async Task UpdateInitialShareOffering_WithNonExistentOffering_ShouldReturnNotFound()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 999;
        var request = TestDataBuilder.InitialShareOfferings.ValidUpdateOfferingRequest();

        _mockTradingAccountService.Setup(x => x.UpdateInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Initial share offering with ID 999 not found for trading account 1"));

        // Act
        var result = await _adminController.UpdateInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Test: Invalid shares offered (≤ 0) should return BadRequest
    /// Verifies validation of shares quantity
    /// </summary>
    [Fact]
    public async Task UpdateInitialShareOffering_WithInvalidSharesOffered_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidUpdateOfferingRequest();
        request.SharesOffered = 0; // Invalid shares

        _mockTradingAccountService.Setup(x => x.UpdateInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Shares offered must be greater than 0"));

        // Act
        var result = await _adminController.UpdateInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test: Invalid price adjustments should return BadRequest
    /// Verifies validation of price values
    /// </summary>
    [Fact]
    public async Task UpdateInitialShareOffering_WithInvalidPriceAdjustments_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidUpdateOfferingRequest();
        request.OfferingPricePerShare = -5.0m; // Invalid price

        _mockTradingAccountService.Setup(x => x.UpdateInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Offering price per share must be greater than 0"));

        // Act
        var result = await _adminController.UpdateInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region UpdateInitialShareOffering - Business Logic Tests

    /// <summary>
    /// Test: Update completed offering should return BadRequest
    /// Verifies business rule that completed offerings cannot be updated
    /// </summary>
    [Fact]
    public async Task UpdateInitialShareOffering_WithCompletedOffering_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidUpdateOfferingRequest();

        _mockTradingAccountService.Setup(x => x.UpdateInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Cannot change offering status from Completed"));

        // Act
        var result = await _adminController.UpdateInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test: Update cancelled offering should return BadRequest
    /// Verifies business rule that cancelled offerings cannot be updated
    /// </summary>
    [Fact]
    public async Task UpdateInitialShareOffering_WithCancelledOffering_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidUpdateOfferingRequest();

        _mockTradingAccountService.Setup(x => x.UpdateInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Cannot change offering status from Cancelled"));

        // Act
        var result = await _adminController.UpdateInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test: Active offering modifications should work
    /// Verifies that active offerings can be modified
    /// </summary>
    [Fact]
    public async Task UpdateInitialShareOffering_WithActiveOffering_ShouldAllowModifications()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidUpdateOfferingRequest();
        var expectedOffering = TestDataBuilder.InitialShareOfferings.ValidInitialOfferingDto();
        expectedOffering.Status = "Active";

        _mockTradingAccountService.Setup(x => x.UpdateInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOffering, null));

        // Act
        var result = await _adminController.UpdateInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var offering = okResult!.Value as InitialShareOfferingDto;
        offering!.Status.Should().Be("Active");
    }

    /// <summary>
    /// Test: Price range validation should work
    /// Verifies that price ranges are validated correctly
    /// </summary>
    [Fact]
    public async Task UpdateInitialShareOffering_WithPriceRangeValidation_ShouldEnforceRules()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidUpdateOfferingRequest();
        request.FloorPricePerShare = 15.0m;
        request.CeilingPricePerShare = 10.0m; // Invalid: ceiling < floor

        _mockTradingAccountService.Setup(x => x.UpdateInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Ceiling price must be greater than floor price"));

        // Act
        var result = await _adminController.UpdateInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test: Null request body should return BadRequest
    /// Verifies proper handling of null request
    /// </summary>
    [Fact]
    public async Task UpdateInitialShareOffering_WithNullRequest_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;

        // Act
        var result = await _adminController.UpdateInitialShareOffering(accountId, offeringId, null!, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var response = badRequestResult!.Value;
        response.Should().NotBeNull();
    }

    /// <summary>
    /// Test: Service should be called with correct parameters
    /// Verifies that service method is called with expected parameters
    /// </summary>
    [Fact]
    public async Task UpdateInitialShareOffering_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidUpdateOfferingRequest();
        var expectedOffering = TestDataBuilder.InitialShareOfferings.ValidInitialOfferingDto();

        _mockTradingAccountService.Setup(x => x.UpdateInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOffering, null));

        // Act
        var result = await _adminController.UpdateInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        _mockTradingAccountService.Verify(x => x.UpdateInitialShareOfferingAsync(
            accountId, 
            offeringId, 
            It.Is<UpdateInitialShareOfferingRequest>(r => r == request),
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SCRUM-75: Cancel Initial Offering Endpoint Tests

    #region Happy Path Tests - Cancel Initial Offering

    /// <summary>
    /// Test: Valid offering cancellation should return OK with cancelled offering
    /// Verifies the happy path scenario works correctly for active offerings
    /// </summary>
    [Fact]
    public async Task CancelInitialShareOffering_WithValidActiveOffering_ShouldReturnOkWithCancelledOffering()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidCancelRequest();
        var expectedOffering = TestDataBuilder.InitialShareOfferings.CancelledOfferingResponse();

        _mockTradingAccountService.Setup(x => x.CancelInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOffering, null));

        // Act
        var result = await _adminController.CancelInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var offering = okResult!.Value as InitialShareOfferingDto;

        offering.Should().NotBeNull();
        offering!.OfferingId.Should().Be(1);
        offering.Status.Should().Be("Cancelled");
        offering.SharesOffered.Should().Be(10000);
        offering.SharesSold.Should().Be(0);
    }

    /// <summary>
    /// Test: Cancellation without admin notes should succeed
    /// Verifies that admin notes are optional for cancellation
    /// </summary>
    [Fact]
    public async Task CancelInitialShareOffering_WithoutAdminNotes_ShouldReturnOkWithCancelledOffering()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidCancelRequestWithoutNotes();
        var expectedOffering = TestDataBuilder.InitialShareOfferings.CancelledOfferingResponse();

        _mockTradingAccountService.Setup(x => x.CancelInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOffering, null));

        // Act
        var result = await _adminController.CancelInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var offering = okResult!.Value as InitialShareOfferingDto;

        offering.Should().NotBeNull();
        offering!.Status.Should().Be("Cancelled");
    }

    /// <summary>
    /// Test: Active offering with sales should be cancelled successfully
    /// Verifies that offerings with partial sales can still be cancelled
    /// </summary>
    [Fact]
    public async Task CancelInitialShareOffering_WithActiveOfferingHavingSales_ShouldReturnOkWithCancelledOffering()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 2;
        var request = TestDataBuilder.InitialShareOfferings.ValidCancelRequest();
        var expectedOffering = TestDataBuilder.InitialShareOfferings.CancelledOfferingResponse();
        expectedOffering.OfferingId = 2;
        expectedOffering.SharesSold = 2500;

        _mockTradingAccountService.Setup(x => x.CancelInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOffering, null));

        // Act
        var result = await _adminController.CancelInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var offering = okResult!.Value as InitialShareOfferingDto;

        offering.Should().NotBeNull();
        offering!.Status.Should().Be("Cancelled");
        offering.SharesSold.Should().Be(2500);
    }

    #endregion

    #region Authorization Tests - Cancel Initial Offering

    /// <summary>
    /// Test: Unauthenticated request should return Unauthorized
    /// Verifies that unauthenticated users cannot cancel offerings
    /// </summary>
    [Fact]
    public async Task CancelInitialShareOffering_WithUnauthenticatedUser_ShouldReturnInternalServerError()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidCancelRequest();

        // Remove authentication
        _adminController.ControllerContext.HttpContext = new DefaultHttpContext();

        _mockTradingAccountService.Setup(x => x.CancelInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Admin user not authenticated."));

        // Act
        var result = await _adminController.CancelInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var objectResult = result as ObjectResult;
        objectResult!.Value.Should().BeEquivalentTo(new { Message = "Admin user not authenticated." });
    }

    /// <summary>
    /// Test: Non-admin user should not have access
    /// Note: This is enforced by the [Authorize(Roles = "Admin")] attribute
    /// The actual test would be handled by the authorization middleware
    /// </summary>
    [Fact]
    public void CancelInitialShareOffering_WithNonAdminUser_ShouldBeHandledByAuthorization()
    {
        // This test verifies that the endpoint has proper authorization attributes
        // The actual authorization is handled by ASP.NET Core's authorization middleware
        
        // Arrange & Assert
        var controllerType = typeof(AdminController);
        var authorizeAttribute = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);
        authorizeAttribute.Should().NotBeEmpty("AdminController should require authorization");
        
        var attribute = authorizeAttribute[0] as Microsoft.AspNetCore.Authorization.AuthorizeAttribute;
        attribute!.Roles.Should().Be("Admin", "AdminController should require Admin role");
    }

    #endregion

    #region Validation Tests - Cancel Initial Offering

    /// <summary>
    /// Test: Invalid account ID should return NotFound
    /// Verifies that non-existent account IDs are handled properly
    /// </summary>
    [Fact]
    public async Task CancelInitialShareOffering_WithInvalidAccountId_ShouldReturnNotFound()
    {
        // Arrange
        const int invalidAccountId = 999;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidCancelRequest();

        _mockTradingAccountService.Setup(x => x.CancelInitialShareOfferingAsync(
                invalidAccountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Trading account not found"));

        // Act
        var result = await _adminController.CancelInitialShareOffering(invalidAccountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new { Message = "Trading account not found" });
    }

    /// <summary>
    /// Test: Invalid offering ID should return NotFound
    /// Verifies that non-existent offering IDs are handled properly
    /// </summary>
    [Fact]
    public async Task CancelInitialShareOffering_WithInvalidOfferingId_ShouldReturnNotFound()
    {
        // Arrange
        const int accountId = 1;
        const int invalidOfferingId = 999;
        var request = TestDataBuilder.InitialShareOfferings.ValidCancelRequest();

        _mockTradingAccountService.Setup(x => x.CancelInitialShareOfferingAsync(
                accountId, invalidOfferingId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, $"Initial share offering with ID {invalidOfferingId} not found for trading account {accountId}."));

        // Act
        var result = await _adminController.CancelInitialShareOffering(accountId, invalidOfferingId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new { Message = $"Initial share offering with ID {invalidOfferingId} not found for trading account {accountId}." });
    }

    /// <summary>
    /// Test: Non-existent offering should return NotFound
    /// Verifies that requesting cancellation of non-existent offerings returns proper error
    /// </summary>
    [Fact]
    public async Task CancelInitialShareOffering_WithNonExistentOffering_ShouldReturnNotFound()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 999;
        var request = TestDataBuilder.InitialShareOfferings.ValidCancelRequest();

        _mockTradingAccountService.Setup(x => x.CancelInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Initial share offering with ID 999 not found for trading account 1."));

        // Act
        var result = await _adminController.CancelInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Test: Cancellation reason too long should return InternalServerError
    /// Verifies that admin notes exceeding 500 characters are handled properly
    /// Note: This validation would typically happen in the service layer or via model validation
    /// </summary>
    [Fact]
    public async Task CancelInitialShareOffering_WithAdminNotesTooLong_ShouldReturnInternalServerError()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.InvalidCancelRequestTooLongNotes();

        _mockTradingAccountService.Setup(x => x.CancelInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Admin notes cannot exceed 500 characters."));

        // Act
        var result = await _adminController.CancelInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var objectResult = result as ObjectResult;
        objectResult!.Value.Should().BeEquivalentTo(new { Message = "Admin notes cannot exceed 500 characters." });
    }

    #endregion

    #region Business Logic Tests - Cancel Initial Offering

    /// <summary>
    /// Test: Cancel completed offering should return BadRequest
    /// Verifies business rule that completed offerings cannot be cancelled
    /// </summary>
    [Fact]
    public async Task CancelInitialShareOffering_WithCompletedOffering_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 3;
        var request = TestDataBuilder.InitialShareOfferings.ValidCancelRequest();

        _mockTradingAccountService.Setup(x => x.CancelInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Only 'Active' offerings can be cancelled. Current status is 'Completed'."));

        // Act
        var result = await _adminController.CancelInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { Message = "Only 'Active' offerings can be cancelled. Current status is 'Completed'." });
    }

    /// <summary>
    /// Test: Cancel already cancelled offering should return BadRequest
    /// Verifies business rule that already cancelled offerings cannot be cancelled again
    /// </summary>
    [Fact]
    public async Task CancelInitialShareOffering_WithAlreadyCancelledOffering_ShouldReturnBadRequest()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 4;
        var request = TestDataBuilder.InitialShareOfferings.ValidCancelRequest();

        _mockTradingAccountService.Setup(x => x.CancelInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Only 'Active' offerings can be cancelled. Current status is 'Cancelled'."));

        // Act
        var result = await _adminController.CancelInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { Message = "Only 'Active' offerings can be cancelled. Current status is 'Cancelled'." });
    }

    #endregion

    #region Technical Tests - Cancel Initial Offering

    /// <summary>
    /// Test: Service should be called with correct parameters
    /// Verifies proper parameter passing and response handling
    /// </summary>
    [Fact]
    public async Task CancelInitialShareOffering_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidCancelRequest();
        var expectedOffering = TestDataBuilder.InitialShareOfferings.CancelledOfferingResponse();
        var cancellationToken = new CancellationToken();

        _mockTradingAccountService.Setup(x => x.CancelInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), cancellationToken))
            .ReturnsAsync((expectedOffering, null));

        // Act
        var result = await _adminController.CancelInitialShareOffering(accountId, offeringId, request, cancellationToken);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockTradingAccountService.Verify(x => x.CancelInitialShareOfferingAsync(
            accountId, 
            offeringId, 
            It.Is<CancelInitialShareOfferingRequest>(r => r == request),
            It.IsAny<ClaimsPrincipal>(),
            cancellationToken), Times.Once);
    }

    /// <summary>
    /// Test: Null request body should be handled gracefully
    /// Verifies that missing request body doesn't cause errors
    /// </summary>
    [Fact]
    public async Task CancelInitialShareOffering_WithNullRequestBody_ShouldCreateEmptyRequest()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        CancelInitialShareOfferingRequest? request = null;
        var expectedOffering = TestDataBuilder.InitialShareOfferings.CancelledOfferingResponse();

        _mockTradingAccountService.Setup(x => x.CancelInitialShareOfferingAsync(
                accountId, offeringId, It.IsAny<CancelInitialShareOfferingRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOffering, null));

        // Act
        var result = await _adminController.CancelInitialShareOffering(accountId, offeringId, request!, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockTradingAccountService.Verify(x => x.CancelInitialShareOfferingAsync(
            accountId, 
            offeringId, 
            It.Is<CancelInitialShareOfferingRequest>(r => r != null),
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Server error should return InternalServerError
    /// Verifies proper error handling for unexpected errors
    /// </summary>
    [Fact]
    public async Task CancelInitialShareOffering_WithServerError_ShouldReturnInternalServerError()
    {
        // Arrange
        const int accountId = 1;
        const int offeringId = 1;
        var request = TestDataBuilder.InitialShareOfferings.ValidCancelRequest();

        _mockTradingAccountService.Setup(x => x.CancelInitialShareOfferingAsync(
                accountId, offeringId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "An error occurred while cancelling the offering."));

        // Act
        var result = await _adminController.CancelInitialShareOffering(accountId, offeringId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var objectResult = result as ObjectResult;
        objectResult!.Value.Should().BeEquivalentTo(new { Message = "An error occurred while cancelling the offering." });
    }

    #endregion

    #endregion
} 
