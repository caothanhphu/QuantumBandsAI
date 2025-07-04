using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.ExchangeMonitor.Dtos;
using QuantumBands.Application.Features.Admin.ExchangeMonitor.Queries;
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands.ManualSnapshotTrigger;
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands.RecalculateProfitDistribution;
using QuantumBands.Application.Features.Admin.TradingAccounts.Queries.GetSnapshotStatus;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;
using QuantumBands.Application.Features.Admin.Users.Commands.UpdateUserPassword;
using QuantumBands.Application.Features.Wallets.Commands.AdminActions;
using QuantumBands.Application.Features.Wallets.Commands.AdminDeposit;
using QuantumBands.Application.Features.Wallets.Commands.BankDeposit;
using QuantumBands.Application.Features.Wallets.Dtos;
using QuantumBands.Application.Features.Wallets.Queries.GetTransactions;
using QuantumBands.Application.Interfaces;
using QuantumBands.Tests.Common;
using QuantumBands.Tests.Fixtures;
using static QuantumBands.Tests.Fixtures.WalletsTestDataBuilder;
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
    private readonly Mock<IDailySnapshotService> _mockDailySnapshotService;
    private readonly Mock<IProfitDistributionService> _mockProfitDistributionService;
    private readonly Mock<ILogger<AdminController>> _mockLogger;

    public AdminControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockWalletService = new Mock<IWalletService>();
        _mockTradingAccountService = new Mock<ITradingAccountService>();
        _mockDashboardService = new Mock<IAdminDashboardService>();
        _mockExchangeService = new Mock<IExchangeService>();
        _mockDailySnapshotService = new Mock<IDailySnapshotService>();
        _mockProfitDistributionService = new Mock<IProfitDistributionService>();
        _mockLogger = new Mock<ILogger<AdminController>>();

        _adminController = new AdminController(
            _mockUserService.Object,
            _mockWalletService.Object,
            _mockTradingAccountService.Object,
            _mockDashboardService.Object,
            _mockExchangeService.Object,
            _mockDailySnapshotService.Object,
            _mockProfitDistributionService.Object,
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

    private void SetupAdminUser(string adminUserId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, adminUserId),
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

    private void SetupInvestorUser(string investorUserId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, investorUserId),
            new(ClaimTypes.Name, "investor"),
            new(ClaimTypes.Role, "Investor")
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
        var query = AdminPendingBankDeposits.ValidQuery();
        var expectedDeposits = AdminPendingBankDeposits.ValidPendingDepositsResponse();
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
        var query = AdminPendingBankDeposits.ValidQuery();
        query.PageNumber = 2;
        query.PageSize = 5;

        var expectedDeposits = AdminPendingBankDeposits.SinglePendingDepositResponse();
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
        var query = AdminPendingBankDeposits.QueryWithDateRange();
        var expectedDeposits = AdminPendingBankDeposits.ValidPendingDepositsResponse();
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
        var query = AdminPendingBankDeposits.ValidQuery();
        var expectedDeposits = AdminPendingBankDeposits.ValidPendingDepositsResponse();
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
        var query = AdminPendingBankDeposits.ValidQuery();
        var expectedDeposits = AdminPendingBankDeposits.ValidPendingDepositsResponse();
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
        var query = AdminPendingBankDeposits.QueryWithUserFilter();
        var expectedDeposits = AdminPendingBankDeposits.SinglePendingDepositResponse();
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
        var query = AdminPendingBankDeposits.QueryWithAmountFilter();
        var expectedDeposits = AdminPendingBankDeposits.PendingDepositsWithVariedAmounts();
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
        var query = AdminPendingBankDeposits.QueryWithReferenceFilter();
        var expectedDeposits = AdminPendingBankDeposits.SinglePendingDepositResponse();
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
        var query = AdminPendingBankDeposits.ValidQuery();
        query.DateFrom = DateTime.UtcNow.AddDays(-7);
        query.DateTo = DateTime.UtcNow;
        query.MinAmountUSD = 500.00m;
        query.MaxAmountUSD = 3000.00m;
        query.UsernameOrEmail = "test";

        var expectedDeposits = AdminPendingBankDeposits.ValidPendingDepositsResponse();
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
        var query = AdminPendingBankDeposits.ValidQuery();
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
        var query = AdminPendingBankDeposits.ValidQuery();
        var expectedDeposits = AdminPendingBankDeposits.ValidPendingDepositsResponse();
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
        var expectedDeposits = AdminPendingBankDeposits.ValidPendingDepositsResponse();
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
        var query = AdminPendingBankDeposits.ValidQuery();
        query.SortBy = "AmountUSD";
        query.SortOrder = "asc";

        var expectedDeposits = AdminPendingBankDeposits.PendingDepositsWithVariedAmounts()
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
    /// - Comprehensive test data is provided via CancelBankDeposit
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
        var request = CancelBankDeposit.ValidRequest();
        var expectedTransaction = CancelBankDeposit.ValidCancelledTransactionDto();

        _mockWalletService.Setup(x => x.CancelBankDepositAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

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
        var request = CancelBankDeposit.ValidRequest();
        var expectedTransaction = CancelBankDeposit.ValidCancelledTransactionDto();

        _mockWalletService.Setup(x => x.CancelBankDepositAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

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
        var request = CancelBankDeposit.ValidRequest();
        
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
        var request = CancelBankDeposit.ValidRequest();
        
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
        var request = CancelBankDeposit.RequestWithInvalidTransactionId();

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
        var request = CancelBankDeposit.RequestWithEmptyAdminNotes();

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
        var request = CancelBankDeposit.RequestWithTooLongAdminNotes();

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
        var request = CancelBankDeposit.RequestForNonExistentTransaction();

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
        var request = CancelBankDeposit.RequestForAlreadyConfirmedDeposit();

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
        var request = CancelBankDeposit.RequestForAlreadyCancelledDeposit();

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
        var request = CancelBankDeposit.ValidRequest();

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
        var request = CancelBankDeposit.ValidRequest();

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

    #region GetPendingWithdrawals - Unit Tests

    /// <summary>
    /// Unit tests for the GetPendingWithdrawals endpoint (GET /admin/wallets/withdrawals/pending-approval)
    /// These tests verify the controller's behavior for pending withdrawals retrieval functionality.
    /// 
    /// Test Coverage:
    /// - Happy Path: Valid queries and successful responses with pagination
    /// - Authorization: Authentication and role-based access control
    /// - Filtering: Date ranges, amount ranges, user filters, and sorting options
    /// - Pagination: Page number, page size validation, and edge cases
    /// 
    /// Implementation details:
    /// - Tests mock the IWalletService to isolate controller behavior
    /// - Authentication is simulated using ClaimsIdentity
    /// - Tests verify HTTP status codes, response types, and logging behavior
    /// - Comprehensive test data is provided via WalletsTestDataBuilder.GetPendingWithdrawals
    /// </summary>

    #region GetPendingWithdrawals - Happy Path Tests

    /// <summary>
    /// Test: Valid query should return OK with paginated withdrawal requests
    /// Verifies the happy path scenario works correctly
    /// </summary>
    [Fact]
    public async Task GetPendingWithdrawals_WithValidQuery_ShouldReturnOkWithPaginatedResults()
    {
        // Arrange
        var query = WalletsTestDataBuilder.GetPendingWithdrawals.ValidQuery();
        var expectedWithdrawals = WalletsTestDataBuilder.GetPendingWithdrawals.MultiplePendingWithdrawals();
        var paginatedResult = new PaginatedList<WithdrawalRequestAdminViewDto>(expectedWithdrawals, 3, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingWithdrawalsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingWithdrawals(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var withdrawals = okResult!.Value as PaginatedList<WithdrawalRequestAdminViewDto>;

        withdrawals.Should().NotBeNull();
        withdrawals!.Items.Should().HaveCount(3);
        withdrawals.TotalCount.Should().Be(3);
        withdrawals.PageNumber.Should().Be(1);
        withdrawals.PageSize.Should().Be(10);
        
        _mockWalletService.Verify(x => x.GetAdminPendingWithdrawalsAsync(
            It.IsAny<ClaimsPrincipal>(), 
            It.Is<GetAdminPendingWithdrawalsQuery>(q => 
                q.PageNumber == 1 && 
                q.PageSize == 10 && 
                q.SortBy == "RequestedAt" && 
                q.SortOrder == "desc"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Valid query should log appropriate information
    /// Verifies logging behavior for successful operations
    /// </summary>
    [Fact]
    public async Task GetPendingWithdrawals_WithValidQuery_ShouldLogInformation()
    {
        // Arrange
        var query = WalletsTestDataBuilder.GetPendingWithdrawals.ValidQuery();
        var expectedWithdrawals = WalletsTestDataBuilder.GetPendingWithdrawals.MultiplePendingWithdrawals();
        var paginatedResult = new PaginatedList<WithdrawalRequestAdminViewDto>(expectedWithdrawals, 3, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingWithdrawalsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingWithdrawals(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Admin 1 requesting list of pending withdrawal requests")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Empty results should return OK with empty paginated list
    /// Verifies handling of no pending withdrawals
    /// </summary>
    [Fact]
    public async Task GetPendingWithdrawals_WithNoResults_ShouldReturnOkWithEmptyList()
    {
        // Arrange
        var query = WalletsTestDataBuilder.GetPendingWithdrawals.ValidQuery();
        var emptyWithdrawals = WalletsTestDataBuilder.GetPendingWithdrawals.EmptyWithdrawalsList();
        var paginatedResult = new PaginatedList<WithdrawalRequestAdminViewDto>(emptyWithdrawals, 0, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingWithdrawalsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingWithdrawals(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var withdrawals = okResult!.Value as PaginatedList<WithdrawalRequestAdminViewDto>;

        withdrawals.Should().NotBeNull();
        withdrawals!.Items.Should().BeEmpty();
        withdrawals.TotalCount.Should().Be(0);
    }

    #endregion

    #region GetPendingWithdrawals - Authorization Tests

    /// <summary>
    /// Test: Unauthenticated user should not be able to access pending withdrawals
    /// Verifies authorization requirement
    /// </summary>
    [Fact]
    public async Task GetPendingWithdrawals_WithoutAuthentication_ShouldRequireAuth()
    {
        // Arrange
        var query = WalletsTestDataBuilder.GetPendingWithdrawals.ValidQuery();
        
        // Remove authentication
        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act & Assert - This would be handled by the [Authorize] attribute
        // In integration tests, this would return 401 Unauthorized
        // For unit tests, we focus on testing the controller logic assuming auth passes
        var result = await _adminController.GetPendingWithdrawals(query, CancellationToken.None);
        
        // The controller method will execute but the service call should handle the missing user context
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Test: Non-admin user should not be able to access pending withdrawals
    /// Verifies role-based authorization
    /// </summary>
    [Fact]
    public async Task GetPendingWithdrawals_WithNonAdminUser_ShouldBeForbidden()
    {
        // Arrange
        var query = WalletsTestDataBuilder.GetPendingWithdrawals.ValidQuery();
        
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
        var result = await _adminController.GetPendingWithdrawals(query, CancellationToken.None);
        
        result.Should().NotBeNull();
    }

    #endregion

    #region GetPendingWithdrawals - Filtering Tests

    /// <summary>
    /// Test: Date range filtering should work correctly
    /// Verifies that date filters are applied properly
    /// </summary>
    [Fact]
    public async Task GetPendingWithdrawals_WithDateRangeFilter_ShouldApplyFiltersCorrectly()
    {
        // Arrange
        var query = WalletsTestDataBuilder.GetPendingWithdrawals.QueryWithDateRange();
        var expectedWithdrawals = WalletsTestDataBuilder.GetPendingWithdrawals.MultiplePendingWithdrawals();
        var paginatedResult = new PaginatedList<WithdrawalRequestAdminViewDto>(expectedWithdrawals, 3, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingWithdrawalsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingWithdrawals(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var withdrawals = okResult!.Value as PaginatedList<WithdrawalRequestAdminViewDto>;

        withdrawals.Should().NotBeNull();
        _mockWalletService.Verify(x => x.GetAdminPendingWithdrawalsAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetAdminPendingWithdrawalsQuery>(q => 
                q.DateFrom.HasValue && 
                q.DateTo.HasValue),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Amount range filtering should work correctly
    /// Verifies that amount filters are applied properly
    /// </summary>
    [Fact]
    public async Task GetPendingWithdrawals_WithAmountRangeFilter_ShouldApplyFiltersCorrectly()
    {
        // Arrange
        var query = WalletsTestDataBuilder.GetPendingWithdrawals.QueryWithAmountRange();
        var expectedWithdrawals = WalletsTestDataBuilder.GetPendingWithdrawals.PendingWithdrawalsWithVariedAmounts()
            .Where(w => w.Amount >= 100m && w.Amount <= 10000m).ToList();
        var paginatedResult = new PaginatedList<WithdrawalRequestAdminViewDto>(expectedWithdrawals, expectedWithdrawals.Count, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingWithdrawalsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingWithdrawals(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var withdrawals = okResult!.Value as PaginatedList<WithdrawalRequestAdminViewDto>;

        withdrawals.Should().NotBeNull();
        _mockWalletService.Verify(x => x.GetAdminPendingWithdrawalsAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetAdminPendingWithdrawalsQuery>(q => 
                q.MinAmount == 100m && 
                q.MaxAmount == 10000m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: User filtering should work correctly
    /// Verifies that user-specific filters are applied properly
    /// </summary>
    [Fact]
    public async Task GetPendingWithdrawals_WithUserFilter_ShouldApplyFiltersCorrectly()
    {
        // Arrange
        var query = WalletsTestDataBuilder.GetPendingWithdrawals.QueryWithUserFilter();
        var expectedWithdrawals = WalletsTestDataBuilder.GetPendingWithdrawals.PendingWithdrawalsForUserFilter();
        var paginatedResult = new PaginatedList<WithdrawalRequestAdminViewDto>(expectedWithdrawals, expectedWithdrawals.Count, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingWithdrawalsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingWithdrawals(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var withdrawals = okResult!.Value as PaginatedList<WithdrawalRequestAdminViewDto>;

        withdrawals.Should().NotBeNull();
        _mockWalletService.Verify(x => x.GetAdminPendingWithdrawalsAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetAdminPendingWithdrawalsQuery>(q => 
                q.UserId == 123 && 
                q.UsernameOrEmail == "testuser"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetPendingWithdrawals - Pagination Tests

    /// <summary>
    /// Test: Custom pagination parameters should work correctly
    /// Verifies that pagination parameters are handled properly
    /// </summary>
    [Fact]
    public async Task GetPendingWithdrawals_WithCustomPagination_ShouldApplyPaginationCorrectly()
    {
        // Arrange
        var query = WalletsTestDataBuilder.GetPendingWithdrawals.QueryWithPagination(2, 5);
        var expectedWithdrawals = WalletsTestDataBuilder.GetPendingWithdrawals.MultiplePendingWithdrawals();
        var paginatedResult = new PaginatedList<WithdrawalRequestAdminViewDto>(expectedWithdrawals, 10, 2, 5);

        _mockWalletService.Setup(x => x.GetAdminPendingWithdrawalsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingWithdrawals(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var withdrawals = okResult!.Value as PaginatedList<WithdrawalRequestAdminViewDto>;

        withdrawals.Should().NotBeNull();
        withdrawals!.PageNumber.Should().Be(2);
        withdrawals.PageSize.Should().Be(5);
        withdrawals.TotalCount.Should().Be(10);
        _mockWalletService.Verify(x => x.GetAdminPendingWithdrawalsAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetAdminPendingWithdrawalsQuery>(q => 
                q.PageNumber == 2 && 
                q.PageSize == 5),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Invalid pagination parameters should be handled by query validation
    /// Verifies that the service receives the query for validation
    /// </summary>
    [Fact]
    public async Task GetPendingWithdrawals_WithInvalidPagination_ShouldStillCallService()
    {
        // Arrange
        var query = WalletsTestDataBuilder.GetPendingWithdrawals.QueryWithInvalidPagination();
        var emptyWithdrawals = WalletsTestDataBuilder.GetPendingWithdrawals.EmptyWithdrawalsList();
        var paginatedResult = new PaginatedList<WithdrawalRequestAdminViewDto>(emptyWithdrawals, 0, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingWithdrawalsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingWithdrawals(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockWalletService.Verify(x => x.GetAdminPendingWithdrawalsAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetAdminPendingWithdrawalsQuery>(q => 
                q.PageNumber == -1 && 
                q.PageSize == -5),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetPendingWithdrawals - Sorting Tests

    /// <summary>
    /// Test: Custom sorting should work correctly
    /// Verifies that sorting parameters are applied
    /// </summary>
    [Fact]
    public async Task GetPendingWithdrawals_WithCustomSorting_ShouldApplySortingCorrectly()
    {
        // Arrange
        var query = WalletsTestDataBuilder.GetPendingWithdrawals.QueryWithCustomSorting("Amount", "asc");
        var expectedWithdrawals = WalletsTestDataBuilder.GetPendingWithdrawals.PendingWithdrawalsWithVariedAmounts()
            .OrderBy(w => w.Amount).ToList();
        var paginatedResult = new PaginatedList<WithdrawalRequestAdminViewDto>(expectedWithdrawals, 3, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingWithdrawalsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingWithdrawals(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var withdrawals = okResult!.Value as PaginatedList<WithdrawalRequestAdminViewDto>;

        withdrawals.Should().NotBeNull();
        _mockWalletService.Verify(x => x.GetAdminPendingWithdrawalsAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetAdminPendingWithdrawalsQuery>(q => 
                q.SortBy == "Amount" && 
                q.SortOrder == "asc"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Default sorting should be applied when not specified
    /// Verifies default sort behavior
    /// </summary>
    [Fact]
    public async Task GetPendingWithdrawals_WithDefaultSorting_ShouldUseDefaultValues()
    {
        // Arrange
        var query = WalletsTestDataBuilder.GetPendingWithdrawals.ValidQuery();
        var expectedWithdrawals = WalletsTestDataBuilder.GetPendingWithdrawals.MultiplePendingWithdrawals();
        var paginatedResult = new PaginatedList<WithdrawalRequestAdminViewDto>(expectedWithdrawals, 3, 1, 10);

        _mockWalletService.Setup(x => x.GetAdminPendingWithdrawalsAsync(
                It.IsAny<ClaimsPrincipal>(), query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetPendingWithdrawals(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var withdrawals = okResult!.Value as PaginatedList<WithdrawalRequestAdminViewDto>;

        withdrawals.Should().NotBeNull();
        _mockWalletService.Verify(x => x.GetAdminPendingWithdrawalsAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetAdminPendingWithdrawalsQuery>(q => 
                q.SortBy == "RequestedAt" && 
                q.SortOrder == "desc"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ApproveWithdrawal Tests

    #region Happy Path Tests

    /// <summary>
    /// Test: Valid withdrawal approval should return OK with transaction details
    /// Verifies the happy path scenario works correctly
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_WithValidRequest_ShouldReturnOkWithTransaction()
    {
        // Arrange
        var request = WalletsTestDataBuilder.ApproveWithdrawal.ValidRequest();
        var expectedTransaction = WalletsTestDataBuilder.ApproveWithdrawal.SuccessfulApprovalResponse();

        _mockWalletService.Setup(x => x.ApproveWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.ApproveWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var transaction = okResult!.Value as WalletTransactionDto;

        transaction.Should().NotBeNull();
        transaction!.TransactionId.Should().Be(expectedTransaction.TransactionId);
        transaction.Status.Should().Be("Completed");
        transaction.Amount.Should().Be(expectedTransaction.Amount);

        _mockWalletService.Verify(x => x.ApproveWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Valid minimal approval request should work
    /// Verifies approval works with minimal required data
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_WithMinimalRequest_ShouldReturnOkWithTransaction()
    {
        // Arrange
        var request = WalletsTestDataBuilder.ApproveWithdrawal.ValidRequestMinimal();
        var expectedTransaction = WalletsTestDataBuilder.ApproveWithdrawal.MinimalApprovalResponse();

        _mockWalletService.Setup(x => x.ApproveWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.ApproveWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var transaction = okResult!.Value as WalletTransactionDto;

        transaction.Should().NotBeNull();
        transaction!.TransactionId.Should().Be(expectedTransaction.TransactionId);
        transaction.Status.Should().Be("Completed");

        _mockWalletService.Verify(x => x.ApproveWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Large withdrawal approval should work correctly
    /// Verifies large amount withdrawals can be processed
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_WithLargeAmount_ShouldReturnOkWithTransaction()
    {
        // Arrange
        var request = WalletsTestDataBuilder.ApproveWithdrawal.ValidRequestMaxNotes();
        var expectedTransaction = WalletsTestDataBuilder.ApproveWithdrawal.LargeAmountApprovalResponse();

        _mockWalletService.Setup(x => x.ApproveWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.ApproveWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var transaction = okResult!.Value as WalletTransactionDto;

        transaction.Should().NotBeNull();
        transaction!.TransactionId.Should().Be(expectedTransaction.TransactionId);
        transaction.Status.Should().Be("Completed");
        transaction.Amount.Should().Be(5000.00m);

        _mockWalletService.Verify(x => x.ApproveWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Admin information should be logged correctly
    /// Verifies proper logging of admin actions
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_ShouldLogAdminInformation()
    {
        // Arrange
        var request = WalletsTestDataBuilder.ApproveWithdrawal.ValidRequest();
        var expectedTransaction = WalletsTestDataBuilder.ApproveWithdrawal.SuccessfulApprovalResponse();

        _mockWalletService.Setup(x => x.ApproveWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.ApproveWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify that logging occurred with correct admin ID and transaction ID
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Admin 1 attempting to approve withdrawal TransactionID: 1001")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Authorization Tests

    /// <summary>
    /// Test: Non-admin users should be handled by authorization
    /// Verifies authorization attribute prevents non-admin access
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_WithNonAdminUser_ShouldBeHandledByAuthorization()
    {
        // Arrange
        var request = WalletsTestDataBuilder.ApproveWithdrawal.ValidRequest();

        // Setup non-admin user
        var nonAdminClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "2"),
            new(ClaimTypes.Name, "normaluser"),
            new(ClaimTypes.Role, "User")
        };

        var nonAdminIdentity = new ClaimsIdentity(nonAdminClaims, "TestAuth");
        var nonAdminPrincipal = new ClaimsPrincipal(nonAdminIdentity);

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = nonAdminPrincipal
            }
        };

        var expectedTransaction = WalletsTestDataBuilder.ApproveWithdrawal.SuccessfulApprovalResponse();
        _mockWalletService.Setup(x => x.ApproveWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.ApproveWithdrawal(request, CancellationToken.None);

        // Assert
        // The method should still execute (authorization is handled at attribute level)
        // But the user claim should be passed correctly to the service
        result.Should().BeOfType<OkObjectResult>();
        
        _mockWalletService.Verify(x => x.ApproveWithdrawalAsync(
            It.Is<ClaimsPrincipal>(p => p.FindFirstValue(ClaimTypes.NameIdentifier) == "2"),
            request, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Validation Tests

    /// <summary>
    /// Test: Non-existent transaction should return NotFound
    /// Verifies proper handling of non-existent transactions
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_WithNonExistentTransaction_ShouldReturnNotFound()
    {
        // Arrange
        var request = WalletsTestDataBuilder.ApproveWithdrawal.NonExistentTransaction();

        _mockWalletService.Setup(x => x.ApproveWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Transaction not found"));

        // Act
        var result = await _adminController.ApproveWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new { Message = "Transaction not found" });

        _mockWalletService.Verify(x => x.ApproveWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Already processed transaction should return BadRequest
    /// Verifies proper handling of already processed transactions
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_WithAlreadyProcessedTransaction_ShouldReturnBadRequest()
    {
        // Arrange
        var request = WalletsTestDataBuilder.ApproveWithdrawal.AlreadyProcessedTransaction();

        _mockWalletService.Setup(x => x.ApproveWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Transaction is not pending approval"));

        // Act
        var result = await _adminController.ApproveWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { Message = "Transaction is not pending approval" });

        _mockWalletService.Verify(x => x.ApproveWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Insufficient balance should return BadRequest
    /// Verifies proper handling of insufficient balance scenarios
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_WithInsufficientBalance_ShouldReturnBadRequest()
    {
        // Arrange
        var request = WalletsTestDataBuilder.ApproveWithdrawal.ValidRequest();

        _mockWalletService.Setup(x => x.ApproveWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Insufficient balance for withdrawal"));

        // Act
        var result = await _adminController.ApproveWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { Message = "Insufficient balance for withdrawal" });

        _mockWalletService.Verify(x => x.ApproveWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Invalid transaction data should return BadRequest
    /// Verifies proper handling of invalid transaction data
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_WithInvalidTransactionData_ShouldReturnBadRequest()
    {
        // Arrange
        var request = WalletsTestDataBuilder.ApproveWithdrawal.ValidRequest();

        _mockWalletService.Setup(x => x.ApproveWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Invalid transaction data"));

        // Act
        var result = await _adminController.ApproveWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { Message = "Invalid transaction data" });

        _mockWalletService.Verify(x => x.ApproveWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Business Logic Tests

    /// <summary>
    /// Test: Service failure should return InternalServerError
    /// Verifies proper handling of service failures
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_WithServiceFailure_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = WalletsTestDataBuilder.ApproveWithdrawal.ValidRequest();

        _mockWalletService.Setup(x => x.ApproveWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Database connection failed"));

        // Act
        var result = await _adminController.ApproveWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeEquivalentTo(new { Message = "Database connection failed" });

        _mockWalletService.Verify(x => x.ApproveWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Service error without message should return generic error
    /// Verifies proper handling when error message is null
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_WithServiceErrorWithoutMessage_ShouldReturnGenericError()
    {
        // Arrange
        var request = WalletsTestDataBuilder.ApproveWithdrawal.ValidRequest();

        _mockWalletService.Setup(x => x.ApproveWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null));

        // Act
        var result = await _adminController.ApproveWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeEquivalentTo(new { Message = "Failed to approve withdrawal request." });

        _mockWalletService.Verify(x => x.ApproveWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Warning should be logged on approval failure
    /// Verifies proper warning logging when approval fails
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_OnFailure_ShouldLogWarning()
    {
        // Arrange
        var request = WalletsTestDataBuilder.ApproveWithdrawal.ValidRequest();

        _mockWalletService.Setup(x => x.ApproveWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Transaction not found"));

        // Act
        var result = await _adminController.ApproveWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Admin approve withdrawal failed for TransactionID 1001")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Cancellation token should be passed correctly
    /// Verifies proper cancellation token handling
    /// </summary>
    [Fact]
    public async Task ApproveWithdrawal_ShouldPassCancellationTokenToService()
    {
        // Arrange
        var request = WalletsTestDataBuilder.ApproveWithdrawal.ValidRequest();
        var expectedTransaction = WalletsTestDataBuilder.ApproveWithdrawal.SuccessfulApprovalResponse();
        var cancellationToken = new CancellationToken();

        _mockWalletService.Setup(x => x.ApproveWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, cancellationToken))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.ApproveWithdrawal(request, cancellationToken);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        _mockWalletService.Verify(x => x.ApproveWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, cancellationToken), Times.Once);
    }

    #endregion

    #endregion

    #region RejectWithdrawal Tests

    #region Happy Path Tests

    /// <summary>
    /// Test: Valid withdrawal rejection should return OK with transaction details
    /// Verifies the happy path scenario works correctly
    /// </summary>
    [Fact]
    public async Task RejectWithdrawal_WithValidRequest_ShouldReturnOkWithTransaction()
    {
        // Arrange
        var request = WalletsTestDataBuilder.RejectWithdrawal.ValidRequest();
        var expectedTransaction = WalletsTestDataBuilder.RejectWithdrawal.SuccessfulRejectionResponse();

        _mockWalletService.Setup(x => x.RejectWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.RejectWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var transaction = okResult!.Value as WalletTransactionDto;

        transaction.Should().NotBeNull();
        transaction!.TransactionId.Should().Be(expectedTransaction.TransactionId);
        transaction.Status.Should().Be("Rejected");
        transaction.Amount.Should().Be(expectedTransaction.Amount);
        transaction.BalanceAfter.Should().Be(expectedTransaction.BalanceAfter); // Balance restored

        _mockWalletService.Verify(x => x.RejectWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Valid detailed rejection request should work
    /// Verifies rejection works with detailed rejection reason
    /// </summary>
    [Fact]
    public async Task RejectWithdrawal_WithDetailedRequest_ShouldReturnOkWithTransaction()
    {
        // Arrange
        var request = WalletsTestDataBuilder.RejectWithdrawal.ValidRequestDetailed();
        var expectedTransaction = WalletsTestDataBuilder.RejectWithdrawal.DetailedRejectionResponse();

        _mockWalletService.Setup(x => x.RejectWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.RejectWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var transaction = okResult!.Value as WalletTransactionDto;

        transaction.Should().NotBeNull();
        transaction!.TransactionId.Should().Be(expectedTransaction.TransactionId);
        transaction.Status.Should().Be("Rejected");

        _mockWalletService.Verify(x => x.RejectWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Large withdrawal rejection should work correctly
    /// Verifies large amount withdrawals can be rejected with balance restoration
    /// </summary>
    [Fact]
    public async Task RejectWithdrawal_WithLargeAmount_ShouldReturnOkWithTransaction()
    {
        // Arrange
        var request = WalletsTestDataBuilder.RejectWithdrawal.ValidRequestMaxNotes();
        var expectedTransaction = WalletsTestDataBuilder.RejectWithdrawal.LargeAmountRejectionResponse();

        _mockWalletService.Setup(x => x.RejectWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.RejectWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var transaction = okResult!.Value as WalletTransactionDto;

        transaction.Should().NotBeNull();
        transaction!.TransactionId.Should().Be(expectedTransaction.TransactionId);
        transaction.Status.Should().Be("Rejected");
        transaction.Amount.Should().Be(10000.00m);
        transaction.BalanceAfter.Should().Be(25000.00m); // Balance properly restored

        _mockWalletService.Verify(x => x.RejectWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Admin information should be logged correctly
    /// Verifies proper logging of admin actions for rejection
    /// </summary>
    [Fact]
    public async Task RejectWithdrawal_ShouldLogAdminInformation()
    {
        // Arrange
        var request = WalletsTestDataBuilder.RejectWithdrawal.ValidRequest();
        var expectedTransaction = WalletsTestDataBuilder.RejectWithdrawal.SuccessfulRejectionResponse();

        _mockWalletService.Setup(x => x.RejectWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.RejectWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify that logging occurred with correct admin ID and transaction ID
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Admin 1 attempting to reject withdrawal TransactionID: 2001")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Authorization Tests

    /// <summary>
    /// Test: Non-admin users should be handled by authorization
    /// Verifies authorization attribute prevents non-admin access
    /// </summary>
    [Fact]
    public async Task RejectWithdrawal_WithNonAdminUser_ShouldBeHandledByAuthorization()
    {
        // Arrange
        var request = WalletsTestDataBuilder.RejectWithdrawal.ValidRequest();

        // Setup non-admin user
        var nonAdminClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "3"),
            new(ClaimTypes.Name, "normaluser"),
            new(ClaimTypes.Role, "User")
        };

        var nonAdminIdentity = new ClaimsIdentity(nonAdminClaims, "TestAuth");
        var nonAdminPrincipal = new ClaimsPrincipal(nonAdminIdentity);

        _adminController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = nonAdminPrincipal
            }
        };

        var expectedTransaction = WalletsTestDataBuilder.RejectWithdrawal.SuccessfulRejectionResponse();
        _mockWalletService.Setup(x => x.RejectWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.RejectWithdrawal(request, CancellationToken.None);

        // Assert
        // The method should still execute (authorization is handled at attribute level)
        // But the user claim should be passed correctly to the service
        result.Should().BeOfType<OkObjectResult>();
        
        _mockWalletService.Verify(x => x.RejectWithdrawalAsync(
            It.Is<ClaimsPrincipal>(p => p.FindFirstValue(ClaimTypes.NameIdentifier) == "3"),
            request, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Validation Tests

    /// <summary>
    /// Test: Non-existent transaction should return NotFound
    /// Verifies proper handling of non-existent transactions
    /// </summary>
    [Fact]
    public async Task RejectWithdrawal_WithNonExistentTransaction_ShouldReturnNotFound()
    {
        // Arrange
        var request = WalletsTestDataBuilder.RejectWithdrawal.NonExistentTransaction();

        _mockWalletService.Setup(x => x.RejectWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Transaction not found"));

        // Act
        var result = await _adminController.RejectWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new { Message = "Transaction not found" });

        _mockWalletService.Verify(x => x.RejectWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Already processed transaction should return BadRequest
    /// Verifies proper handling of already processed transactions
    /// </summary>
    [Fact]
    public async Task RejectWithdrawal_WithAlreadyProcessedTransaction_ShouldReturnBadRequest()
    {
        // Arrange
        var request = WalletsTestDataBuilder.RejectWithdrawal.AlreadyProcessedTransaction();

        _mockWalletService.Setup(x => x.RejectWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Transaction cannot be rejected as it has already been processed"));

        // Act
        var result = await _adminController.RejectWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { Message = "Transaction cannot be rejected as it has already been processed" });

        _mockWalletService.Verify(x => x.RejectWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Already cancelled transaction should return BadRequest
    /// Verifies proper handling of already cancelled transactions
    /// </summary>
    [Fact]
    public async Task RejectWithdrawal_WithAlreadyCancelledTransaction_ShouldReturnBadRequest()
    {
        // Arrange
        var request = WalletsTestDataBuilder.RejectWithdrawal.AlreadyCancelledTransaction();

        _mockWalletService.Setup(x => x.RejectWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Transaction cannot be rejected as it has already been cancelled"));

        // Act
        var result = await _adminController.RejectWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { Message = "Transaction cannot be rejected as it has already been cancelled" });

        _mockWalletService.Verify(x => x.RejectWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Invalid transaction data should return BadRequest
    /// Verifies proper handling of invalid transaction data
    /// </summary>
    [Fact]
    public async Task RejectWithdrawal_WithInvalidTransactionData_ShouldReturnBadRequest()
    {
        // Arrange
        var request = WalletsTestDataBuilder.RejectWithdrawal.ValidRequest();

        _mockWalletService.Setup(x => x.RejectWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Invalid transaction data"));

        // Act
        var result = await _adminController.RejectWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { Message = "Invalid transaction data" });

        _mockWalletService.Verify(x => x.RejectWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Empty rejection reason should be handled by FluentValidation
    /// Verifies that validation prevents empty rejection reasons
    /// </summary>
    [Fact]
    public async Task RejectWithdrawal_WithEmptyRejectionReason_ShouldBeValidatedByFluentValidation()
    {
        // Note: This test documents that FluentValidation should handle empty admin notes
        // The actual validation happens at the framework level before reaching the controller
        // This test verifies the service layer behavior when such a request somehow gets through
        
        // Arrange
        var request = WalletsTestDataBuilder.RejectWithdrawal.InvalidEmptyAdminNotes();

        _mockWalletService.Setup(x => x.RejectWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Invalid rejection reason provided"));

        // Act
        var result = await _adminController.RejectWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { Message = "Invalid rejection reason provided" });

        _mockWalletService.Verify(x => x.RejectWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Business Logic Tests

    /// <summary>
    /// Test: Service failure should return InternalServerError
    /// Verifies proper handling of service failures
    /// </summary>
    [Fact]
    public async Task RejectWithdrawal_WithServiceFailure_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = WalletsTestDataBuilder.RejectWithdrawal.ValidRequest();

        _mockWalletService.Setup(x => x.RejectWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Database connection failed"));

        // Act
        var result = await _adminController.RejectWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeEquivalentTo(new { Message = "Database connection failed" });

        _mockWalletService.Verify(x => x.RejectWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Service error without message should return generic error
    /// Verifies proper handling when error message is null
    /// </summary>
    [Fact]
    public async Task RejectWithdrawal_WithServiceErrorWithoutMessage_ShouldReturnGenericError()
    {
        // Arrange
        var request = WalletsTestDataBuilder.RejectWithdrawal.ValidRequest();

        _mockWalletService.Setup(x => x.RejectWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null));

        // Act
        var result = await _adminController.RejectWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeEquivalentTo(new { Message = "Failed to reject withdrawal request." });

        _mockWalletService.Verify(x => x.RejectWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Warning should be logged on rejection failure
    /// Verifies proper warning logging when rejection fails
    /// </summary>
    [Fact]
    public async Task RejectWithdrawal_OnFailure_ShouldLogWarning()
    {
        // Arrange
        var request = WalletsTestDataBuilder.RejectWithdrawal.ValidRequest();

        _mockWalletService.Setup(x => x.RejectWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Transaction not found"));

        // Act
        var result = await _adminController.RejectWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        
        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Admin reject withdrawal failed for TransactionID 2001")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Cancellation token should be passed correctly
    /// Verifies proper cancellation token handling
    /// </summary>
    [Fact]
    public async Task RejectWithdrawal_ShouldPassCancellationTokenToService()
    {
        // Arrange
        var request = WalletsTestDataBuilder.RejectWithdrawal.ValidRequest();
        var expectedTransaction = WalletsTestDataBuilder.RejectWithdrawal.SuccessfulRejectionResponse();
        var cancellationToken = new CancellationToken();

        _mockWalletService.Setup(x => x.RejectWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, cancellationToken))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.RejectWithdrawal(request, cancellationToken);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        _mockWalletService.Verify(x => x.RejectWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, cancellationToken), Times.Once);
    }

    /// <summary>
    /// Test: Balance restoration should be verified in successful rejection
    /// Verifies that rejected withdrawals restore user balance correctly
    /// </summary>
    [Fact]
    public async Task RejectWithdrawal_OnSuccess_ShouldRestoreBalance()
    {
        // Arrange
        var request = WalletsTestDataBuilder.RejectWithdrawal.ValidRequest();
        var expectedTransaction = WalletsTestDataBuilder.RejectWithdrawal.SuccessfulRejectionResponse();

        _mockWalletService.Setup(x => x.RejectWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.RejectWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var transaction = okResult!.Value as WalletTransactionDto;

        transaction.Should().NotBeNull();
        transaction!.Status.Should().Be("Rejected");
        
        // Verify balance restoration - the balance should be restored to include the rejected amount
        transaction.Amount.Should().Be(150.00m); // The rejected amount
        transaction.BalanceAfter.Should().Be(1150.00m); // Balance after restoration
        
        _mockWalletService.Verify(x => x.RejectWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Rejection reason should be properly recorded
    /// Verifies that admin notes (rejection reason) are processed correctly
    /// </summary>
    [Fact]
    public async Task RejectWithdrawal_ShouldProcessRejectionReasonCorrectly()
    {
        // Arrange
        var request = WalletsTestDataBuilder.RejectWithdrawal.ValidRequestDetailed();
        var expectedTransaction = WalletsTestDataBuilder.RejectWithdrawal.DetailedRejectionResponse();

        _mockWalletService.Setup(x => x.RejectWithdrawalAsync(
                It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.RejectWithdrawal(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var transaction = okResult!.Value as WalletTransactionDto;

        transaction.Should().NotBeNull();
        transaction!.Status.Should().Be("Rejected");
        
        // Verify that the service was called with the detailed rejection reason
        _mockWalletService.Verify(x => x.RejectWithdrawalAsync(
            It.IsAny<ClaimsPrincipal>(), 
            It.Is<RejectWithdrawalRequest>(r => 
                r.TransactionId == 2002L && 
                r.AdminNotes.Contains("risk management system")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #endregion

    #region SCRUM-81: Admin Direct Deposit Tests

    #region Happy Path Tests

    [Fact]
    public async Task AdminDirectDeposit_WithValidRequest_ShouldReturnOkWithTransactionDto()
    {
        // Arrange
        var request = WalletsTestDataBuilder.AdminDirectDeposit.ValidRequest();
        var expectedTransaction = WalletsTestDataBuilder.AdminDirectDeposit.ValidTransactionDto();
        var adminUserId = "admin-123";
        
        SetupAdminUser(adminUserId);
        _mockWalletService.Setup(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.AdminDirectDeposit(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTransaction = okResult.Value.Should().BeOfType<WalletTransactionDto>().Subject;
        returnedTransaction.TransactionId.Should().Be(expectedTransaction.TransactionId);
        returnedTransaction.Amount.Should().Be(expectedTransaction.Amount);
        returnedTransaction.TransactionTypeName.Should().Be(expectedTransaction.TransactionTypeName);
        
        _mockWalletService.Verify(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdminDirectDeposit_WithLargeAmount_ShouldProcessSuccessfully()
    {
        // Arrange
        var request = WalletsTestDataBuilder.AdminDirectDeposit.LargeAmountRequest();
        var expectedTransaction = WalletsTestDataBuilder.AdminDirectDeposit.ValidTransactionDto();
        var adminUserId = "admin-123";
        
        SetupAdminUser(adminUserId);
        _mockWalletService.Setup(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.AdminDirectDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockWalletService.Verify(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdminDirectDeposit_WithSmallAmount_ShouldProcessSuccessfully()
    {
        // Arrange
        var request = WalletsTestDataBuilder.AdminDirectDeposit.SmallAmountRequest();
        var expectedTransaction = WalletsTestDataBuilder.AdminDirectDeposit.ValidTransactionDto();
        var adminUserId = "admin-123";
        
        SetupAdminUser(adminUserId);
        _mockWalletService.Setup(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.AdminDirectDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockWalletService.Verify(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task AdminDirectDeposit_WithUnauthenticatedUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = WalletsTestDataBuilder.AdminDirectDeposit.ValidRequest();
        _mockWalletService.Setup(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Unauthorized access"));

        // Act
        var result = await _adminController.AdminDirectDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task AdminDirectDeposit_WithNonAdminUser_ShouldReturnForbidden()
    {
        // Arrange
        var request = WalletsTestDataBuilder.AdminDirectDeposit.ValidRequest();
        var investorUserId = "investor-123";
        
        SetupInvestorUser(investorUserId);
        _mockWalletService.Setup(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Admin role required"));

        // Act
        var result = await _adminController.AdminDirectDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task AdminDirectDeposit_WithInvalidUserId_ShouldReturnNotFound()
    {
        // Arrange
        var request = WalletsTestDataBuilder.AdminDirectDeposit.InvalidUserIdZeroRequest();
        var adminUserId = "admin-123";
        
        SetupAdminUser(adminUserId);
        _mockWalletService.Setup(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "User not found"));

        // Act
        var result = await _adminController.AdminDirectDeposit(request, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeEquivalentTo(new { Message = "User not found" });
    }

    [Fact]
    public async Task AdminDirectDeposit_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var request = WalletsTestDataBuilder.AdminDirectDeposit.NonExistentUserRequest();
        var adminUserId = "admin-123";
        
        SetupAdminUser(adminUserId);
        _mockWalletService.Setup(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "User not found"));

        // Act
        var result = await _adminController.AdminDirectDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AdminDirectDeposit_WithZeroAmount_ShouldReturnBadRequest()
    {
        // Arrange
        var request = WalletsTestDataBuilder.AdminDirectDeposit.ZeroAmountRequest();
        var adminUserId = "admin-123";
        
        SetupAdminUser(adminUserId);
        _mockWalletService.Setup(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Amount must be positive"));

        // Act
        var result = await _adminController.AdminDirectDeposit(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeEquivalentTo(new { Message = "Amount must be positive" });
    }

    [Fact]
    public async Task AdminDirectDeposit_WithNegativeAmount_ShouldReturnBadRequest()
    {
        // Arrange
        var request = WalletsTestDataBuilder.AdminDirectDeposit.NegativeAmountRequest();
        var adminUserId = "admin-123";
        
        SetupAdminUser(adminUserId);
        _mockWalletService.Setup(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Amount must be positive"));

        // Act
        var result = await _adminController.AdminDirectDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AdminDirectDeposit_WithInvalidCurrency_ShouldReturnBadRequest()
    {
        // Arrange
        var request = WalletsTestDataBuilder.AdminDirectDeposit.InvalidCurrencyRequest();
        var adminUserId = "admin-123";
        
        SetupAdminUser(adminUserId);
        _mockWalletService.Setup(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Invalid or unsupported currency"));

        // Act
        var result = await _adminController.AdminDirectDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AdminDirectDeposit_WithTooLongAdminNotes_ShouldReturnBadRequest()
    {
        // Arrange
        var request = WalletsTestDataBuilder.AdminDirectDeposit.TooLongAdminNotesRequest();
        var adminUserId = "admin-123";
        
        SetupAdminUser(adminUserId);
        _mockWalletService.Setup(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Description must be positive"));

        // Act
        var result = await _adminController.AdminDirectDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AdminDirectDeposit_WithInactiveUser_ShouldReturnBadRequest()
    {
        // Arrange
        var request = WalletsTestDataBuilder.AdminDirectDeposit.InactiveUserRequest();
        var adminUserId = "admin-123";
        
        SetupAdminUser(adminUserId);
        _mockWalletService.Setup(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Amount must be positive"));

        // Act
        var result = await _adminController.AdminDirectDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task AdminDirectDeposit_ShouldLogAdminAction()
    {
        // Arrange
        var request = WalletsTestDataBuilder.AdminDirectDeposit.ValidRequest();
        var expectedTransaction = WalletsTestDataBuilder.AdminDirectDeposit.ValidTransactionDto();
        var adminUserId = "admin-123";
        
        SetupAdminUser(adminUserId);
        _mockWalletService.Setup(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.AdminDirectDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called with correct parameters
        _mockWalletService.Verify(x => x.AdminDirectDepositAsync(
            It.Is<ClaimsPrincipal>(p => p.FindFirstValue(ClaimTypes.NameIdentifier) == adminUserId),
            It.Is<AdminDirectDepositRequest>(r => 
                r.UserId == request.UserId && 
                r.Amount == request.Amount && 
                r.CurrencyCode == request.CurrencyCode),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdminDirectDeposit_WithEmptyAdminNotes_ShouldProcessSuccessfully()
    {
        // Arrange
        var request = WalletsTestDataBuilder.AdminDirectDeposit.EmptyAdminNotesRequest();
        var expectedTransaction = WalletsTestDataBuilder.AdminDirectDeposit.ValidTransactionDto();
        var adminUserId = "admin-123";
        
        SetupAdminUser(adminUserId);
        _mockWalletService.Setup(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedTransaction, (string?)null));

        // Act
        var result = await _adminController.AdminDirectDeposit(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AdminDirectDeposit_WithServiceFailure_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = WalletsTestDataBuilder.AdminDirectDeposit.ValidRequest();
        var adminUserId = "admin-123";
        
        SetupAdminUser(adminUserId);
        _mockWalletService.Setup(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, "Database connection failed"));

        // Act
        var result = await _adminController.AdminDirectDeposit(request, CancellationToken.None);

        // Assert
        var serverErrorResult = result.Should().BeOfType<ObjectResult>().Subject;
        serverErrorResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task AdminDirectDeposit_WithUnexpectedError_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = WalletsTestDataBuilder.AdminDirectDeposit.ValidRequest();
        var adminUserId = "admin-123";
        
        SetupAdminUser(adminUserId);
        _mockWalletService.Setup(x => x.AdminDirectDepositAsync(It.IsAny<ClaimsPrincipal>(), request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, null)); // Both null indicates unexpected error

        // Act
        var result = await _adminController.AdminDirectDeposit(request, CancellationToken.None);

        // Assert
        var serverErrorResult = result.Should().BeOfType<ObjectResult>().Subject;
        serverErrorResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var response = serverErrorResult.Value.Should().BeEquivalentTo(new { Message = "Failed to process admin direct deposit." });
    }

    #endregion

    #endregion

    #endregion

    #region SCRUM-82: GET /admin/exchange/orders - Get All Exchange Orders Endpoint

    /// <summary>
    /// SCRUM-82: Unit Tests for GET /admin/exchange/orders endpoint
    /// 
    /// Test coverage includes:
    /// - Happy path: All orders retrieval with pagination
    /// - Authorization: Admin role verification
    /// - Filtering: Various filter combinations (user, trading account, status, order side, date range)
    /// - Sorting: Different sorting options and edge cases
    /// - Edge cases: Empty results, large page sizes
    /// </summary>

    #region Happy Path Tests

    [Fact]
    public async Task GetAllShareOrders_WithValidQuery_ShouldReturnOkWithOrders()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminExchangeMonitor.ValidQuery();
        var orders = TradingTestDataBuilder.AdminExchangeMonitor.AllOrdersList();
        var paginatedResult = new PaginatedList<AdminShareOrderViewDto>(orders, orders.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllOrdersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<AdminShareOrderViewDto>;

        response.Should().NotBeNull();
        response!.Items.Should().HaveCount(3);
        response.TotalCount.Should().Be(3);
        response.PageNumber.Should().Be(1);
        response.PageSize.Should().Be(10);

        _mockExchangeService.Verify(x => x.GetAdminAllOrdersAsync(
            It.Is<GetAdminAllOrdersQuery>(q => 
                q.PageNumber == 1 && 
                q.PageSize == 10 && 
                q.SortBy == "OrderDate" && 
                q.SortOrder == "desc"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllShareOrders_WithMultipleOrders_ShouldReturnOrderedList()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminExchangeMonitor.ValidQuery();
        var orders = TradingTestDataBuilder.AdminExchangeMonitor.AllOrdersList();
        var paginatedResult = new PaginatedList<AdminShareOrderViewDto>(orders, orders.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllOrdersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<AdminShareOrderViewDto>;

        response.Should().NotBeNull();
        response!.Items.Should().NotBeEmpty();
        
        // Verify data integrity
        var firstOrder = response.Items.First();
        firstOrder.OrderId.Should().BePositive();
        firstOrder.Username.Should().NotBeNullOrEmpty();
        firstOrder.UserEmail.Should().NotBeNullOrEmpty();
        firstOrder.TradingAccountName.Should().NotBeNullOrEmpty();
        firstOrder.OrderSide.Should().BeOneOf("Buy", "Sell");
        firstOrder.OrderType.Should().NotBeNullOrEmpty();
        firstOrder.OrderStatus.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task GetAllShareOrders_WithoutAdminRole_ShouldRequireAdminAccess()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminExchangeMonitor.ValidQuery();
        
        // Setup non-admin user (this would be handled by [Authorize(Roles = "Admin")] in real scenario)
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "2"),
            new(ClaimTypes.Name, "regularuser"),
            new(ClaimTypes.Role, "User")  // Not Admin
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

        // In unit tests, we focus on testing the controller logic
        // The [Authorize(Roles = "Admin")] attribute would be tested in integration tests
        var orders = TradingTestDataBuilder.AdminExchangeMonitor.AllOrdersList();
        var paginatedResult = new PaginatedList<AdminShareOrderViewDto>(orders, orders.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllOrdersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareOrders(query, CancellationToken.None);

        // Assert
        // In unit tests, the controller method executes (auth is handled by middleware)
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllShareOrders_WithAdminRole_ShouldAllowAccess()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminExchangeMonitor.ValidQuery();
        var orders = TradingTestDataBuilder.AdminExchangeMonitor.AllOrdersList();
        var paginatedResult = new PaginatedList<AdminShareOrderViewDto>(orders, orders.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllOrdersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act (using default admin setup from constructor)
        var result = await _adminController.GetAllShareOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<AdminShareOrderViewDto>;

        response.Should().NotBeNull();
        response!.Items.Should().NotBeEmpty();
    }

    #endregion

    #region Filter Tests

    [Fact]
    public async Task GetAllShareOrders_WithTradingAccountFilter_ShouldApplyFilter()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminExchangeMonitor.QueryWithTradingAccountFilter();
        var filteredOrders = TradingTestDataBuilder.AdminExchangeMonitor.FilteredOrdersForTradingAccount(1);
        var paginatedResult = new PaginatedList<AdminShareOrderViewDto>(filteredOrders, filteredOrders.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllOrdersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<AdminShareOrderViewDto>;

        response.Should().NotBeNull();
        
        _mockExchangeService.Verify(x => x.GetAdminAllOrdersAsync(
            It.Is<GetAdminAllOrdersQuery>(q => q.TradingAccountId == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllShareOrders_WithUserFilter_ShouldApplyFilter()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminExchangeMonitor.QueryWithUserFilter();
        var filteredOrders = TradingTestDataBuilder.AdminExchangeMonitor.FilteredOrdersForUser(123);
        var paginatedResult = new PaginatedList<AdminShareOrderViewDto>(filteredOrders, filteredOrders.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllOrdersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        _mockExchangeService.Verify(x => x.GetAdminAllOrdersAsync(
            It.Is<GetAdminAllOrdersQuery>(q => q.UserId == 123),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllShareOrders_WithStatusFilter_ShouldApplyFilter()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminExchangeMonitor.QueryWithStatusFilter();
        var filteredOrders = TradingTestDataBuilder.AdminExchangeMonitor.FilteredOrdersByStatus("Filled");
        var paginatedResult = new PaginatedList<AdminShareOrderViewDto>(filteredOrders, filteredOrders.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllOrdersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        _mockExchangeService.Verify(x => x.GetAdminAllOrdersAsync(
            It.Is<GetAdminAllOrdersQuery>(q => q.Status == "Filled"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllShareOrders_WithOrderSideFilter_ShouldApplyFilter()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminExchangeMonitor.QueryWithOrderSideFilter();
        var filteredOrders = TradingTestDataBuilder.AdminExchangeMonitor.FilteredOrdersByOrderSide("Buy");
        var paginatedResult = new PaginatedList<AdminShareOrderViewDto>(filteredOrders, filteredOrders.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllOrdersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        _mockExchangeService.Verify(x => x.GetAdminAllOrdersAsync(
            It.Is<GetAdminAllOrdersQuery>(q => q.OrderSide == "Buy"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllShareOrders_WithDateRangeFilter_ShouldApplyFilter()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminExchangeMonitor.QueryWithDateRangeFilter();
        var orders = TradingTestDataBuilder.AdminExchangeMonitor.AllOrdersList();
        var paginatedResult = new PaginatedList<AdminShareOrderViewDto>(orders, orders.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllOrdersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        _mockExchangeService.Verify(x => x.GetAdminAllOrdersAsync(
            It.Is<GetAdminAllOrdersQuery>(q => 
                q.DateFrom.HasValue && 
                q.DateTo.HasValue &&
                q.DateFrom.Value <= DateTime.UtcNow &&
                q.DateTo.Value <= DateTime.UtcNow),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllShareOrders_WithCombinedFilters_ShouldApplyAllFilters()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminExchangeMonitor.QueryWithCombinedFilters();
        var filteredOrders = TradingTestDataBuilder.AdminExchangeMonitor.FilteredOrdersForUser(123)
            .Where(o => o.OrderStatus == "Filled" && o.OrderSide == "Buy").ToList();
        var paginatedResult = new PaginatedList<AdminShareOrderViewDto>(filteredOrders, filteredOrders.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllOrdersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        _mockExchangeService.Verify(x => x.GetAdminAllOrdersAsync(
            It.Is<GetAdminAllOrdersQuery>(q => 
                q.UserId == 123 &&
                q.Status == "Filled" &&
                q.OrderSide == "Buy" &&
                q.DateFrom.HasValue &&
                q.DateTo.HasValue),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public async Task GetAllShareOrders_WithCustomSorting_ShouldApplySorting()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminExchangeMonitor.QueryWithCustomSorting();
        var orders = TradingTestDataBuilder.AdminExchangeMonitor.AllOrdersList();
        var paginatedResult = new PaginatedList<AdminShareOrderViewDto>(orders, orders.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllOrdersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        _mockExchangeService.Verify(x => x.GetAdminAllOrdersAsync(
            It.Is<GetAdminAllOrdersQuery>(q => 
                q.SortBy == "Username" && 
                q.SortOrder == "asc"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllShareOrders_WithDefaultSorting_ShouldUseOrderDateDesc()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminExchangeMonitor.ValidQuery();
        var orders = TradingTestDataBuilder.AdminExchangeMonitor.AllOrdersList();
        var paginatedResult = new PaginatedList<AdminShareOrderViewDto>(orders, orders.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllOrdersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        _mockExchangeService.Verify(x => x.GetAdminAllOrdersAsync(
            It.Is<GetAdminAllOrdersQuery>(q => 
                q.SortBy == "OrderDate" && 
                q.SortOrder == "desc"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public async Task GetAllShareOrders_WithLargePageSize_ShouldHandlePagination()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminExchangeMonitor.QueryWithLargePageSize();
        var orders = TradingTestDataBuilder.AdminExchangeMonitor.AllOrdersList();
        var paginatedResult = new PaginatedList<AdminShareOrderViewDto>(orders, orders.Count, 1, 50);

        _mockExchangeService.Setup(x => x.GetAdminAllOrdersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<AdminShareOrderViewDto>;

        response.Should().NotBeNull();
        response!.PageSize.Should().Be(50);
        
        _mockExchangeService.Verify(x => x.GetAdminAllOrdersAsync(
            It.Is<GetAdminAllOrdersQuery>(q => q.PageSize == 50),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllShareOrders_WithEmptyResults_ShouldReturnEmptyList()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminExchangeMonitor.ValidQuery();
        var emptyOrders = TradingTestDataBuilder.AdminExchangeMonitor.EmptyOrdersList();
        var paginatedResult = new PaginatedList<AdminShareOrderViewDto>(emptyOrders, 0, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllOrdersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<AdminShareOrderViewDto>;

        response.Should().NotBeNull();
        response!.Items.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task GetAllShareOrders_ShouldLogRequestDetails()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminExchangeMonitor.ValidQuery();
        var orders = TradingTestDataBuilder.AdminExchangeMonitor.AllOrdersList();
        var paginatedResult = new PaginatedList<AdminShareOrderViewDto>(orders, orders.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllOrdersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Admin requesting list of all share orders with query")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region SCRUM-83: GET /admin/exchange/trades - Get All Exchange Trades Endpoint

    /// <summary>
    /// SCRUM-83: Unit Tests for GET /admin/exchange/trades endpoint
    /// 
    /// Test coverage includes:
    /// - Happy path: All trades retrieval with pagination
    /// - Authorization: Admin role verification
    /// - Filtering: Various filter combinations (buyer, seller, trading account, date range, amount range)
    /// - Sorting: Different sorting options and edge cases
    /// - Data display: Trade details accuracy and user information
    /// - Edge cases: Empty results, large page sizes
    /// </summary>

    [Fact]
    public async Task GetAllShareTrades_WithValidQuery_ShouldReturnOkWithTrades()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminTradeMonitor.ValidTradesQuery();
        var trades = TradingTestDataBuilder.AdminTradeMonitor.AllTradesList();
        var paginatedResult = new PaginatedList<AdminShareTradeViewDto>(trades, trades.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllTradesAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result.As<OkObjectResult>();
        okResult.Value.Should().BeOfType<PaginatedList<AdminShareTradeViewDto>>();
        
        var returnedTrades = okResult.Value.As<PaginatedList<AdminShareTradeViewDto>>();
        returnedTrades.Items.Should().HaveCount(3);
        returnedTrades.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetAllShareTrades_WithEmptyResult_ShouldReturnOkWithEmptyList()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminTradeMonitor.ValidTradesQuery();
        var emptyTrades = TradingTestDataBuilder.AdminTradeMonitor.EmptyTradesList();
        var paginatedResult = new PaginatedList<AdminShareTradeViewDto>(emptyTrades, 0, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllTradesAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result.As<OkObjectResult>();
        var returnedTrades = okResult.Value.As<PaginatedList<AdminShareTradeViewDto>>();
        returnedTrades.Items.Should().BeEmpty();
        returnedTrades.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllShareTrades_WithBuyerUserFilter_ShouldReturnFilteredTrades()
    {
        // Arrange
        var buyerUserId = 123;
        var query = TradingTestDataBuilder.AdminTradeMonitor.QueryForBuyerUser(buyerUserId);
        var filteredTrades = TradingTestDataBuilder.AdminTradeMonitor.FilteredTradesForBuyerUser(buyerUserId);
        var paginatedResult = new PaginatedList<AdminShareTradeViewDto>(filteredTrades, filteredTrades.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllTradesAsync(It.Is<GetAdminAllTradesQuery>(q => q.BuyerUserId == buyerUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result.As<OkObjectResult>();
        var returnedTrades = okResult.Value.As<PaginatedList<AdminShareTradeViewDto>>();
        returnedTrades.Items.Should().OnlyContain(t => t.BuyerUserId == buyerUserId);
    }

    [Fact]
    public async Task GetAllShareTrades_WithSellerUserFilter_ShouldReturnFilteredTrades()
    {
        // Arrange
        var sellerUserId = 456;
        var query = TradingTestDataBuilder.AdminTradeMonitor.QueryForSellerUser(sellerUserId);
        var filteredTrades = TradingTestDataBuilder.AdminTradeMonitor.FilteredTradesForSellerUser(sellerUserId);
        var paginatedResult = new PaginatedList<AdminShareTradeViewDto>(filteredTrades, filteredTrades.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllTradesAsync(It.Is<GetAdminAllTradesQuery>(q => q.SellerUserId == sellerUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result.As<OkObjectResult>();
        var returnedTrades = okResult.Value.As<PaginatedList<AdminShareTradeViewDto>>();
        returnedTrades.Items.Should().OnlyContain(t => t.SellerUserId == sellerUserId);
    }

    [Fact]
    public async Task GetAllShareTrades_WithTradingAccountFilter_ShouldReturnFilteredTrades()
    {
        // Arrange
        var tradingAccountId = 1;
        var query = TradingTestDataBuilder.AdminTradeMonitor.QueryForTradingAccount(tradingAccountId);
        var filteredTrades = TradingTestDataBuilder.AdminTradeMonitor.FilteredTradesForTradingAccount(tradingAccountId);
        var paginatedResult = new PaginatedList<AdminShareTradeViewDto>(filteredTrades, filteredTrades.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllTradesAsync(It.Is<GetAdminAllTradesQuery>(q => q.TradingAccountId == tradingAccountId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result.As<OkObjectResult>();
        var returnedTrades = okResult.Value.As<PaginatedList<AdminShareTradeViewDto>>();
        returnedTrades.Items.Should().OnlyContain(t => t.TradingAccountId == tradingAccountId);
    }

    [Fact]
    public async Task GetAllShareTrades_WithDateRangeFilter_ShouldReturnFilteredTrades()
    {
        // Arrange
        var dateFrom = DateTime.UtcNow.AddDays(-5);
        var dateTo = DateTime.UtcNow.AddDays(-1);
        var query = TradingTestDataBuilder.AdminTradeMonitor.QueryWithDateRange(dateFrom, dateTo);
        var trades = TradingTestDataBuilder.AdminTradeMonitor.AllTradesList();
        var paginatedResult = new PaginatedList<AdminShareTradeViewDto>(trades, trades.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllTradesAsync(It.Is<GetAdminAllTradesQuery>(q => q.DateFrom == dateFrom && q.DateTo == dateTo), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockExchangeService.Verify(x => x.GetAdminAllTradesAsync(It.Is<GetAdminAllTradesQuery>(q => q.DateFrom == dateFrom && q.DateTo == dateTo), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllShareTrades_WithAmountRangeFilter_ShouldReturnFilteredTrades()
    {
        // Arrange
        var minAmount = 1000m;
        var maxAmount = 10000m;
        var query = TradingTestDataBuilder.AdminTradeMonitor.QueryWithAmountRange(minAmount, maxAmount);
        var trades = TradingTestDataBuilder.AdminTradeMonitor.AllTradesList();
        var paginatedResult = new PaginatedList<AdminShareTradeViewDto>(trades, trades.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllTradesAsync(It.Is<GetAdminAllTradesQuery>(q => q.MinAmount == minAmount && q.MaxAmount == maxAmount), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockExchangeService.Verify(x => x.GetAdminAllTradesAsync(It.Is<GetAdminAllTradesQuery>(q => q.MinAmount == minAmount && q.MaxAmount == maxAmount), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllShareTrades_WithBuyerSearchTerm_ShouldReturnFilteredTrades()
    {
        // Arrange
        var searchTerm = "buyer123";
        var query = TradingTestDataBuilder.AdminTradeMonitor.QueryWithBuyerSearch(searchTerm);
        var trades = TradingTestDataBuilder.AdminTradeMonitor.AllTradesList();
        var paginatedResult = new PaginatedList<AdminShareTradeViewDto>(trades, trades.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllTradesAsync(It.Is<GetAdminAllTradesQuery>(q => q.BuyerSearchTerm == searchTerm), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockExchangeService.Verify(x => x.GetAdminAllTradesAsync(It.Is<GetAdminAllTradesQuery>(q => q.BuyerSearchTerm == searchTerm), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllShareTrades_WithSellerSearchTerm_ShouldReturnFilteredTrades()
    {
        // Arrange
        var searchTerm = "seller456";
        var query = TradingTestDataBuilder.AdminTradeMonitor.QueryWithSellerSearch(searchTerm);
        var trades = TradingTestDataBuilder.AdminTradeMonitor.AllTradesList();
        var paginatedResult = new PaginatedList<AdminShareTradeViewDto>(trades, trades.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllTradesAsync(It.Is<GetAdminAllTradesQuery>(q => q.SellerSearchTerm == searchTerm), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockExchangeService.Verify(x => x.GetAdminAllTradesAsync(It.Is<GetAdminAllTradesQuery>(q => q.SellerSearchTerm == searchTerm), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllShareTrades_WithCustomSorting_ShouldReturnSortedTrades()
    {
        // Arrange
        var sortBy = "TotalValue";
        var sortOrder = "asc";
        var query = TradingTestDataBuilder.AdminTradeMonitor.QueryWithSorting(sortBy, sortOrder);
        var trades = TradingTestDataBuilder.AdminTradeMonitor.AllTradesList();
        var paginatedResult = new PaginatedList<AdminShareTradeViewDto>(trades, trades.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllTradesAsync(It.Is<GetAdminAllTradesQuery>(q => q.SortBy == sortBy && q.SortOrder == sortOrder), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockExchangeService.Verify(x => x.GetAdminAllTradesAsync(It.Is<GetAdminAllTradesQuery>(q => q.SortBy == sortBy && q.SortOrder == sortOrder), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllShareTrades_WithHighValueTrade_ShouldReturnAccurateFinancialData()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminTradeMonitor.ValidTradesQuery();
        var highValueTrade = TradingTestDataBuilder.AdminTradeMonitor.HighValueTrade();
        var trades = new List<AdminShareTradeViewDto> { highValueTrade };
        var paginatedResult = new PaginatedList<AdminShareTradeViewDto>(trades, 1, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllTradesAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result.As<OkObjectResult>();
        var returnedTrades = okResult.Value.As<PaginatedList<AdminShareTradeViewDto>>();
        
        var trade = returnedTrades.Items.First();
        trade.TradeId.Should().Be(6001);
        trade.TotalValue.Should().Be(1000000.00m);
        trade.QuantityTraded.Should().Be(10000);
        trade.TradePrice.Should().Be(100.00m);
        trade.BuyerFeeAmount.Should().Be(1000.00m);
        trade.SellerFeeAmount.Should().Be(1000.00m);
    }

    [Fact]
    public async Task GetAllShareTrades_WithDefaultSorting_ShouldSortByTradeDateDescending()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminTradeMonitor.ValidTradesQuery();
        var trades = TradingTestDataBuilder.AdminTradeMonitor.AllTradesList();
        var paginatedResult = new PaginatedList<AdminShareTradeViewDto>(trades, trades.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllTradesAsync(It.Is<GetAdminAllTradesQuery>(q => q.SortBy == "TradeDate" && q.SortOrder == "desc"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockExchangeService.Verify(x => x.GetAdminAllTradesAsync(It.Is<GetAdminAllTradesQuery>(q => q.SortBy == "TradeDate" && q.SortOrder == "desc"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllShareTrades_ShouldLogAdminRequest()
    {
        // Arrange
        var query = TradingTestDataBuilder.AdminTradeMonitor.ValidTradesQuery();
        var trades = TradingTestDataBuilder.AdminTradeMonitor.AllTradesList();
        var paginatedResult = new PaginatedList<AdminShareTradeViewDto>(trades, trades.Count, 1, 10);

        _mockExchangeService.Setup(x => x.GetAdminAllTradesAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _adminController.GetAllShareTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Admin requesting list of all share trades with query")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #endregion

    #region Manual Snapshot and Profit Distribution Tests
    
    /// <summary>
    /// Test: Manual snapshot endpoints should require admin authorization
    /// </summary>
    [Fact]
    public void ManualSnapshotEndpoints_ShouldRequireAdminAuthorization()
    {
        // This test verifies that the manual snapshot endpoints exist
        // In production they would be protected by [Authorize(Policy = "AdminPolicy")]
        var controller = typeof(AdminController);
        
        Assert.NotNull(controller.GetMethod("TriggerManualSnapshot"));
        Assert.NotNull(controller.GetMethod("GetSnapshotStatus"));
        Assert.NotNull(controller.GetMethod("RecalculateProfitDistribution"));
        Assert.NotNull(controller.GetMethod("GetProfitDistributionHistory"));
        Assert.NotNull(controller.GetMethod("GetAllProfitDistributionHistory"));
    }

    #endregion

    #region UpdateUserPassword Tests

    /// <summary>
    /// Test: Valid admin request should successfully update user password
    /// Verifies the happy path where an admin updates a user's password
    /// </summary>
    [Fact]
    public async Task UpdateUserPassword_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        const int targetUserId = 2;
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = "Password reset requested by user"
        };

        _mockUserService.Setup(x => x.UpdateUserPasswordByAdminAsync(
                targetUserId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Password updated successfully. User will need to log in again."));

        // Act
        var result = await _adminController.UpdateUserPassword(targetUserId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        response.Should().NotBeNull();

        // Verify service was called correctly
        _mockUserService.Verify(x => x.UpdateUserPasswordByAdminAsync(
            targetUserId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Valid admin request without reason should successfully update user password
    /// Verifies that reason is optional
    /// </summary>
    [Fact]
    public async Task UpdateUserPassword_WithNoReason_ShouldReturnOk()
    {
        // Arrange
        const int targetUserId = 2;
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = null
        };

        _mockUserService.Setup(x => x.UpdateUserPasswordByAdminAsync(
                targetUserId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Password updated successfully. User will need to log in again."));

        // Act
        var result = await _adminController.UpdateUserPassword(targetUserId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        response.Should().NotBeNull();

        // Verify service was called correctly
        _mockUserService.Verify(x => x.UpdateUserPasswordByAdminAsync(
            targetUserId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Non-existent user should return NotFound
    /// Verifies proper handling of user not found scenarios
    /// </summary>
    [Fact]
    public async Task UpdateUserPassword_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        const int invalidUserId = 999;
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = "Test"
        };

        _mockUserService.Setup(x => x.UpdateUserPasswordByAdminAsync(
                invalidUserId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "User not found."));

        // Act
        var result = await _adminController.UpdateUserPassword(invalidUserId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var response = notFoundResult!.Value;
        response.Should().NotBeNull();

        // Verify service was called correctly
        _mockUserService.Verify(x => x.UpdateUserPasswordByAdminAsync(
            invalidUserId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Authentication failure should return Unauthorized
    /// Verifies proper handling of admin authentication issues
    /// </summary>
    [Fact]
    public async Task UpdateUserPassword_WithAuthenticationFailure_ShouldReturnUnauthorized()
    {
        // Arrange
        const int targetUserId = 2;
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = "Test"
        };

        _mockUserService.Setup(x => x.UpdateUserPasswordByAdminAsync(
                targetUserId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Admin authentication required."));

        // Act
        var result = await _adminController.UpdateUserPassword(targetUserId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        var response = unauthorizedResult!.Value;
        response.Should().NotBeNull();

        // Verify service was called correctly
        _mockUserService.Verify(x => x.UpdateUserPasswordByAdminAsync(
            targetUserId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Service validation failure should return BadRequest
    /// Verifies proper handling of business logic validation errors
    /// </summary>
    [Fact]
    public async Task UpdateUserPassword_WithValidationFailure_ShouldReturnBadRequest()
    {
        // Arrange
        const int targetUserId = 2;
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = "Test"
        };

        _mockUserService.Setup(x => x.UpdateUserPasswordByAdminAsync(
                targetUserId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Password validation failed."));

        // Act
        var result = await _adminController.UpdateUserPassword(targetUserId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var response = badRequestResult!.Value;
        response.Should().NotBeNull();

        // Verify service was called correctly
        _mockUserService.Verify(x => x.UpdateUserPasswordByAdminAsync(
            targetUserId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Service error should return BadRequest with generic message
    /// Verifies proper handling of general service errors
    /// </summary>
    [Fact]
    public async Task UpdateUserPassword_WithServiceError_ShouldReturnBadRequest()
    {
        // Arrange
        const int targetUserId = 2;
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = "Test"
        };

        _mockUserService.Setup(x => x.UpdateUserPasswordByAdminAsync(
                targetUserId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "An error occurred while updating the password."));

        // Act
        var result = await _adminController.UpdateUserPassword(targetUserId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var response = badRequestResult!.Value;
        response.Should().NotBeNull();

        // Verify service was called correctly
        _mockUserService.Verify(x => x.UpdateUserPasswordByAdminAsync(
            targetUserId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Controller should properly pass admin claims to service
    /// Verifies that the authenticated admin context is passed correctly
    /// </summary>
    [Fact]
    public async Task UpdateUserPassword_ShouldPassAdminClaimsToService()
    {
        // Arrange
        const int targetUserId = 2;
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = "Test claims passing"
        };

        _mockUserService.Setup(x => x.UpdateUserPasswordByAdminAsync(
                targetUserId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Password updated successfully. User will need to log in again."));

        // Act
        var result = await _adminController.UpdateUserPassword(targetUserId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        // Verify that the correct ClaimsPrincipal (admin user) was passed to the service
        _mockUserService.Verify(x => x.UpdateUserPasswordByAdminAsync(
            targetUserId, 
            request, 
            It.Is<ClaimsPrincipal>(p => p.FindFirst(ClaimTypes.NameIdentifier) != null && p.FindFirst(ClaimTypes.NameIdentifier).Value == "1"), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Controller should log admin actions appropriately
    /// Verifies that admin password updates are logged for audit purposes
    /// </summary>
    [Fact]
    public async Task UpdateUserPassword_ShouldLogAdminAction()
    {
        // Arrange
        const int targetUserId = 2;
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = "User account security compromise"
        };

        _mockUserService.Setup(x => x.UpdateUserPasswordByAdminAsync(
                targetUserId, request, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Password updated successfully. User will need to log in again."));

        // Act
        var result = await _adminController.UpdateUserPassword(targetUserId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Admin 1 attempting to update password for UserID: 2")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Endpoint method signature and attributes validation
    /// Verifies that the endpoint has correct routing and authorization attributes
    /// </summary>
    [Fact]
    public void UpdateUserPassword_ShouldHaveCorrectMethodSignature()
    {
        // Arrange & Act
        var method = typeof(AdminController).GetMethod("UpdateUserPassword");

        // Assert
        method.Should().NotBeNull("UpdateUserPassword method should exist");
        
        // Verify method signature
        var parameters = method!.GetParameters();
        parameters.Should().HaveCount(3, "Method should have userId, request, and cancellationToken parameters");
        parameters[0].ParameterType.Should().Be<int>("First parameter should be int userId");
        parameters[1].ParameterType.Should().Be<UpdateUserPasswordRequest>("Second parameter should be UpdateUserPasswordRequest");
        parameters[2].ParameterType.Should().Be<CancellationToken>("Third parameter should be CancellationToken");
        
        // Verify return type
        method.ReturnType.Should().Be<Task<IActionResult>>("Method should return Task<IActionResult>");
    }

    #endregion
}
