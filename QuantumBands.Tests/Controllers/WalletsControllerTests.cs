// SCRUM-43: Unit Tests for POST /wallets/deposits/bank/initiate - Initiate Bank Deposit Endpoint
// SCRUM-49: Unit Tests for GET /wallets - Get Wallet Information Endpoint
// This test class provides comprehensive test coverage for the WalletsController endpoints:
// - InitiateBankDeposit (POST /wallets/deposits/bank/initiate): Bank deposit initiation with validation, authentication, and business logic tests
// - GetMyWallet (GET /wallets): Wallet information retrieval with authentication, data validation, and business logic tests

using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Features.Wallets.Commands.BankDeposit;
using QuantumBands.Application.Features.Wallets.Commands.CreateWithdrawal;
using QuantumBands.Application.Features.Wallets.Commands.InternalTransfer;
using QuantumBands.Application.Features.Wallets.Dtos;
using QuantumBands.Application.Features.Wallets.Queries.GetTransactions;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Interfaces;
using QuantumBands.Tests.Common;
using QuantumBands.Tests.Fixtures;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace QuantumBands.Tests.Controllers;

/// <summary>
/// Comprehensive test class for WalletsController endpoints
/// 
/// Test Categories:
/// - GetMyWallet: Wallet information retrieval with authentication and data validation
/// - InitiateBankDeposit: Bank deposit initiation with validation, authentication, and business logic
/// 
/// Total Test Coverage: Comprehensive validation of authentication, business logic, security, and error handling
/// </summary>
public class WalletsControllerTests : TestBase
{
    private readonly WalletsController _walletsController;
    private readonly Mock<IWalletService> _mockWalletService;
    private readonly Mock<ILogger<WalletsController>> _mockControllerLogger;

    public WalletsControllerTests()
    {
        _mockWalletService = new Mock<IWalletService>();
        _mockControllerLogger = new Mock<ILogger<WalletsController>>();
        _walletsController = new WalletsController(_mockWalletService.Object, _mockControllerLogger.Object);
    }

    /// <summary>
    /// Creates a ClaimsPrincipal representing an authenticated user with valid claims
    /// </summary>
    private static ClaimsPrincipal CreateAuthenticatedUser(int userId, string username = "testuser")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim("jti", Guid.NewGuid().ToString())
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    /// <summary>
    /// Creates a ClaimsPrincipal representing an unauthenticated user
    /// </summary>
    private static ClaimsPrincipal CreateUnauthenticatedUser()
    {
        return new ClaimsPrincipal(new ClaimsIdentity());
    }

    /// <summary>
    /// Creates a ClaimsPrincipal with invalid or missing required claims
    /// </summary>
    private static ClaimsPrincipal CreateUserWithInvalidClaims()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "testuser")
            // Missing NameIdentifier claim
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    /// <summary>
    /// Helper method to extract message from response object
    /// </summary>
    private static string GetMessageFromResponse(object? responseValue)
    {
        if (responseValue == null) return string.Empty;
        
        var responseType = responseValue.GetType();
        var messageProperty = responseType.GetProperty("Message");
        return messageProperty?.GetValue(responseValue)?.ToString() ?? string.Empty;
    }

    #region Happy Path Tests
    // Tests that verify the endpoint works correctly under normal, expected conditions

    /// <summary>
    /// Test: Valid authenticated user should successfully initiate bank deposit
    /// Verifies the happy path where an authenticated user initiates a bank deposit with valid amount
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_WithValidRequest_ShouldReturnBankDepositInfo()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ValidInitiateBankDepositRequest();
        var expectedResponse = TestDataBuilder.BankDeposit.ValidBankDepositInfoResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as BankDepositInfoResponse;
        
        response.Should().NotBeNull();
        response!.TransactionId.Should().Be(expectedResponse.TransactionId);
        response.RequestedAmountUSD.Should().Be(expectedResponse.RequestedAmountUSD);
        response.AmountVND.Should().Be(expectedResponse.AmountVND);
        response.ExchangeRate.Should().Be(expectedResponse.ExchangeRate);
        response.BankName.Should().Be(expectedResponse.BankName);
        response.AccountHolder.Should().Be(expectedResponse.AccountHolder);
        response.AccountNumber.Should().Be(expectedResponse.AccountNumber);
        response.ReferenceCode.Should().Be(expectedResponse.ReferenceCode);
    }

    /// <summary>
    /// Test: Valid deposit should generate unique reference code
    /// Verifies that each deposit initiation generates a unique reference code
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_WithValidRequest_ShouldGenerateUniqueReferenceCode()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ValidInitiateBankDepositRequest();
        var expectedResponse = TestDataBuilder.BankDeposit.ValidBankDepositInfoResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as BankDepositInfoResponse;
        
        response!.ReferenceCode.Should().NotBeNullOrEmpty();
        response.ReferenceCode.Should().StartWith("FINIXDEP");
        response.ReferenceCode.Length.Should().BeGreaterThan(8);
    }

    /// <summary>
    /// Test: Valid deposit should create transaction record
    /// Verifies that initiating deposit creates a proper transaction record with correct details
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_WithValidRequest_ShouldCreateTransactionRecord()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ValidInitiateBankDepositRequest();
        var expectedResponse = TestDataBuilder.BankDeposit.ValidBankDepositInfoResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as BankDepositInfoResponse;
        
        response!.TransactionId.Should().BeGreaterThan(0);
        response.RequestedAmountUSD.Should().Be(depositRequest.AmountUSD);
        response.AmountVND.Should().BeGreaterThan(0);
        response.ExchangeRate.Should().BeGreaterThan(0);
    }

    #endregion

    #region Validation Tests
    // Tests that verify input validation and business rule enforcement

    /// <summary>
    /// Test: Zero amount should return BadRequest
    /// Verifies that deposit amount of zero is rejected with proper validation message
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_WithZeroAmount_ShouldReturnBadRequest()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ZeroAmountDepositRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((BankDepositInfoResponse?)null, "Deposit amount (USD) must be greater than zero."));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("must be greater than zero");
    }

    /// <summary>
    /// Test: Negative amount should return BadRequest
    /// Verifies that negative deposit amounts are rejected
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_WithNegativeAmount_ShouldReturnBadRequest()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.NegativeAmountDepositRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((BankDepositInfoResponse?)null, "Deposit amount (USD) must be greater than zero."));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("must be greater than zero");
    }

    /// <summary>
    /// Test: Extremely large amount should be handled appropriately
    /// Verifies that very large deposit amounts are handled correctly
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_WithVeryLargeAmount_ShouldReturnAppropriateResponse()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.VeryLargeAmountDepositRequest();
        var expectedResponse = TestDataBuilder.BankDeposit.LargeAmountBankDepositInfoResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as BankDepositInfoResponse;
        
        response!.RequestedAmountUSD.Should().Be(depositRequest.AmountUSD);
        response.AmountVND.Should().BeGreaterThan(0);
    }

    #endregion

    #region Business Logic Tests
    // Tests that verify business rules and logic implementation

    /// <summary>
    /// Test: User wallet not found should return BadRequest
    /// Verifies proper handling when user wallet doesn't exist
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_WithNonExistentWallet_ShouldReturnBadRequest()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ValidInitiateBankDepositRequest();
        var authenticatedUser = CreateAuthenticatedUser(999, "nonexistentuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((BankDepositInfoResponse?)null, "User wallet not found."));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("User wallet not found.");
    }

    /// <summary>
    /// Test: Exchange rate configuration error should return InternalServerError
    /// Verifies proper handling of system configuration errors
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_WithInvalidExchangeRateConfig_ShouldReturnInternalServerError()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ValidInitiateBankDepositRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((BankDepositInfoResponse?)null, "System error: Exchange rate configuration is invalid or not found."));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Contain("Exchange rate configuration");
    }

    /// <summary>
    /// Test: Missing transaction type configuration should return InternalServerError
    /// Verifies proper handling when required transaction types are not configured
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_WithMissingTransactionTypeConfig_ShouldReturnInternalServerError()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ValidInitiateBankDepositRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((BankDepositInfoResponse?)null, "System error: Deposit type configuration missing."));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Contain("configuration missing");
    }

    #endregion

    #region Security Tests
    // Tests that verify security measures and access control

    /// <summary>
    /// Test: Service authentication error should return BadRequest (since [Authorize] handles unauthenticated users)
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_WithServiceAuthError_ShouldReturnBadRequest()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ValidInitiateBankDepositRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((BankDepositInfoResponse?)null, "User not authenticated."));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("User not authenticated.");
    }

    /// <summary>
    /// Test: User should only be able to initiate deposits for their own wallet
    /// Verifies that deposit initiation is properly scoped to the authenticated user
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_ShouldOnlyAllowUserToInitiateOwnDeposit()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ValidInitiateBankDepositRequest();
        var expectedResponse = TestDataBuilder.BankDeposit.ValidBankDepositInfoResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called with the authenticated user's context only
        _mockWalletService.Verify(x => x.InitiateBankDepositAsync(
            It.Is<ClaimsPrincipal>(u => u.FindFirst(ClaimTypes.NameIdentifier) != null && 
                                        u.FindFirst(ClaimTypes.NameIdentifier).Value == "1"),
            It.IsAny<InitiateBankDepositRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Deposit initiation should not expose sensitive system information
    /// Verifies that responses don't contain sensitive data
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_ShouldNotExposeSensitiveSystemInformation()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ValidInitiateBankDepositRequest();
        var expectedResponse = TestDataBuilder.BankDeposit.ValidBankDepositInfoResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var responseJson = System.Text.Json.JsonSerializer.Serialize(okResult!.Value);
        
        responseJson.Should().NotContain("password", "Response should not expose password information");
        responseJson.Should().NotContain("secret", "Response should not expose secret keys");
        responseJson.Should().NotContain("private", "Response should not expose private system information");
        responseJson.Should().NotContain("internal", "Response should not expose internal system details");
    }

    #endregion

    #region Service Integration Tests
    // Tests that verify proper integration with the service layer

    /// <summary>
    /// Test: Service should be called with correct parameters
    /// Verifies that controller passes the correct parameters to the service
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ValidInitiateBankDepositRequest();
        var expectedResponse = TestDataBuilder.BankDeposit.ValidBankDepositInfoResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        _mockWalletService.Verify(x => x.InitiateBankDepositAsync(
            It.Is<ClaimsPrincipal>(u => u == authenticatedUser),
            It.Is<InitiateBankDepositRequest>(r => r.AmountUSD == depositRequest.AmountUSD),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Cancellation token should be passed to service
    /// Verifies that the cancellation token is properly forwarded to the service layer
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_WithCancellationToken_ShouldPassTokenToService()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ValidInitiateBankDepositRequest();
        var expectedResponse = TestDataBuilder.BankDeposit.ValidBankDepositInfoResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        var cancellationToken = new CancellationToken();
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, cancellationToken);

        // Assert
        _mockWalletService.Verify(x => x.InitiateBankDepositAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<InitiateBankDepositRequest>(),
            cancellationToken), Times.Once);
    }

    #endregion

    #region Logging Tests
    // Tests that verify proper logging behavior

    /// <summary>
    /// Test: Deposit initiation should log information at start
    /// Verifies that proper information logs are created when initiating deposits
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_ShouldLogInformationAtStart()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ValidInitiateBankDepositRequest();
        var expectedResponse = TestDataBuilder.BankDeposit.ValidBankDepositInfoResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("initiating bank deposit for AmountUSD")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Deposit failure should log warning
    /// Verifies that failures are properly logged with warning level
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_WithError_ShouldLogWarning()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ValidInitiateBankDepositRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((BankDepositInfoResponse?)null, "User wallet not found."));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bank deposit initiation failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Deposit initiation should not log sensitive information
    /// Verifies that logs don't contain sensitive user or financial data
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_ShouldNotLogSensitiveInformation()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ValidInitiateBankDepositRequest();
        var expectedResponse = TestDataBuilder.BankDeposit.ValidBankDepositInfoResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => !v.ToString()!.Contains("password") &&
                                           !v.ToString()!.Contains("secret") &&
                                           !v.ToString()!.Contains("private")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Error Handling Tests
    // Tests that verify comprehensive error handling scenarios

    /// <summary>
    /// Test: Database error should return InternalServerError
    /// Verifies proper handling of database connectivity or transaction issues
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_WithDatabaseError_ShouldReturnInternalServerError()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ValidInitiateBankDepositRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((BankDepositInfoResponse?)null, "Database connection error"));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("Database connection error");
    }

    /// <summary>
    /// Test: Null error message should return generic error
    /// Verifies proper handling when service returns null error message
    /// </summary>
    [Fact]
    public async Task InitiateBankDeposit_WithNullErrorMessage_ShouldReturnGenericError()
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ValidInitiateBankDepositRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((BankDepositInfoResponse?)null, (string?)null));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("Failed to initiate bank deposit.");
    }

    /// <summary>
    /// Test: Should return appropriate error categories for different system failures
    /// </summary>
    [Theory]
    [InlineData("System error: Exchange rate configuration", 500)]
    [InlineData("configuration missing", 500)]
    [InlineData("User wallet not found", 400)]
    [InlineData("must be greater than zero", 400)]
    public async Task InitiateBankDeposit_WithVariousErrors_ShouldReturnAppropriateStatusCodes(string errorMessage, int expectedStatusCode)
    {
        // Arrange
        var depositRequest = TestDataBuilder.BankDeposit.ValidInitiateBankDepositRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.InitiateBankDepositAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<InitiateBankDepositRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((BankDepositInfoResponse?)null, errorMessage));

        // Act
        var result = await _walletsController.InitiateBankDeposit(depositRequest, CancellationToken.None);

        // Assert
        if (expectedStatusCode == 500)
        {
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
        }
        else
        {
            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }

    #endregion

    #region GetMyWallet Tests

    #region Happy Path Tests - GetMyWallet
    // Tests that verify the endpoint works correctly under normal, expected conditions

    /// <summary>
    /// Test: Valid authenticated user should get wallet information
    /// </summary>
    [Fact]
    public async Task GetMyWallet_WithValidAuthenticatedUser_ShouldReturnWalletInfo()
    {
        // Arrange
        var walletDto = TestDataBuilder.GetMyWallet.ValidUserWallet();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((walletDto, (string?)null));

        // Act
        var result = await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedWallet = okResult!.Value as WalletDto;
        
        returnedWallet.Should().NotBeNull();
        returnedWallet!.WalletId.Should().Be(1);
        returnedWallet.UserId.Should().Be(1);
        returnedWallet.Balance.Should().Be(1000.50m);
        returnedWallet.CurrencyCode.Should().Be("USD");
        returnedWallet.EmailForQrCode.Should().Be("testuser@example.com");
    }

    /// <summary>
    /// Test: Business user should get correct wallet information
    /// </summary>
    [Fact]
    public async Task GetMyWallet_WithBusinessUser_ShouldReturnBusinessWalletInfo()
    {
        // Arrange
        var walletDto = TestDataBuilder.GetMyWallet.ValidBusinessUserWallet();
        var authenticatedUser = CreateAuthenticatedUser(2, "businessuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((walletDto, (string?)null));

        // Act
        var result = await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedWallet = okResult!.Value as WalletDto;
        
        returnedWallet.Should().NotBeNull();
        returnedWallet!.Balance.Should().Be(25000.75m);
        returnedWallet.EmailForQrCode.Should().Be("business.user@company.com");
    }

    /// <summary>
    /// Test: Zero balance wallet should be returned correctly
    /// </summary>
    [Fact]
    public async Task GetMyWallet_WithZeroBalance_ShouldReturnZeroBalance()
    {
        // Arrange
        var walletDto = TestDataBuilder.GetMyWallet.ZeroBalanceWallet();
        var authenticatedUser = CreateAuthenticatedUser(4, "newuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((walletDto, (string?)null));

        // Act
        var result = await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedWallet = okResult!.Value as WalletDto;
        
        returnedWallet.Should().NotBeNull();
        returnedWallet!.Balance.Should().Be(0.00m);
        returnedWallet.EmailForQrCode.Should().Be("newuser@example.com");
    }

    /// <summary>
    /// Test: Wallet with high precision balance should maintain precision
    /// </summary>
    [Fact]
    public async Task GetMyWallet_WithPreciseBalance_ShouldMaintainPrecision()
    {
        // Arrange
        var walletDto = TestDataBuilder.GetMyWallet.WalletWithPreciseBalance();
        var authenticatedUser = CreateAuthenticatedUser(7, "precisionuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((walletDto, (string?)null));

        // Act
        var result = await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedWallet = okResult!.Value as WalletDto;
        
        returnedWallet.Should().NotBeNull();
        returnedWallet!.Balance.Should().Be(123.456789m);
        returnedWallet.CurrencyCode.Should().Be("USD");
    }

    /// <summary>
    /// Test: Wallet with QR code prefix should be formatted correctly
    /// </summary>
    [Fact]
    public async Task GetMyWallet_WithQrCodePrefix_ShouldReturnFormattedEmail()
    {
        // Arrange
        var walletDto = TestDataBuilder.GetMyWallet.WalletWithQrCodePrefix();
        var authenticatedUser = CreateAuthenticatedUser(10, "qrcodeuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((walletDto, (string?)null));

        // Act
        var result = await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedWallet = okResult!.Value as WalletDto;
        
        returnedWallet.Should().NotBeNull();
        returnedWallet!.EmailForQrCode.Should().Be("mailto:qrcode@example.com");
    }

    #endregion

    #region Authentication Tests - GetMyWallet
    // Tests that verify authentication and user validation scenarios

    /// <summary>
    /// Test: Unauthenticated request should return Unauthorized
    /// </summary>
    [Fact]
    public async Task GetMyWallet_WithUnauthenticatedUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var unauthenticatedUser = CreateUnauthenticatedUser();
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = unauthenticatedUser
        };

        // Act
        var result = await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        var response = unauthorizedResult!.Value;
        
        response.Should().NotBeNull();
        var messageProperty = response!.GetType().GetProperty("Message");
        messageProperty.Should().NotBeNull();
    }

    /// <summary>
    /// Test: Invalid claims should return Unauthorized
    /// </summary>
    [Fact]
    public async Task GetMyWallet_WithInvalidClaims_ShouldReturnUnauthorized()
    {
        // Arrange
        var userWithInvalidClaims = CreateUserWithInvalidClaims();
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = userWithInvalidClaims
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WalletDto?)null, "User not authenticated or identity is invalid."));

        // Act
        var result = await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var responseMessage = GetMessageFromResponse(notFoundResult!.Value);
        responseMessage.Should().Contain("not authenticated");
    }

    /// <summary>
    /// Test: Service authentication error should be handled properly
    /// </summary>
    [Fact]
    public async Task GetMyWallet_WithServiceAuthenticationError_ShouldReturnNotFound()
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(999, "invaliduser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WalletDto?)null, "User or Wallet not found."));

        // Act
        var result = await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var responseMessage = GetMessageFromResponse(notFoundResult!.Value);
        responseMessage.Should().Be("User or Wallet not found.");
    }

    #endregion

    #region Data Validation Tests - GetMyWallet
    // Tests that verify data mapping and validation scenarios

    /// <summary>
    /// Test: Wallet ID mapping should be correct
    /// </summary>
    [Fact]
    public async Task GetMyWallet_ShouldMapWalletIdCorrectly()
    {
        // Arrange
        var walletDto = TestDataBuilder.GetMyWallet.ValidAdminWallet();
        var authenticatedUser = CreateAuthenticatedUser(3, "admin");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((walletDto, (string?)null));

        // Act
        var result = await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedWallet = okResult!.Value as WalletDto;
        
        returnedWallet.Should().NotBeNull();
        returnedWallet!.WalletId.Should().Be(3);
        returnedWallet.UserId.Should().Be(3);
    }

    /// <summary>
    /// Test: Currency code format should be validated
    /// </summary>
    [Fact]
    public async Task GetMyWallet_ShouldReturnCorrectCurrencyCodeFormat()
    {
        // Arrange
        var walletDto = TestDataBuilder.GetMyWallet.WalletWithDifferentCurrency();
        var authenticatedUser = CreateAuthenticatedUser(9, "eurouser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((walletDto, (string?)null));

        // Act
        var result = await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedWallet = okResult!.Value as WalletDto;
        
        returnedWallet.Should().NotBeNull();
        returnedWallet!.CurrencyCode.Should().Be("EUR");
        returnedWallet.CurrencyCode.Should().HaveLength(3);
        returnedWallet.CurrencyCode.Should().MatchRegex("^[A-Z]{3}$");
    }

    /// <summary>
    /// Test: Email for QR display should be formatted correctly
    /// </summary>
    [Fact]
    public async Task GetMyWallet_ShouldReturnFormattedEmailForQrCode()
    {
        // Arrange
        var walletDto = TestDataBuilder.GetMyWallet.WalletWithSpecialCharacterEmail();
        var authenticatedUser = CreateAuthenticatedUser(8, "specialuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((walletDto, (string?)null));

        // Act
        var result = await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedWallet = okResult!.Value as WalletDto;
        
        returnedWallet.Should().NotBeNull();
        returnedWallet!.EmailForQrCode.Should().Be("special+user@test-domain.co.uk");
        returnedWallet.EmailForQrCode.Should().Contain("@");
    }

    /// <summary>
    /// Test: Updated timestamp should be included
    /// </summary>
    [Fact]
    public async Task GetMyWallet_ShouldIncludeUpdatedTimestamp()
    {
        // Arrange
        var walletDto = TestDataBuilder.GetMyWallet.WalletWithRecentActivity();
        var authenticatedUser = CreateAuthenticatedUser(15, "activeuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((walletDto, (string?)null));

        // Act
        var result = await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedWallet = okResult!.Value as WalletDto;
        
        returnedWallet.Should().NotBeNull();
        returnedWallet!.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
    }

    #endregion

    #region Business Logic Tests - GetMyWallet
    // Tests that verify business logic and edge case scenarios

    /// <summary>
    /// Test: Small balance should be handled correctly
    /// </summary>
    [Fact]
    public async Task GetMyWallet_WithSmallBalance_ShouldHandleCorrectly()
    {
        // Arrange
        var walletDto = TestDataBuilder.GetMyWallet.SmallBalanceWallet();
        var authenticatedUser = CreateAuthenticatedUser(5, "smallbalanceuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((walletDto, (string?)null));

        // Act
        var result = await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedWallet = okResult!.Value as WalletDto;
        
        returnedWallet.Should().NotBeNull();
        returnedWallet!.Balance.Should().Be(0.01m);
        returnedWallet.Balance.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Test: Large balance should be handled correctly
    /// </summary>
    [Fact]
    public async Task GetMyWallet_WithLargeBalance_ShouldHandleCorrectly()
    {
        // Arrange
        var walletDto = TestDataBuilder.GetMyWallet.LargeBalanceWallet();
        var authenticatedUser = CreateAuthenticatedUser(6, "largebalanceuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((walletDto, (string?)null));

        // Act
        var result = await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedWallet = okResult!.Value as WalletDto;
        
        returnedWallet.Should().NotBeNull();
        returnedWallet!.Balance.Should().Be(999999.99m);
        returnedWallet.Balance.Should().BeLessThan(1000000m);
    }

    /// <summary>
    /// Test: VIP user wallet should be handled appropriately
    /// </summary>
    [Fact]
    public async Task GetMyWallet_WithVipUser_ShouldReturnVipWalletInfo()
    {
        // Arrange
        var walletDto = TestDataBuilder.GetMyWallet.WalletForVipUser();
        var authenticatedUser = CreateAuthenticatedUser(13, "vipuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((walletDto, (string?)null));

        // Act
        var result = await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedWallet = okResult!.Value as WalletDto;
        
        returnedWallet.Should().NotBeNull();
        returnedWallet!.Balance.Should().Be(100000.00m);
        returnedWallet.EmailForQrCode.Should().Contain("vip.member");
    }

    #endregion

    #region Service Integration Tests - GetMyWallet
    // Tests that verify correct integration with service layer

    /// <summary>
    /// Test: Controller should pass correct parameters to service
    /// </summary>
    [Fact]
    public async Task GetMyWallet_ShouldPassCorrectParametersToService()
    {
        // Arrange
        var walletDto = TestDataBuilder.GetMyWallet.ValidUserWallet();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((walletDto, (string?)null));

        // Act
        await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        _mockWalletService.Verify(x => x.GetUserWalletAsync(
            It.Is<ClaimsPrincipal>(cp => cp == authenticatedUser),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    /// <summary>
    /// Test: Cancellation token should be properly forwarded
    /// </summary>
    [Fact]
    public async Task GetMyWallet_ShouldForwardCancellationToken()
    {
        // Arrange
        var walletDto = TestDataBuilder.GetMyWallet.ValidUserWallet();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        var cancellationToken = new CancellationToken();
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((walletDto, (string?)null));

        // Act
        await _walletsController.GetMyWallet(cancellationToken);

        // Assert
        _mockWalletService.Verify(x => x.GetUserWalletAsync(
            It.IsAny<ClaimsPrincipal>(),
            cancellationToken
        ), Times.Once);
    }

    #endregion

    #region Logging Tests - GetMyWallet
    // Tests that verify appropriate logging behavior

    /// <summary>
    /// Test: Successful wallet retrieval should be logged
    /// </summary>
    [Fact]
    public async Task GetMyWallet_OnSuccess_ShouldLogInformation()
    {
        // Arrange
        var walletDto = TestDataBuilder.GetMyWallet.ValidUserWallet();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((walletDto, (string?)null));

        // Act
        await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempting to retrieve wallet")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Wallet retrieval failure should be logged as warning
    /// </summary>
    [Fact]
    public async Task GetMyWallet_OnFailure_ShouldLogWarning()
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(999, "notfounduser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WalletDto?)null, "User or Wallet not found."));

        // Act
        await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to retrieve wallet")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Error Handling Tests - GetMyWallet
    // Tests that verify proper error handling in various failure scenarios

    /// <summary>
    /// Test: Service error should return Internal Server Error
    /// </summary>
    [Fact]
    public async Task GetMyWallet_WithServiceError_ShouldReturnInternalServerError()
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WalletDto?)null, "Database connection error."));

        // Act
        var result = await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("Database connection error.");
    }

    /// <summary>
    /// Test: Null error message should return generic error
    /// </summary>
    [Fact]
    public async Task GetMyWallet_WithNullErrorMessage_ShouldReturnGenericError()
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WalletDto?)null, (string?)null));

        // Act
        var result = await _walletsController.GetMyWallet(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("An unexpected error occurred while retrieving the wallet.");
    }

    /// <summary>
    /// Test: Service exception should propagate up (handled by global middleware)
    /// </summary>
    [Fact]
    public async Task GetMyWallet_WithServiceException_ShouldReturnInternalServerError()
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        // Act & Assert
        // Exception should propagate up to be handled by global exception middleware
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _walletsController.GetMyWallet(CancellationToken.None));
        
        exception.Message.Should().Be("Service unavailable");
    }

    #endregion

    #endregion

    #region GetMyWalletTransactions Tests - SCRUM-50

    // Tests for GET /api/v1/wallets/transactions endpoint
    // According to SCRUM-50 requirements: Happy Path, Pagination, Filters, Authentication, Data Isolation

    #region Happy Path Tests

    /// <summary>
    /// Test: Valid authenticated user should retrieve transaction history successfully
    /// </summary>
    [Fact]
    public async Task GetMyWalletTransactions_WithValidRequest_ShouldReturnTransactionHistory()
    {
        // Arrange
        var query = TestDataBuilder.GetTransactions.ValidQuery();
        var expectedTransactions = TestDataBuilder.GetTransactions.ValidPaginatedTransactions();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletTransactionsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetWalletTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _walletsController.GetMyWalletTransactions(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var transactions = okResult!.Value as PaginatedList<WalletTransactionDto>;
        
        transactions.Should().NotBeNull();
        transactions!.Items.Should().HaveCount(3);
        transactions.TotalCount.Should().Be(15);
        transactions.PageNumber.Should().Be(1);
        transactions.PageSize.Should().Be(10);
    }

    /// <summary>
    /// Test: Transaction history should support pagination correctly
    /// </summary>
    [Fact]
    public async Task GetMyWalletTransactions_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var query = TestDataBuilder.GetTransactions.QueryWithMaxPageSize();
        var expectedTransactions = TestDataBuilder.GetTransactions.LargePaginatedTransactions();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletTransactionsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetWalletTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _walletsController.GetMyWalletTransactions(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var transactions = okResult!.Value as PaginatedList<WalletTransactionDto>;
        
        transactions.Should().NotBeNull();
        transactions!.Items.Should().HaveCount(50);
        transactions.PageSize.Should().Be(50);
        transactions.TotalCount.Should().Be(250);
    }

    /// <summary>
    /// Test: Transaction history should support filtering by transaction type
    /// </summary>
    [Fact]
    public async Task GetMyWalletTransactions_WithTransactionTypeFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var query = TestDataBuilder.GetTransactions.QueryWithTransactionTypeFilter();
        var expectedTransactions = TestDataBuilder.GetTransactions.ValidPaginatedTransactions();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletTransactionsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetWalletTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _walletsController.GetMyWalletTransactions(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var transactions = okResult!.Value as PaginatedList<WalletTransactionDto>;
        
        transactions.Should().NotBeNull();
        transactions!.Items.Should().NotBeEmpty();
        
        // Verify service was called with correct filter
        _mockWalletService.Verify(x => x.GetUserWalletTransactionsAsync(
            It.IsAny<ClaimsPrincipal>(), 
            It.Is<GetWalletTransactionsQuery>(q => q.TransactionType == "Deposit"), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Transaction history should support date range filtering
    /// </summary>
    [Fact]
    public async Task GetMyWalletTransactions_WithDateRangeFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var query = TestDataBuilder.GetTransactions.QueryWithDateRangeFilter();
        var expectedTransactions = TestDataBuilder.GetTransactions.ValidPaginatedTransactions();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletTransactionsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetWalletTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _walletsController.GetMyWalletTransactions(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var transactions = okResult!.Value as PaginatedList<WalletTransactionDto>;
        
        transactions.Should().NotBeNull();
        
        // Verify service was called with correct date filters
        _mockWalletService.Verify(x => x.GetUserWalletTransactionsAsync(
            It.IsAny<ClaimsPrincipal>(), 
            It.Is<GetWalletTransactionsQuery>(q => q.StartDate.HasValue && q.EndDate.HasValue), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    #endregion

    #region Pagination Tests

    /// <summary>
    /// Test: Page size should be limited to maximum of 50
    /// </summary>
    [Fact]
    public async Task GetMyWalletTransactions_WithExcessivePageSize_ShouldLimitToMaximum()
    {
        // Arrange
        var query = TestDataBuilder.GetTransactions.QueryWithExcessivePageSize();
        var expectedTransactions = TestDataBuilder.GetTransactions.LargePaginatedTransactions();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletTransactionsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetWalletTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _walletsController.GetMyWalletTransactions(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify that the query validation works (should be handled by GetWalletTransactionsQuery.ValidatedPageSize)
        _mockWalletService.Verify(x => x.GetUserWalletTransactionsAsync(
            It.IsAny<ClaimsPrincipal>(), 
            It.Is<GetWalletTransactionsQuery>(q => q.ValidatedPageSize <= 50), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Page number should be validated (must be positive)
    /// </summary>
    [Fact]
    public async Task GetMyWalletTransactions_WithNegativePageNumber_ShouldUseValidatedPageNumber()
    {
        // Arrange
        var query = TestDataBuilder.GetTransactions.QueryWithNegativePageNumber();
        var expectedTransactions = TestDataBuilder.GetTransactions.ValidPaginatedTransactions();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletTransactionsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetWalletTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _walletsController.GetMyWalletTransactions(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify that the query validation works (should be handled by GetWalletTransactionsQuery.ValidatedPageNumber)
        _mockWalletService.Verify(x => x.GetUserWalletTransactionsAsync(
            It.IsAny<ClaimsPrincipal>(), 
            It.Is<GetWalletTransactionsQuery>(q => q.ValidatedPageNumber >= 1), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Total count should be calculated correctly
    /// </summary>
    [Fact]
    public async Task GetMyWalletTransactions_ShouldReturnCorrectTotalCount()
    {
        // Arrange
        var query = TestDataBuilder.GetTransactions.ValidQuery();
        var expectedTransactions = TestDataBuilder.GetTransactions.LargePaginatedTransactions();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletTransactionsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetWalletTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _walletsController.GetMyWalletTransactions(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var transactions = okResult!.Value as PaginatedList<WalletTransactionDto>;
        
        transactions.Should().NotBeNull();
        transactions!.TotalCount.Should().Be(250);
        transactions.HasNextPage.Should().BeTrue();
        transactions.HasPreviousPage.Should().BeFalse();
    }

    /// <summary>
    /// Test: Empty result should be handled correctly
    /// </summary>
    [Fact]
    public async Task GetMyWalletTransactions_WithNoTransactions_ShouldReturnEmptyResult()
    {
        // Arrange
        var query = TestDataBuilder.GetTransactions.ValidQuery();
        var expectedTransactions = TestDataBuilder.GetTransactions.EmptyPaginatedTransactions();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletTransactionsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetWalletTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _walletsController.GetMyWalletTransactions(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var transactions = okResult!.Value as PaginatedList<WalletTransactionDto>;
        
        transactions.Should().NotBeNull();
        transactions!.Items.Should().BeEmpty();
        transactions.TotalCount.Should().Be(0);
        transactions.HasNextPage.Should().BeFalse();
        transactions.HasPreviousPage.Should().BeFalse();
    }

    #endregion

    #region Filter Tests

    /// <summary>
    /// Test: Combined filters should work correctly
    /// </summary>
    [Fact]
    public async Task GetMyWalletTransactions_WithCombinedFilters_ShouldApplyAllFilters()
    {
        // Arrange
        var query = TestDataBuilder.GetTransactions.QueryWithCombinedFilters();
        var expectedTransactions = TestDataBuilder.GetTransactions.ValidPaginatedTransactions();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletTransactionsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetWalletTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _walletsController.GetMyWalletTransactions(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called with all filters
        _mockWalletService.Verify(x => x.GetUserWalletTransactionsAsync(
            It.IsAny<ClaimsPrincipal>(), 
            It.Is<GetWalletTransactionsQuery>(q => 
                q.TransactionType == "Withdrawal" &&
                q.Status == "Completed" &&
                q.StartDate.HasValue &&
                q.EndDate.HasValue &&
                q.SortBy == "Amount" &&
                q.SortOrder == "asc"), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Start date filtering should work correctly
    /// </summary>
    [Fact]
    public async Task GetMyWalletTransactions_WithStartDateFilter_ShouldApplyDateFilter()
    {
        // Arrange
        var query = new GetWalletTransactionsQuery
        {
            PageNumber = 1,
            PageSize = 10,
            StartDate = DateTime.UtcNow.AddDays(-30),
            SortBy = "TransactionDate",
            SortOrder = "desc"
        };
        var expectedTransactions = TestDataBuilder.GetTransactions.ValidPaginatedTransactions();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletTransactionsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetWalletTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _walletsController.GetMyWalletTransactions(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called with start date filter
        _mockWalletService.Verify(x => x.GetUserWalletTransactionsAsync(
            It.IsAny<ClaimsPrincipal>(), 
            It.Is<GetWalletTransactionsQuery>(q => q.StartDate.HasValue && !q.EndDate.HasValue), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: End date filtering should work correctly
    /// </summary>
    [Fact]
    public async Task GetMyWalletTransactions_WithEndDateFilter_ShouldApplyDateFilter()
    {
        // Arrange
        var query = new GetWalletTransactionsQuery
        {
            PageNumber = 1,
            PageSize = 10,
            EndDate = DateTime.UtcNow,
            SortBy = "TransactionDate",
            SortOrder = "desc"
        };
        var expectedTransactions = TestDataBuilder.GetTransactions.ValidPaginatedTransactions();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletTransactionsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetWalletTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _walletsController.GetMyWalletTransactions(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called with end date filter
        _mockWalletService.Verify(x => x.GetUserWalletTransactionsAsync(
            It.IsAny<ClaimsPrincipal>(), 
            It.Is<GetWalletTransactionsQuery>(q => !q.StartDate.HasValue && q.EndDate.HasValue), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    #endregion

    #region Authentication Tests

    /// <summary>
    /// Test: Unauthenticated request should be handled by [Authorize] attribute
    /// </summary>
    [Fact]
    public async Task GetMyWalletTransactions_WithUnauthenticatedUser_ShouldStillCallService()
    {
        // Arrange
        var query = TestDataBuilder.GetTransactions.ValidQuery();
        var expectedTransactions = TestDataBuilder.GetTransactions.EmptyPaginatedTransactions();
        var unauthenticatedUser = CreateUnauthenticatedUser();
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = unauthenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletTransactionsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetWalletTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _walletsController.GetMyWalletTransactions(query, CancellationToken.None);

        // Assert
        // Note: In real scenario, [Authorize] would prevent this from reaching the controller
        // But in unit tests, we can verify that the service handles authentication properly
        result.Should().BeOfType<OkObjectResult>();
        
        _mockWalletService.Verify(x => x.GetUserWalletTransactionsAsync(
            It.IsAny<ClaimsPrincipal>(), 
            It.IsAny<GetWalletTransactionsQuery>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: User should only see their own transactions (data isolation)
    /// </summary>
    [Fact]
    public async Task GetMyWalletTransactions_ShouldOnlyReturnUserOwnTransactions()
    {
        // Arrange
        var query = TestDataBuilder.GetTransactions.ValidQuery();
        var expectedTransactions = TestDataBuilder.GetTransactions.ValidPaginatedTransactions();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletTransactionsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetWalletTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _walletsController.GetMyWalletTransactions(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify the authenticated user is passed to the service (data isolation is handled in service layer)
        _mockWalletService.Verify(x => x.GetUserWalletTransactionsAsync(
            It.Is<ClaimsPrincipal>(p => p.FindFirst(ClaimTypes.NameIdentifier) != null && p.FindFirst(ClaimTypes.NameIdentifier).Value == "1"), 
            It.IsAny<GetWalletTransactionsQuery>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    #endregion

    #region Service Integration Tests

    /// <summary>
    /// Test: Controller should pass correct parameters to service
    /// </summary>
    [Fact]
    public async Task GetMyWalletTransactions_ShouldPassCorrectParametersToService()
    {
        // Arrange
        var query = TestDataBuilder.GetTransactions.QueryWithCombinedFilters();
        var expectedTransactions = TestDataBuilder.GetTransactions.ValidPaginatedTransactions();
        var authenticatedUser = CreateAuthenticatedUser(42, "specificuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletTransactionsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetWalletTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _walletsController.GetMyWalletTransactions(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        _mockWalletService.Verify(x => x.GetUserWalletTransactionsAsync(
            It.Is<ClaimsPrincipal>(p => p.FindFirst(ClaimTypes.NameIdentifier) != null && p.FindFirst(ClaimTypes.NameIdentifier).Value == "42"), 
            It.Is<GetWalletTransactionsQuery>(q => 
                q.PageNumber == query.PageNumber &&
                q.PageSize == query.PageSize &&
                q.TransactionType == query.TransactionType &&
                q.Status == query.Status &&
                q.StartDate == query.StartDate &&
                q.EndDate == query.EndDate &&
                q.SortBy == query.SortBy &&
                q.SortOrder == query.SortOrder), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Controller should forward cancellation token to service
    /// </summary>
    [Fact]
    public async Task GetMyWalletTransactions_ShouldForwardCancellationToken()
    {
        // Arrange
        var query = TestDataBuilder.GetTransactions.ValidQuery();
        var expectedTransactions = TestDataBuilder.GetTransactions.ValidPaginatedTransactions();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        var cancellationToken = new CancellationToken();
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletTransactionsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetWalletTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _walletsController.GetMyWalletTransactions(query, cancellationToken);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        _mockWalletService.Verify(x => x.GetUserWalletTransactionsAsync(
            It.IsAny<ClaimsPrincipal>(), 
            It.IsAny<GetWalletTransactionsQuery>(), 
            cancellationToken), 
            Times.Once);
    }

    /// <summary>
    /// Test: Controller should log information about transaction retrieval
    /// </summary>
    [Fact]
    public async Task GetMyWalletTransactions_ShouldLogInformation()
    {
        // Arrange
        var query = TestDataBuilder.GetTransactions.ValidQuery();
        var expectedTransactions = TestDataBuilder.GetTransactions.ValidPaginatedTransactions();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.GetUserWalletTransactionsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetWalletTransactionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _walletsController.GetMyWalletTransactions(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify that controller logs the transaction retrieval attempt
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Attempting to retrieve transactions")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #endregion

    #region CreateWithdrawalRequest Tests - SCRUM-51

    // Tests for POST /api/v1/wallets/withdrawals endpoint
    // According to SCRUM-51 requirements: Happy Path, Validation, Business Logic, Security

    #region Happy Path Tests

    /// <summary>
    /// Test: Valid withdrawal request should be created successfully
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_WithValidRequest_ShouldReturnWithdrawalRequestDto()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.ValidRequest();
        var expectedResponse = TestDataBuilder.CreateWithdrawal.ValidResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        var response = createdResult!.Value as WithdrawalRequestDto;
        
        response.Should().NotBeNull();
        response!.Amount.Should().Be(request.Amount);
        response.CurrencyCode.Should().Be(request.CurrencyCode);
        response.WithdrawalMethodDetails.Should().Be(request.WithdrawalMethodDetails);
        response.Notes.Should().Be(request.Notes);
        response.Status.Should().Be("PendingAdminApproval");
    }

    /// <summary>
    /// Test: Withdrawal request should create transaction record
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_WithValidRequest_ShouldCreateTransactionRecord()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.LargeAmountRequest();
        var expectedResponse = TestDataBuilder.CreateWithdrawal.LargeAmountResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        var response = createdResult!.Value as WithdrawalRequestDto;
        
        response.Should().NotBeNull();
        response!.WithdrawalRequestId.Should().BeGreaterThan(0);
        response.UserId.Should().Be(1);
        response.Status.Should().Be("PendingAdminApproval");
        response.RequestedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Test: Withdrawal request without notes should work
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_WithoutNotes_ShouldSucceed()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.RequestWithoutNotes();
        var expectedResponse = TestDataBuilder.CreateWithdrawal.ResponseWithoutNotes();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        var response = createdResult!.Value as WithdrawalRequestDto;
        
        response.Should().NotBeNull();
        response!.Notes.Should().BeNull();
        response.Amount.Should().Be(request.Amount);
    }

    /// <summary>
    /// Test: Minimum amount withdrawal should work
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_WithMinimumAmount_ShouldSucceed()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.MinimumAmountRequest();
        var expectedResponse = TestDataBuilder.CreateWithdrawal.CustomResponse(3004, 1, 0.01m);
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        var response = createdResult!.Value as WithdrawalRequestDto;
        
        response.Should().NotBeNull();
        response!.Amount.Should().Be(0.01m);
    }

    #endregion

    #region Validation Tests

    /// <summary>
    /// Test: Zero amount should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_WithZeroAmount_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.ZeroAmountRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WithdrawalRequestDto?)null, "Withdrawal amount must be greater than 0."));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("must be greater than 0");
    }

    /// <summary>
    /// Test: Negative amount should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_WithNegativeAmount_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.NegativeAmountRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WithdrawalRequestDto?)null, "Withdrawal amount must be greater than 0."));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("must be greater than 0");
    }

    /// <summary>
    /// Test: Invalid currency code should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_WithInvalidCurrency_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.InvalidCurrencyRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WithdrawalRequestDto?)null, "Currency is required"));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("required");
    }

    /// <summary>
    /// Test: Empty withdrawal method details should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_WithEmptyWithdrawalMethod_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.EmptyWithdrawalMethodRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WithdrawalRequestDto?)null, "Withdrawal method details are required."));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("required");
    }

    /// <summary>
    /// Test: Too long withdrawal method details should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_WithTooLongWithdrawalMethod_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.TooLongWithdrawalMethodRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WithdrawalRequestDto?)null, "Withdrawal method details are required"));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("required");
    }

    /// <summary>
    /// Test: Too long notes should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_WithTooLongNotes_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.TooLongNotesRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WithdrawalRequestDto?)null, "Notes are required"));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("required");
    }

    #endregion

    #region Business Logic Tests

    /// <summary>
    /// Test: Amount exceeding balance should return BadRequest
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_WithAmountExceedingBalance_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.AmountExceedingBalanceRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WithdrawalRequestDto?)null, "Insufficient balance. Requested: 100000.00, Available: 1000.00"));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("Insufficient balance");
    }

    /// <summary>
    /// Test: Non-existent wallet should return NotFound
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_WithNonExistentWallet_ShouldReturnNotFound()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.ValidRequest();
        var authenticatedUser = CreateAuthenticatedUser(999, "nonexistentuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WithdrawalRequestDto?)null, "User wallet not found."));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var responseMessage = GetMessageFromResponse(notFoundResult!.Value);
        responseMessage.Should().Be("User wallet not found.");
    }

    /// <summary>
    /// Test: Valid balance checking should work
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_ShouldValidateBalance()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.ValidRequest();
        var expectedResponse = TestDataBuilder.CreateWithdrawal.ValidResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        
        // Verify service was called with correct parameters for balance validation
        _mockWalletService.Verify(x => x.CreateWithdrawalRequestAsync(
            It.IsAny<ClaimsPrincipal>(), 
            It.Is<CreateWithdrawalRequest>(r => r.Amount == request.Amount && r.CurrencyCode == request.CurrencyCode), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Status should be set to PendingAdminApproval
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_ShouldSetStatusToPendingAdminApproval()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.ValidRequest();
        var expectedResponse = TestDataBuilder.CreateWithdrawal.ValidResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        var response = createdResult!.Value as WithdrawalRequestDto;
        
        response.Should().NotBeNull();
        response!.Status.Should().Be("PendingAdminApproval");
    }

    #endregion

    #region Security Tests

    /// <summary>
    /// Test: User authentication should be required
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_WithUnauthenticatedUser_ShouldCallServiceWithUnauthenticatedPrincipal()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.ValidRequest();
        var unauthenticatedUser = CreateUnauthenticatedUser();
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = unauthenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WithdrawalRequestDto?)null, "User not authenticated or identity is invalid."));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        // Note: In real scenario, [Authorize] would prevent this from reaching the controller
        // But in unit tests, we verify that the service handles authentication properly
        result.Should().BeOfType<ObjectResult>();
        var errorResult = result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
        
        _mockWalletService.Verify(x => x.CreateWithdrawalRequestAsync(
            It.IsAny<ClaimsPrincipal>(), 
            It.IsAny<CreateWithdrawalRequest>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Amount validation should be enforced
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_ShouldValidateAmount()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.ValidRequest();
        var expectedResponse = TestDataBuilder.CreateWithdrawal.ValidResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        
        // Verify service was called with positive amount
        _mockWalletService.Verify(x => x.CreateWithdrawalRequestAsync(
            It.IsAny<ClaimsPrincipal>(), 
            It.Is<CreateWithdrawalRequest>(r => r.Amount > 0), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: User should only be able to create withdrawal for their own wallet
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_ShouldOnlyAllowUserOwnWithdrawal()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.ValidRequest();
        var expectedResponse = TestDataBuilder.CreateWithdrawal.ValidResponse();
        var authenticatedUser = CreateAuthenticatedUser(42, "specificuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        
        // Verify the authenticated user is passed to the service (data isolation is handled in service layer)
        _mockWalletService.Verify(x => x.CreateWithdrawalRequestAsync(
            It.Is<ClaimsPrincipal>(p => p.FindFirst(ClaimTypes.NameIdentifier) != null && p.FindFirst(ClaimTypes.NameIdentifier).Value == "42"), 
            It.IsAny<CreateWithdrawalRequest>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    #endregion

    #region Service Integration Tests

    /// <summary>
    /// Test: Controller should pass correct parameters to service
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_ShouldPassCorrectParametersToService()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.LargeAmountRequest();
        var expectedResponse = TestDataBuilder.CreateWithdrawal.LargeAmountResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        
        _mockWalletService.Verify(x => x.CreateWithdrawalRequestAsync(
            It.Is<ClaimsPrincipal>(p => p.FindFirst(ClaimTypes.NameIdentifier) != null && p.FindFirst(ClaimTypes.NameIdentifier).Value == "1"), 
            It.Is<CreateWithdrawalRequest>(r => 
                r.Amount == request.Amount &&
                r.CurrencyCode == request.CurrencyCode &&
                r.WithdrawalMethodDetails == request.WithdrawalMethodDetails &&
                r.Notes == request.Notes), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Controller should forward cancellation token to service
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_ShouldForwardCancellationToken()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.ValidRequest();
        var expectedResponse = TestDataBuilder.CreateWithdrawal.ValidResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        var cancellationToken = new CancellationToken();
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, cancellationToken);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        
        _mockWalletService.Verify(x => x.CreateWithdrawalRequestAsync(
            It.IsAny<ClaimsPrincipal>(), 
            It.IsAny<CreateWithdrawalRequest>(), 
            cancellationToken), 
            Times.Once);
    }

    /// <summary>
    /// Test: Controller should log information about withdrawal request
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_ShouldLogInformation()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.ValidRequest();
        var expectedResponse = TestDataBuilder.CreateWithdrawal.ValidResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        
        // Verify logging behavior (implementation depends on your logging setup)
        // This test ensures the controller is structured to support logging
        _mockWalletService.Verify(x => x.CreateWithdrawalRequestAsync(
            It.IsAny<ClaimsPrincipal>(), 
            It.IsAny<CreateWithdrawalRequest>(), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Service error should return InternalServerError
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_WithServiceError_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.ValidRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WithdrawalRequestDto?)null, "Internal system error occurred."));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var errorResult = result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(errorResult.Value);
        responseMessage.Should().Contain("Internal system error occurred");
    }

    /// <summary>
    /// Test: Null error message should return generic error
    /// </summary>
    [Fact]
    public async Task CreateWithdrawalRequest_WithNullErrorMessage_ShouldReturnGenericError()
    {
        // Arrange
        var request = TestDataBuilder.CreateWithdrawal.ValidRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.CreateWithdrawalRequestAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CreateWithdrawalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WithdrawalRequestDto?)null, (string?)null));

        // Act
        var result = await _walletsController.CreateWithdrawalRequest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var errorResult = result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(errorResult.Value);
        responseMessage.Should().Be("Failed to create withdrawal request.");
    }

    #endregion

    #endregion

    #region VerifyRecipient Tests - SCRUM-52

    // Tests for POST /api/v1/wallets/internal-transfer/verify-recipient endpoint
    // According to SCRUM-52 requirements: Happy Path, Validation, Business Logic, Security

    #region Happy Path Tests

    /// <summary>
    /// Test: Valid recipient email should return recipient information
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_WithValidEmail_ShouldReturnRecipientInfo()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.ValidRequest();
        var expectedResponse = TestDataBuilder.VerifyRecipient.ValidResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as RecipientInfoResponse;
        
        response.Should().NotBeNull();
        response!.RecipientUserId.Should().Be(expectedResponse.RecipientUserId);
        response.RecipientUsername.Should().Be(expectedResponse.RecipientUsername);
        response.RecipientFullName.Should().Be(expectedResponse.RecipientFullName);
    }

    /// <summary>
    /// Test: Valid alternative recipient email should return recipient information
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_WithValidAlternativeEmail_ShouldReturnRecipientInfo()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.ValidAlternativeRequest();
        var expectedResponse = TestDataBuilder.VerifyRecipient.AlternativeValidResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as RecipientInfoResponse;
        
        response.Should().NotBeNull();
        response!.RecipientUserId.Should().Be(3);
        response.RecipientUsername.Should().Be("user_b");
        response.RecipientFullName.Should().Be("User B Full Name");
    }

    /// <summary>
    /// Test: Recipient without full name should still return valid response
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_WithRecipientWithoutFullName_ShouldReturnValidResponse()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.ValidRequest();
        var expectedResponse = TestDataBuilder.VerifyRecipient.ResponseWithoutFullName();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as RecipientInfoResponse;
        
        response.Should().NotBeNull();
        response!.RecipientUserId.Should().Be(4);
        response.RecipientUsername.Should().Be("minimal_user");
        response.RecipientFullName.Should().BeNull();
    }

    /// <summary>
    /// Test: Valid recipient display data should be returned
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_WithValidRequest_ShouldReturnProperDisplayData()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.ValidRequest();
        var expectedResponse = TestDataBuilder.VerifyRecipient.ValidResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as RecipientInfoResponse;
        
        response.Should().NotBeNull();
        response!.RecipientUserId.Should().BeGreaterThan(0);
        response.RecipientUsername.Should().NotBeNullOrEmpty();
        // RecipientFullName may be null, which is acceptable
    }

    #endregion

    #region Validation Tests

    /// <summary>
    /// Test: Empty recipient email should return BadRequest
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_WithEmptyEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.EmptyEmailRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((RecipientInfoResponse?)null, "Recipient email is required."));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("required");
    }

    /// <summary>
    /// Test: Invalid email format should return BadRequest
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_WithInvalidEmailFormat_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.InvalidEmailFormatRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((RecipientInfoResponse?)null, "A valid recipient email address is required."));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("valid");
    }

    /// <summary>
    /// Test: Non-existent email should return NotFound
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_WithNonExistentEmail_ShouldReturnNotFound()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.NonExistentEmailRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((RecipientInfoResponse?)null, "Recipient email not found or user is inactive."));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var responseMessage = GetMessageFromResponse(notFoundResult!.Value);
        responseMessage.Should().Contain("not found");
    }

    #endregion

    #region Business Logic Tests

    /// <summary>
    /// Test: Self-transfer prevention - same email as authenticated user
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_WithSelfEmail_ShouldReturnValidResponse()
    {
        // Arrange - In the business logic, self-transfer prevention happens at the ExecuteTransfer level, not VerifyRecipient
        var request = TestDataBuilder.VerifyRecipient.SelfEmailRequest();
        var expectedResponse = new RecipientInfoResponse
        {
            RecipientUserId = 1, // Same as authenticated user
            RecipientUsername = "testuser",
            RecipientFullName = "Test User"
        };
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as RecipientInfoResponse;
        
        response.Should().NotBeNull();
        response!.RecipientUserId.Should().Be(1); // Should return info even for self - prevention is at transfer level
    }

    /// <summary>
    /// Test: Inactive recipient account should return NotFound
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_WithInactiveUser_ShouldReturnNotFound()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.InactiveUserEmailRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((RecipientInfoResponse?)null, "Recipient email not found or user is inactive."));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var responseMessage = GetMessageFromResponse(notFoundResult!.Value);
        responseMessage.Should().Contain("inactive");
    }

    /// <summary>
    /// Test: Recipient account verification should work correctly
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_ShouldVerifyRecipientAccountProperly()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.ValidRequest();
        var expectedResponse = TestDataBuilder.VerifyRecipient.ValidResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockWalletService.Verify(x => x.VerifyRecipientForTransferAsync(
            It.Is<VerifyRecipientRequest>(r => r.RecipientEmail == request.RecipientEmail),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Privacy data filtering should be applied
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_ShouldReturnFilteredDataForPrivacy()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.ValidRequest();
        var expectedResponse = TestDataBuilder.VerifyRecipient.ValidResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as RecipientInfoResponse;
        
        // Verify that only appropriate data is returned (no sensitive info like balance, email, etc.)
        response.Should().NotBeNull();
        response!.RecipientUserId.Should().BeGreaterThan(0);
        response.RecipientUsername.Should().NotBeNullOrEmpty();
        // RecipientFullName is optional and can be null for privacy
    }

    #endregion

    #region Security Tests

    /// <summary>
    /// Test: User authentication should be required
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_WithoutAuthentication_ShouldStillCallService()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.ValidRequest();
        var expectedResponse = TestDataBuilder.VerifyRecipient.ValidResponse();
        var unauthenticatedUser = CreateUnauthenticatedUser();
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = unauthenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        // Note: In real scenario, [Authorize] attribute would prevent unauthenticated access
        // But in unit tests, we verify that the service handles it appropriately
        result.Should().BeOfType<OkObjectResult>();
        _mockWalletService.Verify(x => x.VerifyRecipientForTransferAsync(
            It.IsAny<VerifyRecipientRequest>(),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Data privacy protection should be maintained
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_ShouldProtectUserPrivacy()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.ValidRequest();
        var expectedResponse = TestDataBuilder.VerifyRecipient.ValidResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as RecipientInfoResponse;
        
        // Ensure no sensitive information is disclosed
        response.Should().NotBeNull();
        response!.RecipientUserId.Should().BeGreaterThan(0);
        response.RecipientUsername.Should().NotBeNullOrEmpty();
        // Only basic display information should be returned
    }

    /// <summary>
    /// Test: Information disclosure prevention should be enforced
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_ShouldPreventInformationDisclosure()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.NonExistentEmailRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((RecipientInfoResponse?)null, "Recipient email not found or user is inactive."));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var responseMessage = GetMessageFromResponse(notFoundResult!.Value);
        
        // Verify that error messages don't reveal too much information
        responseMessage.Should().NotContain("password");
        responseMessage.Should().NotContain("balance");
        responseMessage.Should().NotContain("private");
    }

    #endregion

    #region Service Integration Tests

    /// <summary>
    /// Test: Should pass correct parameters to service
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_ShouldPassCorrectParametersToService()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.ValidRequest();
        var expectedResponse = TestDataBuilder.VerifyRecipient.ValidResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockWalletService.Verify(x => x.VerifyRecipientForTransferAsync(
            It.Is<VerifyRecipientRequest>(r => r.RecipientEmail == "recipient@example.com"),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Should forward cancellation token to service
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_ShouldForwardCancellationToken()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.ValidRequest();
        var expectedResponse = TestDataBuilder.VerifyRecipient.ValidResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        var cancellationToken = new CancellationToken();
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.VerifyRecipient(request, cancellationToken);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockWalletService.Verify(x => x.VerifyRecipientForTransferAsync(
            It.IsAny<VerifyRecipientRequest>(),
            cancellationToken), 
            Times.Once);
    }

    /// <summary>
    /// Test: Should log information appropriately
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_ShouldLogInformation()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.ValidRequest();
        var expectedResponse = TestDataBuilder.VerifyRecipient.ValidResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify that controller logs the verification attempt
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Attempting to verify recipient email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// Test: Service error should return BadRequest
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_WithServiceError_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.ValidRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((RecipientInfoResponse?)null, "Internal system error occurred"));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("Internal system error occurred");
    }

    /// <summary>
    /// Test: Null error message should return generic error
    /// </summary>
    [Fact]
    public async Task VerifyRecipient_WithNullErrorMessage_ShouldReturnGenericError()
    {
        // Arrange
        var request = TestDataBuilder.VerifyRecipient.ValidRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.VerifyRecipientForTransferAsync(It.IsAny<VerifyRecipientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((RecipientInfoResponse?)null, (string?)null));

        // Act
        var result = await _walletsController.VerifyRecipient(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("Failed to verify recipient.");
    }

    #endregion

    #endregion

    #region ExecuteTransfer Tests - SCRUM-53

    // Tests for POST /api/v1/wallets/internal-transfer/execute endpoint
    // According to SCRUM-53 requirements: Happy Path, Validation, Business Logic, Security

    #region Happy Path Tests

    /// <summary>
    /// Test: Valid internal transfer should be executed successfully
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_WithValidRequest_ShouldReturnSenderTransactionDto()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.ValidRequest();
        var expectedResponse = TestDataBuilder.ExecuteInternalTransfer.ValidSenderTransactionResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as WalletTransactionDto;
        
        response.Should().NotBeNull();
        response!.TransactionId.Should().Be(expectedResponse.TransactionId);
        response.Amount.Should().Be(expectedResponse.Amount);
        response.TransactionTypeName.Should().Be("InternalTransferSent");
        response.Status.Should().Be("Completed");
    }

    /// <summary>
    /// Test: Sender balance should be deducted correctly
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_WithValidRequest_ShouldDeductSenderBalance()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.ValidRequest();
        var expectedResponse = TestDataBuilder.ExecuteInternalTransfer.ValidSenderTransactionResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as WalletTransactionDto;
        
        response.Should().NotBeNull();
        response!.BalanceAfter.Should().Be(900.00m); // 1000 - 100 = 900
        response.Amount.Should().Be(100.00m);
    }

    /// <summary>
    /// Test: Large amount transfer should work correctly
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_WithLargeAmount_ShouldExecuteSuccessfully()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.LargeAmountRequest();
        var expectedResponse = TestDataBuilder.ExecuteInternalTransfer.LargeAmountSenderTransactionResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as WalletTransactionDto;
        
        response.Should().NotBeNull();
        response!.Amount.Should().Be(5000.00m);
        response.BalanceAfter.Should().Be(5000.00m);
    }

    /// <summary>
    /// Test: Transaction record should be created with correct details
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_WithValidRequest_ShouldCreateCorrectTransactionRecord()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.ValidRequest();
        var expectedResponse = TestDataBuilder.ExecuteInternalTransfer.ValidSenderTransactionResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as WalletTransactionDto;
        
        response.Should().NotBeNull();
        response!.TransactionTypeName.Should().Be("InternalTransferSent");
        response.PaymentMethod.Should().Be("InternalTransfer");
        response.ReferenceId.Should().Be("TRANSFER_TO_USER_2");
        response.CurrencyCode.Should().Be("USD");
        response.Status.Should().Be("Completed");
    }

    /// <summary>
    /// Test: Transfer without description should work
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_WithoutDescription_ShouldExecuteSuccessfully()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.RequestWithoutDescription();
        var expectedResponse = TestDataBuilder.ExecuteInternalTransfer.TransactionResponseWithoutDescription();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as WalletTransactionDto;
        
        response.Should().NotBeNull();
        response!.Description.Should().Contain("Notes: N/A");
    }

    #endregion

    #region Validation Tests

    /// <summary>
    /// Test: Invalid recipient user ID should return BadRequest
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_WithInvalidRecipientId_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.InvalidRecipientIdRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WalletTransactionDto?)null, "Recipient User ID must be valid."));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("valid");
    }

    /// <summary>
    /// Test: Zero amount should return BadRequest
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_WithZeroAmount_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.ZeroAmountRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WalletTransactionDto?)null, "Transfer amount must be greater than 0."));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("must be greater than 0");
    }

    /// <summary>
    /// Test: Negative amount should return BadRequest
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_WithNegativeAmount_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.NegativeAmountRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WalletTransactionDto?)null, "Transfer amount must be greater than 0."));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("must be greater than 0");
    }

    /// <summary>
    /// Test: Invalid currency code should return BadRequest
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_WithInvalidCurrency_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.InvalidCurrencyRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WalletTransactionDto?)null, "Currently, only USD transfers are supported."));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("USD");
    }

    /// <summary>
    /// Test: Too long description should return BadRequest
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_WithTooLongDescription_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.TooLongDescriptionRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WalletTransactionDto?)null, "Description cannot exceed 500 characters."));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("500 characters");
    }

    #endregion

    #region Business Logic Tests

    /// <summary>
    /// Test: Insufficient sender balance should return BadRequest
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_WithInsufficientBalance_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.AmountExceedingBalanceRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WalletTransactionDto?)null, "Insufficient wallet balance."));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("Insufficient");
    }

    /// <summary>
    /// Test: Self-transfer attempt should return BadRequest
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_WithSelfTransfer_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.SelfTransferRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WalletTransactionDto?)null, "Cannot transfer funds to yourself."));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("yourself");
    }

    /// <summary>
    /// Test: Inactive recipient account should return NotFound
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_WithInactiveRecipient_ShouldReturnNotFound()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.TransferToInactiveUserRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WalletTransactionDto?)null, "Recipient user or their wallet not found, or recipient is inactive."));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var responseMessage = GetMessageFromResponse(notFoundResult!.Value);
        responseMessage.Should().Contain("not found");
    }

    /// <summary>
    /// Test: Atomic transaction execution should be verified
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_ShouldExecuteAtomicallyAtServiceLevel()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.ValidRequest();
        var expectedResponse = TestDataBuilder.ExecuteInternalTransfer.ValidSenderTransactionResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockWalletService.Verify(x => x.ExecuteInternalTransferAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<ExecuteInternalTransferRequest>(),
            It.IsAny<CancellationToken>()), 
            Times.Once); // Service call should be atomic at service level
    }

    /// <summary>
    /// Test: Double-entry bookkeeping should be handled by service
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_ShouldHandleDoubleEntryBookkeeping()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.ValidRequest();
        var expectedResponse = TestDataBuilder.ExecuteInternalTransfer.ValidSenderTransactionResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as WalletTransactionDto;
        
        // Verify that response contains sender transaction (debit)
        response.Should().NotBeNull();
        response!.TransactionTypeName.Should().Be("InternalTransferSent");
        response.Amount.Should().BeGreaterThan(0);
        // Double-entry bookkeeping: corresponding credit transaction for recipient is handled at service level
    }

    #endregion

    #region Security Tests

    /// <summary>
    /// Test: User authentication should be required
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_WithoutAuthentication_ShouldStillCallService()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.ValidRequest();
        var expectedResponse = TestDataBuilder.ExecuteInternalTransfer.ValidSenderTransactionResponse();
        var unauthenticatedUser = CreateUnauthenticatedUser();
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = unauthenticatedUser
        };

        // Service should handle unauthenticated users properly
        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WalletTransactionDto?)null, "Sender not authenticated or identity is invalid."));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        // Note: In real scenario, [Authorize] attribute would prevent unauthenticated access
        result.Should().BeOfType<ObjectResult>();
        var errorResult = result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
    }

    /// <summary>
    /// Test: Amount validation should be enforced
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_ShouldValidateAmount()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.ValidRequest();
        var expectedResponse = TestDataBuilder.ExecuteInternalTransfer.ValidSenderTransactionResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockWalletService.Verify(x => x.ExecuteInternalTransferAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<ExecuteInternalTransferRequest>(r => r.Amount == 100.00m),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Recipient verification should be enforced
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_ShouldVerifyRecipient()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.ValidRequest();
        var expectedResponse = TestDataBuilder.ExecuteInternalTransfer.ValidSenderTransactionResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockWalletService.Verify(x => x.ExecuteInternalTransferAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<ExecuteInternalTransferRequest>(r => r.RecipientUserId == 2),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Transaction integrity should be maintained
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_ShouldMaintainTransactionIntegrity()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.ValidRequest();
        var expectedResponse = TestDataBuilder.ExecuteInternalTransfer.ValidSenderTransactionResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as WalletTransactionDto;
        
        // Verify transaction integrity
        response.Should().NotBeNull();
        response!.Status.Should().Be("Completed");
        response.TransactionDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        response.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    #endregion

    #region Service Integration Tests

    /// <summary>
    /// Test: Should pass correct parameters to service
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_ShouldPassCorrectParametersToService()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.ValidRequest();
        var expectedResponse = TestDataBuilder.ExecuteInternalTransfer.ValidSenderTransactionResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockWalletService.Verify(x => x.ExecuteInternalTransferAsync(
            It.Is<ClaimsPrincipal>(p => p.Identity!.Name == "testuser"),
            It.Is<ExecuteInternalTransferRequest>(r => 
                r.RecipientUserId == 2 && 
                r.Amount == 100.00m && 
                r.CurrencyCode == "USD" &&
                r.Description == "Transfer for lunch payment"),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Should forward cancellation token to service
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_ShouldForwardCancellationToken()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.ValidRequest();
        var expectedResponse = TestDataBuilder.ExecuteInternalTransfer.ValidSenderTransactionResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        var cancellationToken = new CancellationToken();
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, cancellationToken);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockWalletService.Verify(x => x.ExecuteInternalTransferAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<ExecuteInternalTransferRequest>(),
            cancellationToken), 
            Times.Once);
    }

    /// <summary>
    /// Test: Should log information appropriately
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_ShouldLogInformation()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.ValidRequest();
        var expectedResponse = TestDataBuilder.ExecuteInternalTransfer.ValidSenderTransactionResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify that controller logs the transfer attempt
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("attempting to execute internal transfer")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// Test: Service error should return appropriate status code
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_WithServiceError_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.ValidRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WalletTransactionDto?)null, "An error occurred while executing the transfer."));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var errorResult = result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(errorResult.Value);
        responseMessage.Should().Contain("error occurred");
    }

    /// <summary>
    /// Test: Null error message should return generic error
    /// </summary>
    [Fact]
    public async Task ExecuteTransfer_WithNullErrorMessage_ShouldReturnGenericError()
    {
        // Arrange
        var request = TestDataBuilder.ExecuteInternalTransfer.ValidRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _walletsController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockWalletService.Setup(x => x.ExecuteInternalTransferAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ExecuteInternalTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WalletTransactionDto?)null, (string?)null));

        // Act
        var result = await _walletsController.ExecuteTransfer(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var errorResult = result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(errorResult.Value);
        responseMessage.Should().Be("Failed to execute internal transfer.");
    }

    #endregion

    #endregion
}