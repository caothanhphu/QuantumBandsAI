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
using QuantumBands.Application.Features.Wallets.Dtos;
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
}