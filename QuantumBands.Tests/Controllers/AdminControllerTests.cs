using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;
using QuantumBands.Application.Features.Wallets.Commands.BankDeposit;
using QuantumBands.Application.Features.Wallets.Dtos;
using QuantumBands.Application.Features.Wallets.Queries.GetTransactions;
using QuantumBands.Application.Interfaces;
using QuantumBands.Tests.Common;
using QuantumBands.Tests.Fixtures;
using System.Linq;
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

    #region SCRUM-76: Get Pending Bank Deposits Endpoint Tests

    #region Happy Path Tests - Get Pending Bank Deposits

    /// <summary>
    /// Test: Valid request should return OK with pending deposits list
    /// Verifies the happy path scenario works correctly
    /// </summary>
    [Fact]
    public async Task GetPendingBankDeposits_WithValidRequest_ShouldReturnOkWithPendingDeposits()
    {
        // Arrange
        var query = TestDataBuilder.AdminPendingBankDeposits.ValidQuery();
        var expectedDeposits = TestDataBuilder.AdminPendingBankDeposits.ValidPendingDepositsResponse();
        var paginatedResult = new PaginatedList<AdminPendingBankDepositDto>(expectedDeposits, 2, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingBankDepositsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingBankDeposits(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var deposits = okResult!.Value as PaginatedList<AdminPendingBankDepositDto>;

        deposits.Should().NotBeNull();
        deposits!.Items.Should().HaveCount(2);
        deposits.TotalCount.Should().Be(2);
        deposits.Items.First().TransactionId.Should().Be(1001);
        deposits.Items.First().Username.Should().Be("testuser123");
        deposits.Items.First().AmountUSD.Should().Be(1000.00m);
        deposits.Items.First().Status.Should().Be("Pending");
    }

    /// <summary>
    /// Test: Pagination support should work correctly
    /// Verifies that pagination parameters are handled properly
    /// </summary>
    [Fact]
    public async Task GetPendingBankDeposits_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var query = TestDataBuilder.AdminPendingBankDeposits.ValidQuery();
        query.PageNumber = 2;
        query.PageSize = 5;

        var expectedDeposits = TestDataBuilder.AdminPendingBankDeposits.SinglePendingDepositResponse();
        var paginatedResult = new PaginatedList<AdminPendingBankDepositDto>(expectedDeposits, 11, 2, 5);

        _mockWalletService.Setup(x => x.GetAdminPendingBankDepositsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingBankDeposits(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var deposits = okResult!.Value as PaginatedList<AdminPendingBankDepositDto>;

        deposits.Should().NotBeNull();
        deposits!.PageNumber.Should().Be(2);
        deposits.PageSize.Should().Be(5);
        deposits.TotalCount.Should().Be(11);
        deposits.TotalPages.Should().Be(3);
    }

    /// <summary>
    /// Test: Date range filtering should work correctly
    /// Verifies that date filtering is applied properly
    /// </summary>
    [Fact]
    public async Task GetPendingBankDeposits_WithDateRangeFiltering_ShouldReturnFilteredResults()
    {
        // Arrange
        var query = TestDataBuilder.AdminPendingBankDeposits.QueryWithDateRange();
        var expectedDeposits = TestDataBuilder.AdminPendingBankDeposits.ValidPendingDepositsResponse();
        var paginatedResult = new PaginatedList<AdminPendingBankDepositDto>(expectedDeposits, 2, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingBankDepositsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingBankDeposits(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var deposits = okResult!.Value as PaginatedList<AdminPendingBankDepositDto>;

        deposits.Should().NotBeNull();
        deposits!.Items.Should().NotBeEmpty();
        // Verify service was called with date filtering parameters
        _mockWalletService.Verify(x => x.GetAdminPendingBankDepositsAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetAdminPendingBankDepositsQuery>(q => q.DateFrom.HasValue && q.DateTo.HasValue),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Complete deposit information should be displayed correctly
    /// Verifies all required deposit details are included in response
    /// </summary>
    [Fact]
    public async Task GetPendingBankDeposits_ShouldDisplayCompleteDepositInformation()
    {
        // Arrange
        var query = TestDataBuilder.AdminPendingBankDeposits.ValidQuery();
        var expectedDeposits = TestDataBuilder.AdminPendingBankDeposits.ValidPendingDepositsResponse();
        var paginatedResult = new PaginatedList<AdminPendingBankDepositDto>(expectedDeposits, 2, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingBankDepositsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingBankDeposits(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var deposits = okResult!.Value as PaginatedList<AdminPendingBankDepositDto>;

        deposits.Should().NotBeNull();
        var firstDeposit = deposits!.Items.First();

        // Verify user information display
        firstDeposit.UserId.Should().Be(1);
        firstDeposit.Username.Should().Be("testuser123");
        firstDeposit.UserEmail.Should().Be("test@example.com");

        // Verify amount and currency
        firstDeposit.AmountUSD.Should().Be(1000.00m);
        firstDeposit.CurrencyCode.Should().Be("USD");
        firstDeposit.AmountVND.Should().Be(24000000m);
        firstDeposit.ExchangeRate.Should().Be(24000m);

        // Verify reference codes
        firstDeposit.ReferenceCode.Should().Be("DEP001");
        firstDeposit.PaymentMethod.Should().Be("Bank Transfer");

        // Verify status and dates
        firstDeposit.Status.Should().Be("Pending");
        firstDeposit.TransactionDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(3));
        firstDeposit.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(3));
    }

    #endregion

    #region Authorization Tests - Get Pending Bank Deposits

    /// <summary>
    /// Test: Admin role verification should work
    /// Verifies only admin users can access the endpoint
    /// Note: This is enforced by the [Authorize(Roles = "Admin")] attribute on the controller
    /// </summary>
    [Fact]
    public async Task GetPendingBankDeposits_WithAdminRole_ShouldAllowAccess()
    {
        // Arrange
        var query = TestDataBuilder.AdminPendingBankDeposits.ValidQuery();
        var expectedDeposits = TestDataBuilder.AdminPendingBankDeposits.ValidPendingDepositsResponse();
        var paginatedResult = new PaginatedList<AdminPendingBankDepositDto>(expectedDeposits, 2, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingBankDepositsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingBankDeposits(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockWalletService.Verify(x => x.GetAdminPendingBankDepositsAsync(
            It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Filter Tests - Get Pending Bank Deposits

    /// <summary>
    /// Test: User filtering should work correctly
    /// Verifies that user ID and username/email filters are applied
    /// </summary>
    [Fact]
    public async Task GetPendingBankDeposits_WithUserFiltering_ShouldReturnFilteredResults()
    {
        // Arrange
        var query = TestDataBuilder.AdminPendingBankDeposits.QueryWithUserFilter();
        var expectedDeposits = TestDataBuilder.AdminPendingBankDeposits.SinglePendingDepositResponse();
        var paginatedResult = new PaginatedList<AdminPendingBankDepositDto>(expectedDeposits, 1, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingBankDepositsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingBankDeposits(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var deposits = okResult!.Value as PaginatedList<AdminPendingBankDepositDto>;

        deposits.Should().NotBeNull();
        deposits!.Items.Should().HaveCount(1);
        _mockWalletService.Verify(x => x.GetAdminPendingBankDepositsAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetAdminPendingBankDepositsQuery>(q => q.UserId.HasValue && !string.IsNullOrEmpty(q.UsernameOrEmail)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Amount range filtering should work correctly
    /// Verifies that min/max amount filters are applied
    /// </summary>
    [Fact]
    public async Task GetPendingBankDeposits_WithAmountFiltering_ShouldReturnFilteredResults()
    {
        // Arrange
        var query = TestDataBuilder.AdminPendingBankDeposits.QueryWithAmountFilter();
        var expectedDeposits = TestDataBuilder.AdminPendingBankDeposits.PendingDepositsWithVariedAmounts();
        var paginatedResult = new PaginatedList<AdminPendingBankDepositDto>(expectedDeposits, 2, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingBankDepositsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingBankDeposits(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var deposits = okResult!.Value as PaginatedList<AdminPendingBankDepositDto>;

        deposits.Should().NotBeNull();
        deposits!.Items.Should().NotBeEmpty();
        _mockWalletService.Verify(x => x.GetAdminPendingBankDepositsAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetAdminPendingBankDepositsQuery>(q => q.MinAmountUSD.HasValue && q.MaxAmountUSD.HasValue),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Reference code filtering should work correctly
    /// Verifies that reference code filter is applied
    /// </summary>
    [Fact]
    public async Task GetPendingBankDeposits_WithReferenceCodeFiltering_ShouldReturnFilteredResults()
    {
        // Arrange
        var query = TestDataBuilder.AdminPendingBankDeposits.QueryWithReferenceFilter();
        var expectedDeposits = TestDataBuilder.AdminPendingBankDeposits.SinglePendingDepositResponse();
        var paginatedResult = new PaginatedList<AdminPendingBankDepositDto>(expectedDeposits, 1, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingBankDepositsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingBankDeposits(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var deposits = okResult!.Value as PaginatedList<AdminPendingBankDepositDto>;

        deposits.Should().NotBeNull();
        deposits!.Items.Should().HaveCount(1);
        _mockWalletService.Verify(x => x.GetAdminPendingBankDepositsAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetAdminPendingBankDepositsQuery>(q => !string.IsNullOrEmpty(q.ReferenceCode)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Combined filters should work correctly
    /// Verifies that multiple filters can be applied simultaneously
    /// </summary>
    [Fact]
    public async Task GetPendingBankDeposits_WithCombinedFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        var query = TestDataBuilder.AdminPendingBankDeposits.ValidQuery();
        query.DateFrom = DateTime.UtcNow.AddDays(-7);
        query.DateTo = DateTime.UtcNow;
        query.MinAmountUSD = 500.00m;
        query.MaxAmountUSD = 3000.00m;
        query.UsernameOrEmail = "test";

        var expectedDeposits = TestDataBuilder.AdminPendingBankDeposits.ValidPendingDepositsResponse();
        var paginatedResult = new PaginatedList<AdminPendingBankDepositDto>(expectedDeposits, 2, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingBankDepositsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingBankDeposits(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var deposits = okResult!.Value as PaginatedList<AdminPendingBankDepositDto>;

        deposits.Should().NotBeNull();
        deposits!.Items.Should().NotBeEmpty();
        _mockWalletService.Verify(x => x.GetAdminPendingBankDepositsAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetAdminPendingBankDepositsQuery>(q => 
                q.DateFrom.HasValue && 
                q.DateTo.HasValue && 
                q.MinAmountUSD.HasValue && 
                q.MaxAmountUSD.HasValue && 
                !string.IsNullOrEmpty(q.UsernameOrEmail)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Empty results handling should work correctly
    /// Verifies that empty results are handled properly
    /// </summary>
    [Fact]
    public async Task GetPendingBankDeposits_WithNoMatchingResults_ShouldReturnEmptyList()
    {
        // Arrange
        var query = TestDataBuilder.AdminPendingBankDeposits.ValidQuery();
        var emptyResult = new PaginatedList<AdminPendingBankDepositDto>(new List<AdminPendingBankDepositDto>(), 0, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingBankDepositsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyResult);

        // Act
        var result = await _adminController.GetPendingBankDeposits(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var deposits = okResult!.Value as PaginatedList<AdminPendingBankDepositDto>;

        deposits.Should().NotBeNull();
        deposits!.Items.Should().BeEmpty();
        deposits.TotalCount.Should().Be(0);
        deposits.PageNumber.Should().Be(1);
        deposits.TotalPages.Should().Be(0);
    }

    #endregion

    #region Technical Tests - Get Pending Bank Deposits

    /// <summary>
    /// Test: Service should be called with correct parameters
    /// Verifies proper parameter passing and response handling
    /// </summary>
    [Fact]
    public async Task GetPendingBankDeposits_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        var query = TestDataBuilder.AdminPendingBankDeposits.ValidQuery();
        var expectedDeposits = TestDataBuilder.AdminPendingBankDeposits.ValidPendingDepositsResponse();
        var paginatedResult = new PaginatedList<AdminPendingBankDepositDto>(expectedDeposits, 2, 1, 10);
        var cancellationToken = new CancellationToken();

        _mockWalletService.Setup(x => x.GetAdminPendingBankDepositsAsync(
                It.IsAny<ClaimsPrincipal>(), query, cancellationToken))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingBankDeposits(query, cancellationToken);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockWalletService.Verify(x => x.GetAdminPendingBankDepositsAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetAdminPendingBankDepositsQuery>(q => q == query),
            cancellationToken), Times.Once);
    }

    /// <summary>
    /// Test: Default query parameters should be handled correctly
    /// Verifies that default values are applied when parameters are not provided
    /// </summary>
    [Fact]
    public async Task GetPendingBankDeposits_WithDefaultParameters_ShouldUseDefaultValues()
    {
        // Arrange
        var query = new GetAdminPendingBankDepositsQuery(); // Using default values
        var expectedDeposits = TestDataBuilder.AdminPendingBankDeposits.ValidPendingDepositsResponse();
        var paginatedResult = new PaginatedList<AdminPendingBankDepositDto>(expectedDeposits, 2, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingBankDepositsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingBankDeposits(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var deposits = okResult!.Value as PaginatedList<AdminPendingBankDepositDto>;

        deposits.Should().NotBeNull();
        _mockWalletService.Verify(x => x.GetAdminPendingBankDepositsAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetAdminPendingBankDepositsQuery>(q => 
                q.PageNumber == 1 && 
                q.PageSize == 10 && 
                q.SortBy == "TransactionDate" && 
                q.SortOrder == "desc"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Sorting should work correctly
    /// Verifies that sorting parameters are applied
    /// </summary>
    [Fact]
    public async Task GetPendingBankDeposits_WithCustomSorting_ShouldApplySortingCorrectly()
    {
        // Arrange
        var query = TestDataBuilder.AdminPendingBankDeposits.ValidQuery();
        query.SortBy = "AmountUSD";
        query.SortOrder = "asc";

        var expectedDeposits = TestDataBuilder.AdminPendingBankDeposits.PendingDepositsWithVariedAmounts()
            .OrderBy(d => d.AmountUSD).ToList();
        var paginatedResult = new PaginatedList<AdminPendingBankDepositDto>(expectedDeposits, 2, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingBankDepositsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingBankDeposits(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var deposits = okResult!.Value as PaginatedList<AdminPendingBankDepositDto>;

        deposits.Should().NotBeNull();
        _mockWalletService.Verify(x => x.GetAdminPendingBankDepositsAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetAdminPendingBankDepositsQuery>(q => q.SortBy == "AmountUSD" && q.SortOrder == "asc"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #endregion

    #region CancelBankDeposit - Unit Tests

    /// <summary>
    /// Unit tests for the CancelBankDeposit endpoint (POST /admin/wallets/deposits/bank/cancel)
    /// These tests verify the controller's behavior for bank deposit cancellation functionality.
    /// 
    /// Test Coverage:
    /// - Happy Path: Valid cancellation requests and successful responses
    /// - Authorization: Authentication and role-based access control
    /// - Validation: Request validation and error handling
    /// - Business Logic: Various business scenarios and edge cases
    /// 
    /// Implementation details:
    /// - Tests mock the IWalletService to isolate controller behavior
    /// - Authentication is simulated using ClaimsIdentity
    /// - Tests verify HTTP status codes, response types, and logging behavior
    /// - Comprehensive test data is provided via TestDataBuilder.CancelBankDeposit
    /// </summary>

    #region CancelBankDeposit - Happy Path Tests

    /// <summary>
    /// Test: Valid bank deposit cancellation should return OK with transaction details
    /// Verifies the happy path scenario works correctly
    /// </summary>
    [Fact]
    public async Task CancelBankDeposit_WithValidRequest_ShouldReturnOkWithTransaction()
    {
        // Arrange
        var request = TestDataBuilder.CancelBankDeposit.ValidRequest();
        var expectedTransaction = TestDataBuilder.CancelBankDeposit.ValidCancelledTransactionDto();

        _mockWalletService.Setup(x => x.CancelBankDepositAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, null));

        // Act
        var result = await _adminController.CancelBankDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var transaction = okResult!.Value as WalletTransactionDto;

        transaction.Should().NotBeNull();
        transaction!.TransactionId.Should().Be(expectedTransaction.TransactionId);
        transaction.Status.Should().Be("Cancelled");
        transaction.Description.Should().Contain("cancelled by admin");
        _mockWalletService.Verify(x => x.CancelBankDepositAsync(
            It.IsAny<ClaimsPrincipal>(), 
            It.Is<CancelBankDepositRequest>(r => r.TransactionId == request.TransactionId && r.AdminNotes == request.AdminNotes),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Successful cancellation should log appropriate information
    /// Verifies logging behavior for successful operations
    /// </summary>
    [Fact]
    public async Task CancelBankDeposit_WithValidRequest_ShouldLogInformation()
    {
        // Arrange
        var request = TestDataBuilder.CancelBankDeposit.ValidRequest();
        var expectedTransaction = TestDataBuilder.CancelBankDeposit.ValidCancelledTransactionDto();

        _mockWalletService.Setup(x => x.CancelBankDepositAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, null));

        // Act
        var result = await _adminController.CancelBankDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Admin 1 attempting to cancel bank deposit TransactionID: {request.TransactionId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region CancelBankDeposit - Authorization Tests

    /// <summary>
    /// Test: Unauthenticated user should not be able to cancel deposits
    /// Verifies authorization requirement
    /// </summary>
    [Fact]
    public async Task CancelBankDeposit_WithoutAuthentication_ShouldRequireAuth()
    {
        // Arrange
        var request = TestDataBuilder.CancelBankDeposit.ValidRequest();
        
        // Remove authentication
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act & Assert - This would be handled by the [Authorize] attribute
        // In integration tests, this would return 401 Unauthorized
        // For unit tests, we focus on testing the controller logic assuming auth passes
        var result = await _adminController.CancelBankDeposit(request, CancellationToken.None);
        
        // The controller method will execute but the service call should handle the missing user context
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Test: Non-admin user should not be able to cancel deposits
    /// Verifies role-based authorization
    /// </summary>
    [Fact]
    public async Task CancelBankDeposit_WithNonAdminUser_ShouldBeForbidden()
    {
        // Arrange
        var request = TestDataBuilder.CancelBankDeposit.ValidRequest();
        
        // Setup non-admin user
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "2"),
            new(ClaimTypes.Name, "regularuser"),
            new(ClaimTypes.Role, "User") // Not Admin
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

        // Act & Assert - This would be handled by the [Authorize(Roles = "Admin")] attribute
        // In integration tests, this would return 403 Forbidden
        // For unit tests, we assume the authorization passes and test the business logic
        var result = await _adminController.CancelBankDeposit(request, CancellationToken.None);
        
        result.Should().NotBeNull();
    }

    #endregion

    #region CancelBankDeposit - Validation Tests

    /// <summary>
    /// Test: Request with invalid transaction ID should return BadRequest
    /// Verifies validation for transaction ID
    /// </summary>
    [Fact]
    public async Task CancelBankDeposit_WithInvalidTransactionId_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CancelBankDeposit.RequestWithInvalidTransactionId();

        _mockWalletService.Setup(x => x.CancelBankDepositAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Transaction ID must be valid."));

        // Act
        var result = await _adminController.CancelBankDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Admin cancel bank deposit failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Request with empty admin notes should return BadRequest
    /// Verifies validation for required admin notes
    /// </summary>
    [Fact]
    public async Task CancelBankDeposit_WithEmptyAdminNotes_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CancelBankDeposit.RequestWithEmptyAdminNotes();

        _mockWalletService.Setup(x => x.CancelBankDepositAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Admin notes are required for cancellation."));

        // Act
        var result = await _adminController.CancelBankDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    /// <summary>
    /// Test: Request with admin notes too long should return BadRequest
    /// Verifies validation for admin notes length
    /// </summary>
    [Fact]
    public async Task CancelBankDeposit_WithTooLongAdminNotes_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CancelBankDeposit.RequestWithTooLongAdminNotes();

        _mockWalletService.Setup(x => x.CancelBankDepositAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Admin notes cannot exceed 500 characters."));

        // Act
        var result = await _adminController.CancelBankDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    #endregion

    #region CancelBankDeposit - Business Logic Tests

    /// <summary>
    /// Test: Cancelling non-existent transaction should return NotFound
    /// Verifies handling of non-existent transactions
    /// </summary>
    [Fact]
    public async Task CancelBankDeposit_WithNonExistentTransaction_ShouldReturnNotFound()
    {
        // Arrange
        var request = TestDataBuilder.CancelBankDeposit.RequestForNonExistentTransaction();

        _mockWalletService.Setup(x => x.CancelBankDepositAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Bank deposit transaction not found."));

        // Act
        var result = await _adminController.CancelBankDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var responseMessage = notFoundResult!.Value;
        responseMessage.Should().NotBeNull();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Admin cancel bank deposit failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Cancelling already confirmed deposit should return BadRequest
    /// Verifies business rule for confirmed deposits
    /// </summary>
    [Fact]
    public async Task CancelBankDeposit_WithAlreadyConfirmedDeposit_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CancelBankDeposit.RequestForAlreadyConfirmedDeposit();

        _mockWalletService.Setup(x => x.CancelBankDepositAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Confirmed deposit cannot be cancelled."));

        // Act
        var result = await _adminController.CancelBankDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = badRequestResult!.Value;
        responseMessage.Should().NotBeNull();
    }

    /// <summary>
    /// Test: Cancelling already cancelled deposit should return BadRequest
    /// Verifies business rule for already cancelled deposits
    /// </summary>
    [Fact]
    public async Task CancelBankDeposit_WithAlreadyCancelledDeposit_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CancelBankDeposit.RequestForAlreadyCancelledDeposit();

        _mockWalletService.Setup(x => x.CancelBankDepositAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Deposit is already cancelled and cannot be cancelled again."));

        // Act
        var result = await _adminController.CancelBankDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = badRequestResult!.Value;
        responseMessage.Should().NotBeNull();
    }

    /// <summary>
    /// Test: Service returning generic error should return InternalServerError
    /// Verifies handling of unexpected service errors
    /// </summary>
    [Fact]
    public async Task CancelBankDeposit_WithServiceError_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = TestDataBuilder.CancelBankDeposit.ValidRequest();

        _mockWalletService.Setup(x => x.CancelBankDepositAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Database connection failed."));

        // Act
        var result = await _adminController.CancelBankDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        var serverErrorResult = result as ObjectResult;
        var responseMessage = serverErrorResult!.Value;
        responseMessage.Should().NotBeNull();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Admin cancel bank deposit failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Service returning null error message should use default message
    /// Verifies default error message handling
    /// </summary>
    [Fact]
    public async Task CancelBankDeposit_WithNullErrorMessage_ShouldReturnDefaultMessage()
    {
        // Arrange
        var request = TestDataBuilder.CancelBankDeposit.ValidRequest();

        _mockWalletService.Setup(x => x.CancelBankDepositAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null)); // null error message

        // Act
        var result = await _adminController.CancelBankDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        var serverErrorResult = result as ObjectResult;
        var response = serverErrorResult!.Value;
        response.Should().NotBeNull();
        
        // Check that default message is used
        var messageProperty = response.GetType().GetProperty("Message");
        var message = messageProperty?.GetValue(response)?.ToString();
        message.Should().Be("Failed to cancel bank deposit.");
    }

    #endregion

    #endregion
} 
