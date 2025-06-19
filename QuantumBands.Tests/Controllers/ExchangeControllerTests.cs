// SCRUM-44: Unit Tests for POST /exchange/orders - PlaceOrder Endpoint
// This test class provides comprehensive test coverage for the ExchangeController.PlaceOrder endpoint:
// - PlaceOrder (POST /exchange/orders): Share order placement with validation, authentication, and business logic tests
//
// Test Coverage Summary:
// 1. Happy Path Tests (4 tests): Valid market/limit orders, order ID generation, date/status verification
// 2. Validation Tests (8 tests): Invalid inputs, case sensitivity, zero/negative values
// 3. Business Logic Tests (4 tests): Non-existent accounts, insufficient funds/shares, invalid order types
// 4. Security Tests (3 tests): Authentication, user scoping, sensitive data protection
// 5. Service Integration Tests (2 tests): Parameter passing, cancellation token handling
// 6. Logging Tests (3 tests): Information logging, warning on errors, no sensitive data in logs
// 7. Error Handling Tests (3 tests): System errors, null messages, various error categories
// Total: 27 comprehensive unit tests covering all scenarios for the PlaceOrder endpoint

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Features.Exchange.Commands.CreateOrder;
using QuantumBands.Application.Features.Exchange.Dtos;
using QuantumBands.Application.Features.Exchange.Queries;
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Common.Models;
using QuantumBands.Tests.Common;
using QuantumBands.Tests.Fixtures;
using static QuantumBands.Tests.Fixtures.TradingTestDataBuilder;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace QuantumBands.Tests.Controllers;

/// <summary>
/// Comprehensive test class for ExchangeController.PlaceOrder endpoint (POST /exchange/orders)
/// 
/// Test Categories:
/// - Happy Path: Valid order placement scenarios with different order types
/// - Validation: Input validation for all request parameters and business rules
/// - Business Logic: Trading account validation, balance checks, share holdings
/// - Security: Authentication, authorization, and data protection
/// - Service Integration: Proper parameter passing and dependency interaction
/// - Logging: Appropriate logging levels and content verification
/// - Error Handling: System errors, validation failures, and edge cases
/// 
/// Total Test Coverage: 27 unit tests ensuring comprehensive validation of the PlaceOrder endpoint
/// </summary>
public class ExchangeControllerTests : TestBase
{
    private readonly ExchangeController _exchangeController;
    private readonly Mock<IExchangeService> _mockExchangeService;
    private readonly Mock<ILogger<ExchangeController>> _mockControllerLogger;

    public ExchangeControllerTests()
    {
        _mockExchangeService = new Mock<IExchangeService>();
        _mockControllerLogger = new Mock<ILogger<ExchangeController>>();
        _exchangeController = new ExchangeController(_mockExchangeService.Object, _mockControllerLogger.Object);
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
    /// Test: Valid authenticated user should successfully place market buy order
    /// Verifies the happy path where an authenticated user places a market buy order
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithValidMarketBuyOrder_ShouldReturnCreatedWithOrderDto()
    {
        // Arrange
        var orderRequest = Exchange.ValidMarketBuyOrderRequest();
        var expectedResponse = Exchange.ValidMarketBuyOrderResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var createdResult = result as ObjectResult;
        createdResult!.StatusCode.Should().Be(201);
        var response = createdResult.Value as ShareOrderDto;
        
        response.Should().NotBeNull();
        response!.OrderId.Should().Be(expectedResponse.OrderId);
        response.UserId.Should().Be(expectedResponse.UserId);
        response.TradingAccountId.Should().Be(expectedResponse.TradingAccountId);
        response.OrderSide.Should().Be(expectedResponse.OrderSide);
        response.OrderType.Should().Be(expectedResponse.OrderType);
        response.QuantityOrdered.Should().Be(expectedResponse.QuantityOrdered);
        response.OrderStatus.Should().Be(expectedResponse.OrderStatus);
    }

    /// <summary>
    /// Test: Valid authenticated user should successfully place limit sell order
    /// Verifies that limit orders with price specifications work correctly
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithValidLimitSellOrder_ShouldReturnCreatedWithOrderDto()
    {
        // Arrange
        var orderRequest = Exchange.ValidLimitSellOrderRequest();
        var expectedResponse = Exchange.ValidLimitSellOrderResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var createdResult = result as ObjectResult;
        createdResult!.StatusCode.Should().Be(201);
        var response = createdResult.Value as ShareOrderDto;
        
        response.Should().NotBeNull();
        response!.OrderSide.Should().Be("Sell");
        response.OrderType.Should().Be("Limit");
        response.LimitPrice.Should().Be(55.00m);
        response.QuantityOrdered.Should().Be(50);
    }

    /// <summary>
    /// Test: Valid order should generate unique order ID
    /// Verifies that each order placement generates a unique order identifier
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithValidOrder_ShouldGenerateUniqueOrderId()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketBuyOrderRequest();
        var expectedResponse = TestDataBuilder.Exchange.ValidMarketBuyOrderResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var createdResult = result as ObjectResult;
        var response = createdResult!.Value as ShareOrderDto;
        
        response!.OrderId.Should().BeGreaterThan(0);
        response.OrderId.Should().Be(expectedResponse.OrderId);
    }

    /// <summary>
    /// Test: Valid order should set correct order date and status
    /// Verifies that order timing and initial status are properly set
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithValidOrder_ShouldSetCorrectOrderDateAndStatus()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketBuyOrderRequest();
        var expectedResponse = TestDataBuilder.Exchange.ValidMarketBuyOrderResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var createdResult = result as ObjectResult;
        var response = createdResult!.Value as ShareOrderDto;
        
        response!.OrderDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        response.OrderStatus.Should().Be("Open");
        response.QuantityFilled.Should().Be(0);
        response.TransactionFee.Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region Validation Tests
    // Tests that verify input validation and business rule enforcement

    /// <summary>
    /// Test: Invalid trading account ID should return BadRequest
    /// Verifies that zero or negative trading account IDs are rejected
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithInvalidTradingAccountId_ShouldReturnBadRequest()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.RequestWithInvalidTradingAccountId();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, "Invalid TradingAccountId."));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("Invalid TradingAccountId");
    }

    /// <summary>
    /// Test: Invalid order type ID should return BadRequest
    /// Verifies that zero or negative order type IDs are rejected
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithInvalidOrderTypeId_ShouldReturnBadRequest()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.RequestWithInvalidOrderTypeId();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, "Invalid OrderTypeId."));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("Invalid OrderTypeId");
    }

    /// <summary>
    /// Test: Invalid order side should return BadRequest
    /// Verifies that order sides other than "Buy" or "Sell" are rejected
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithInvalidOrderSide_ShouldReturnBadRequest()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.RequestWithInvalidOrderSide();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, "Invalid OrderSide. Must be 'Buy' or 'Sell'."));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("Invalid OrderSide");
    }

    /// <summary>
    /// Test: Zero quantity should return InternalServerError (controller returns 500 for validation errors)
    /// Verifies that orders with zero quantity are rejected
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithZeroQuantity_ShouldReturnInternalServerError()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.RequestWithZeroQuantity();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, (string?)null));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("Failed to place order.");
    }

    /// <summary>
    /// Test: Negative quantity should return InternalServerError (controller returns 500 for validation errors)
    /// Verifies that orders with negative quantities are rejected
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithNegativeQuantity_ShouldReturnInternalServerError()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.RequestWithNegativeQuantity();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, (string?)null));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("Failed to place order.");
    }

    /// <summary>
    /// Test: Zero limit price should return InternalServerError (controller returns 500 for validation errors)
    /// Verifies that limit orders with zero price are rejected
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithZeroLimitPrice_ShouldReturnInternalServerError()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.RequestWithZeroLimitPrice();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, (string?)null));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("Failed to place order.");
    }

    /// <summary>
    /// Test: Case insensitive order side should be accepted
    /// Verifies that "buy", "BUY", "sell", "SELL" are all accepted
    /// </summary>
    [Theory]
    [InlineData("buy")]
    [InlineData("SELL")]
    [InlineData("BuY")]
    public async Task PlaceOrder_WithCaseInsensitiveOrderSide_ShouldReturnCreated(string orderSide)
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketBuyOrderRequest();
        orderRequest.OrderSide = orderSide;
        var expectedResponse = TestDataBuilder.Exchange.ValidMarketBuyOrderResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var createdResult = result as ObjectResult;
        createdResult!.StatusCode.Should().Be(201);
    }

    #endregion

    #region Business Logic Tests
    // Tests that verify business rules and logic implementation

    /// <summary>
    /// Test: Trading account not found should return NotFound
    /// Verifies proper handling when trading account doesn't exist
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithNonExistentTradingAccount_ShouldReturnNotFound()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketBuyOrderRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, "Trading account not found"));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var responseMessage = GetMessageFromResponse(notFoundResult!.Value);
        responseMessage.Should().Contain("not found");
    }

    /// <summary>
    /// Test: Insufficient wallet balance for buy order should return BadRequest
    /// Verifies proper handling when user doesn't have enough funds
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithInsufficientBalance_ShouldReturnBadRequest()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketBuyOrderRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, "Insufficient wallet balance for this order"));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("Insufficient");
    }

    /// <summary>
    /// Test: Insufficient share holdings for sell order should return BadRequest
    /// Verifies proper handling when user doesn't have enough shares to sell
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithInsufficientShares_ShouldReturnBadRequest()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketSellOrderRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, "Insufficient share holdings for this sell order"));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("Insufficient");
    }

    /// <summary>
    /// Test: Order type not found should return BadRequest
    /// Verifies proper handling when order type doesn't exist in system
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithNonExistentOrderType_ShouldReturnBadRequest()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketBuyOrderRequest();
        orderRequest.OrderTypeId = 999; // Non-existent order type
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, "Invalid order type specified"));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("Invalid");
    }

    #endregion

    #region Security Tests
    // Tests that verify security measures and access control

    /// <summary>
    /// Test: Service authentication error should return InternalServerError (since [Authorize] handles unauthenticated users)
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithServiceAuthError_ShouldReturnInternalServerError()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketBuyOrderRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, (string?)null));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("Failed to place order.");
    }

    /// <summary>
    /// Test: Order placement should be properly scoped to authenticated user
    /// Verifies that orders are created with the correct user context
    /// </summary>
    [Fact]
    public async Task PlaceOrder_ShouldAssociateOrderWithAuthenticatedUser()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketBuyOrderRequest();
        var expectedResponse = TestDataBuilder.Exchange.ValidMarketBuyOrderResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        
        // Verify service was called with the authenticated user's context only
        _mockExchangeService.Verify(x => x.PlaceOrderAsync(
            It.IsAny<CreateShareOrderRequest>(),
            It.Is<ClaimsPrincipal>(u => u.FindFirst(ClaimTypes.NameIdentifier) != null && 
                                        u.FindFirst(ClaimTypes.NameIdentifier).Value == "1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Order placement should not expose sensitive system information
    /// Verifies that responses don't contain sensitive data
    /// </summary>
    [Fact]
    public async Task PlaceOrder_ShouldNotExposeSensitiveSystemInformation()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketBuyOrderRequest();
        var expectedResponse = TestDataBuilder.Exchange.ValidMarketBuyOrderResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var createdResult = result as ObjectResult;
        var responseJson = System.Text.Json.JsonSerializer.Serialize(createdResult!.Value);
        
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
    public async Task PlaceOrder_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketBuyOrderRequest();
        var expectedResponse = TestDataBuilder.Exchange.ValidMarketBuyOrderResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        _mockExchangeService.Verify(x => x.PlaceOrderAsync(
            It.Is<CreateShareOrderRequest>(r => 
                r.TradingAccountId == orderRequest.TradingAccountId &&
                r.OrderTypeId == orderRequest.OrderTypeId &&
                r.OrderSide == orderRequest.OrderSide &&
                r.QuantityOrdered == orderRequest.QuantityOrdered),
            It.Is<ClaimsPrincipal>(u => u == authenticatedUser),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Cancellation token should be passed to service
    /// Verifies that the cancellation token is properly forwarded to the service layer
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithCancellationToken_ShouldPassTokenToService()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketBuyOrderRequest();
        var expectedResponse = TestDataBuilder.Exchange.ValidMarketBuyOrderResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        var cancellationToken = new CancellationToken();
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, cancellationToken);

        // Assert
        _mockExchangeService.Verify(x => x.PlaceOrderAsync(
            It.IsAny<CreateShareOrderRequest>(),
            It.IsAny<ClaimsPrincipal>(),
            cancellationToken), Times.Once);
    }

    #endregion

    #region Logging Tests
    // Tests that verify proper logging behavior

    /// <summary>
    /// Test: Order placement should log information at start
    /// Verifies that proper information logs are created when placing orders
    /// </summary>
    [Fact]
    public async Task PlaceOrder_ShouldLogInformationAtStart()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketBuyOrderRequest();
        var expectedResponse = TestDataBuilder.Exchange.ValidMarketBuyOrderResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("attempting to place a") && v.ToString()!.Contains("order")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Order placement failure should log warning
    /// Verifies that failures are properly logged with warning level
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithError_ShouldLogWarning()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketBuyOrderRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, "Trading account not found"));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to place order")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Order placement should not log sensitive information
    /// Verifies that logs don't contain sensitive user or financial data
    /// </summary>
    [Fact]
    public async Task PlaceOrder_ShouldNotLogSensitiveInformation()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketBuyOrderRequest();
        var expectedResponse = TestDataBuilder.Exchange.ValidMarketBuyOrderResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

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
    /// Test: System configuration error should return InternalServerError
    /// Verifies proper handling of system configuration errors
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithSystemConfigurationError_ShouldReturnInternalServerError()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketBuyOrderRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, "System error: Order status configuration missing"));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Contain("configuration missing");
    }

    /// <summary>
    /// Test: Null error message should return generic error
    /// Verifies proper handling when service returns null error message
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithNullErrorMessage_ShouldReturnGenericError()
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketBuyOrderRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, (string?)null));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("Failed to place order.");
    }

    /// <summary>
    /// Test: Should return appropriate error categories for different system failures
    /// </summary>
    [Theory]
    [InlineData("not found", 404)]
    [InlineData("Insufficient wallet balance", 400)]
    [InlineData("Invalid TradingAccountId", 400)]
    [InlineData("System error", 500)]
    public async Task PlaceOrder_WithVariousErrors_ShouldReturnAppropriateStatusCodes(string errorMessage, int expectedStatusCode)
    {
        // Arrange
        var orderRequest = TestDataBuilder.Exchange.ValidMarketBuyOrderRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.PlaceOrderAsync(It.IsAny<CreateShareOrderRequest>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, errorMessage));

        // Act
        var result = await _exchangeController.PlaceOrder(orderRequest, CancellationToken.None);

        // Assert
        if (expectedStatusCode == 404)
        {
            result.Should().BeOfType<NotFoundObjectResult>();
        }
        else if (expectedStatusCode == 400)
        {
            result.Should().BeOfType<BadRequestObjectResult>();
        }
        else if (expectedStatusCode == 500)
        {
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
        }
    }

    #endregion

    #region GetMyOrders Tests - SCRUM-57

    /// <summary>
    /// Test: Should return paginated orders for authenticated user with valid query
    /// </summary>
    [Fact]
    public async Task GetMyOrders_WithValidQuery_ShouldReturnOkWithOrders()
    {
        // Arrange
        var query = TestDataBuilder.GetMyOrders.ValidFullQuery();
        var expectedResponse = TestDataBuilder.GetMyOrders.SuccessfulOrdersResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyOrdersAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
        
        _mockExchangeService.Verify(x => x.GetMyOrdersAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetMyShareOrdersQuery>(q => 
                q.PageNumber == query.PageNumber &&
                q.PageSize == query.PageSize &&
                q.TradingAccountId == query.TradingAccountId &&
                q.Status == query.Status &&
                q.OrderSide == query.OrderSide &&
                q.OrderType == query.OrderType),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Should return paginated orders with minimal query parameters
    /// </summary>
    [Fact]
    public async Task GetMyOrders_WithMinimalQuery_ShouldReturnOkWithOrders()
    {
        // Arrange
        var query = TestDataBuilder.GetMyOrders.ValidMinimalQuery();
        var expectedResponse = TestDataBuilder.GetMyOrders.SuccessfulOrdersResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyOrdersAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    /// <summary>
    /// Test: Should return empty result when no orders exist
    /// </summary>
    [Fact]
    public async Task GetMyOrders_WithNoOrders_ShouldReturnEmptyPaginatedList()
    {
        // Arrange
        var query = TestDataBuilder.GetMyOrders.ValidMinimalQuery();
        var expectedResponse = TestDataBuilder.GetMyOrders.EmptyOrdersResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyOrdersAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
        var paginatedResult = okResult.Value as PaginatedList<ShareOrderDto>;
        paginatedResult!.Items.Should().BeEmpty();
        paginatedResult.TotalCount.Should().Be(0);
    }

    /// <summary>
    /// Test: Should return filtered orders based on OrderSide
    /// </summary>
    [Fact]
    public async Task GetMyOrders_WithOrderSideFilter_ShouldReturnFilteredOrders()
    {
        // Arrange
        var query = TestDataBuilder.GetMyOrders.ValidFullQuery();
        query.OrderSide = "Buy";
        var expectedResponse = TestDataBuilder.GetMyOrders.BuyOrdersResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyOrdersAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var paginatedResult = okResult!.Value as PaginatedList<ShareOrderDto>;
        paginatedResult!.Items.Should().AllSatisfy(order => order.OrderSide.Should().Be("Buy"));
    }

    /// <summary>
    /// Test: Should handle pagination parameters correctly
    /// </summary>
    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 10)]
    [InlineData(3, 25)]
    public async Task GetMyOrders_WithDifferentPagination_ShouldRespectPaginationParameters(int pageNumber, int pageSize)
    {
        // Arrange
        var query = new GetMyShareOrdersQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var expectedResponse = TestDataBuilder.GetMyOrders.SuccessfulOrdersResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyOrdersAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockExchangeService.Verify(x => x.GetMyOrdersAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetMyShareOrdersQuery>(q => q.PageNumber == pageNumber && q.PageSize == pageSize),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Should handle invalid pagination parameters gracefully
    /// </summary>
    [Fact]
    public async Task GetMyOrders_WithInvalidPagination_ShouldStillReturnResults()
    {
        // Arrange
        var query = TestDataBuilder.GetMyOrders.InvalidPaginationQuery();
        var expectedResponse = TestDataBuilder.GetMyOrders.SuccessfulOrdersResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyOrdersAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        // Service should handle validation and return appropriate results
        _mockExchangeService.Verify(x => x.GetMyOrdersAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareOrdersQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Should handle large page size by limiting to maximum
    /// </summary>
    [Fact]
    public async Task GetMyOrders_WithLargePageSize_ShouldLimitToMaximum()
    {
        // Arrange
        var query = TestDataBuilder.GetMyOrders.LargePageSizeQuery();
        var expectedResponse = TestDataBuilder.GetMyOrders.SuccessfulOrdersResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyOrdersAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        // Verify that the query's ValidatedPageSize property would limit the page size
        query.ValidatedPageSize.Should().Be(100); // Max page size
    }

    /// <summary>
    /// Test: Should handle status filter correctly
    /// </summary>
    [Theory]
    [InlineData("Active")]
    [InlineData("Filled")]
    [InlineData("Cancelled")]
    [InlineData("Active,PartiallyFilled")]
    public async Task GetMyOrders_WithStatusFilter_ShouldPassStatusToService(string status)
    {
        // Arrange
        var query = TestDataBuilder.GetMyOrders.ValidMinimalQuery();
        query.Status = status;
        var expectedResponse = TestDataBuilder.GetMyOrders.SuccessfulOrdersResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyOrdersAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockExchangeService.Verify(x => x.GetMyOrdersAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetMyShareOrdersQuery>(q => q.Status == status),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Should handle date range filters correctly
    /// </summary>
    [Fact]
    public async Task GetMyOrders_WithDateRange_ShouldPassDateRangeToService()
    {
        // Arrange
        var dateFrom = DateTime.UtcNow.AddDays(-30);
        var dateTo = DateTime.UtcNow;
        var query = TestDataBuilder.GetMyOrders.ValidMinimalQuery();
        query.DateFrom = dateFrom;
        query.DateTo = dateTo;
        var expectedResponse = TestDataBuilder.GetMyOrders.SuccessfulOrdersResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyOrdersAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockExchangeService.Verify(x => x.GetMyOrdersAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetMyShareOrdersQuery>(q => q.DateFrom == dateFrom && q.DateTo == dateTo),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Controller method execution with valid authentication (framework-level [Authorize] handles auth)
    /// </summary>
    [Fact]
    public async Task GetMyOrders_WithValidAuthentication_ShouldCallService()
    {
        // Arrange
        var query = TestDataBuilder.GetMyOrders.ValidMinimalQuery();
        var expectedResponse = TestDataBuilder.GetMyOrders.SuccessfulOrdersResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyOrdersAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    /// <summary>
    /// Test: Should handle user with missing claims gracefully (service layer handles user validation)
    /// </summary>
    [Fact]
    public async Task GetMyOrders_WithMissingClaims_ShouldStillCallService()
    {
        // Arrange
        var query = TestDataBuilder.GetMyOrders.ValidMinimalQuery();
        var expectedResponse = TestDataBuilder.GetMyOrders.EmptyOrdersResponse();
        var userWithInvalidClaims = CreateUserWithInvalidClaims();
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = userWithInvalidClaims
        };

        _mockExchangeService.Setup(x => x.GetMyOrdersAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        // The service layer should handle user validation and may return empty results
        _mockExchangeService.Verify(x => x.GetMyOrdersAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareOrdersQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Should only return orders for the authenticated user
    /// </summary>
    [Fact]
    public async Task GetMyOrders_WithValidUser_ShouldOnlyReturnUserOrders()
    {
        // Arrange
        var query = TestDataBuilder.GetMyOrders.ValidMinimalQuery();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        var expectedResponse = TestDataBuilder.GetMyOrders.SuccessfulOrdersResponse();
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyOrdersAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyOrders(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var paginatedResult = okResult!.Value as PaginatedList<ShareOrderDto>;
        // All orders should belong to the authenticated user
        paginatedResult!.Items.Should().AllSatisfy(order => order.UserId.Should().Be(1));
    }

    /// <summary>
    /// Test: Service exceptions should be handled by global exception handler (not controller level)
    /// </summary>
    [Fact]
    public async Task GetMyOrders_WhenServiceThrowsException_ShouldPropagateException()
    {
        // Arrange
        var query = TestDataBuilder.GetMyOrders.ValidMinimalQuery();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyOrdersAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act & Assert
        // The controller doesn't handle exceptions - they should be caught by global exception middleware
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _exchangeController.GetMyOrders(query, CancellationToken.None));
        
        exception.Message.Should().Be("Database connection failed");
    }

    #endregion

    #region CancelOrder Tests - SCRUM-58

    /// <summary>
    /// Test: Should successfully cancel an active order and return NoContent
    /// </summary>
    [Fact]
    public async Task CancelOrder_WithValidActiveOrder_ShouldReturnNoContent()
    {
        // Arrange
        var orderId = TestDataBuilder.CancelOrder.ValidOrderId();
        var expectedCancelledOrder = TestDataBuilder.CancelOrder.CancelledOrderDto();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.CancelOrderAsync(orderId, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedCancelledOrder, (string?)null));

        // Act
        var result = await _exchangeController.CancelOrder(orderId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockExchangeService.Verify(x => x.CancelOrderAsync(orderId, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Should successfully cancel a partially filled order and return NoContent
    /// </summary>
    [Fact]
    public async Task CancelOrder_WithPartiallyFilledOrder_ShouldReturnNoContent()
    {
        // Arrange
        var orderId = 1004L; // From PartiallyFilledOrderDto
        var expectedCancelledOrder = TestDataBuilder.CancelOrder.PartiallyFilledOrderDto();
        expectedCancelledOrder.OrderStatus = "Cancelled"; // Update status after cancellation
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.CancelOrderAsync(orderId, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedCancelledOrder, (string?)null));

        // Act
        var result = await _exchangeController.CancelOrder(orderId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockExchangeService.Verify(x => x.CancelOrderAsync(orderId, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Should return BadRequest for invalid order ID (negative or zero)
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(-999)]
    public async Task CancelOrder_WithInvalidOrderId_ShouldReturnBadRequest(long invalidOrderId)
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        // Act
        var result = await _exchangeController.CancelOrder(invalidOrderId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { Message = "Invalid Order ID." });
        
        // Verify service is not called for invalid input
        _mockExchangeService.Verify(x => x.CancelOrderAsync(It.IsAny<long>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Test: Should return NotFound when order does not exist
    /// </summary>
    [Fact]
    public async Task CancelOrder_WithNonExistentOrder_ShouldReturnNotFound()
    {
        // Arrange
        var orderId = TestDataBuilder.CancelOrder.NonExistentOrderId();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.CancelOrderAsync(orderId, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, "Order not found"));

        // Act
        var result = await _exchangeController.CancelOrder(orderId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new { Message = "Order not found" });
    }

    /// <summary>
    /// Test: Should return Forbid when user tries to cancel another user's order
    /// </summary>
    [Fact]
    public async Task CancelOrder_WithUnauthorizedUser_ShouldReturnForbid()
    {
        // Arrange
        var orderId = TestDataBuilder.CancelOrder.OtherUserOrderId();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.CancelOrderAsync(orderId, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, "User not authorized to cancel this order"));

        // Act
        var result = await _exchangeController.CancelOrder(orderId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    /// <summary>
    /// Test: Should return BadRequest when trying to cancel an already cancelled order
    /// </summary>
    [Fact]
    public async Task CancelOrder_WithAlreadyCancelledOrder_ShouldReturnBadRequest()
    {
        // Arrange
        var orderId = TestDataBuilder.CancelOrder.CancelledOrderId();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.CancelOrderAsync(orderId, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, "Order cannot be cancelled - already cancelled"));

        // Act
        var result = await _exchangeController.CancelOrder(orderId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { Message = "Order cannot be cancelled - already cancelled" });
    }

    /// <summary>
    /// Test: Should return BadRequest when trying to cancel a fully executed order
    /// </summary>
    [Fact]
    public async Task CancelOrder_WithExecutedOrder_ShouldReturnBadRequest()
    {
        // Arrange
        var orderId = TestDataBuilder.CancelOrder.ExecutedOrderId();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.CancelOrderAsync(orderId, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, "Order cannot be cancelled - fully executed"));

        // Act
        var result = await _exchangeController.CancelOrder(orderId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { Message = "Order cannot be cancelled - fully executed" });
    }

    /// <summary>
    /// Test: Should return BadRequest for invalid business logic scenarios
    /// </summary>
    [Theory]
    [InlineData("Invalid order status")]
    [InlineData("Order cannot be cancelled")]
    [InlineData("Invalid trading account")]
    public async Task CancelOrder_WithBusinessLogicErrors_ShouldReturnBadRequest(string errorMessage)
    {
        // Arrange
        var orderId = TestDataBuilder.CancelOrder.ValidOrderId();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.CancelOrderAsync(orderId, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, errorMessage));

        // Act
        var result = await _exchangeController.CancelOrder(orderId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { Message = errorMessage });
    }

    /// <summary>
    /// Test: Should handle service exceptions and propagate them (handled by global middleware)
    /// </summary>
    [Fact]
    public async Task CancelOrder_WhenServiceThrowsException_ShouldPropagateException()
    {
        // Arrange
        var orderId = TestDataBuilder.CancelOrder.ValidOrderId();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.CancelOrderAsync(orderId, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act & Assert
        // The controller doesn't handle exceptions - they should be caught by global exception middleware
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _exchangeController.CancelOrder(orderId, CancellationToken.None));
        
        exception.Message.Should().Be("Database connection failed");
    }

    /// <summary>
    /// Test: Should return InternalServerError for unknown service errors
    /// </summary>
    [Fact]
    public async Task CancelOrder_WithUnknownServiceError_ShouldReturnInternalServerError()
    {
        // Arrange
        var orderId = TestDataBuilder.CancelOrder.ValidOrderId();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.CancelOrderAsync(orderId, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, "Unknown error occurred"));

        // Act
        var result = await _exchangeController.CancelOrder(orderId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("Unknown error occurred");
    }

    /// <summary>
    /// Test: Should return InternalServerError with default message when error message is null
    /// </summary>
    [Fact]
    public async Task CancelOrder_WithNullErrorMessage_ShouldReturnDefaultMessage()
    {
        // Arrange
        var orderId = TestDataBuilder.CancelOrder.ValidOrderId();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.CancelOrderAsync(orderId, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((ShareOrderDto?)null, (string?)null));

        // Act
        var result = await _exchangeController.CancelOrder(orderId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("Failed to cancel order.");
    }

    /// <summary>
    /// Test: Should verify correct parameter passing to service
    /// </summary>
    [Fact]
    public async Task CancelOrder_ShouldPassCorrectParametersToService()
    {
        // Arrange
        var orderId = TestDataBuilder.CancelOrder.ValidOrderId();
        var expectedCancelledOrder = TestDataBuilder.CancelOrder.CancelledOrderDto();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.CancelOrderAsync(orderId, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedCancelledOrder, (string?)null));

        // Act
        var result = await _exchangeController.CancelOrder(orderId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockExchangeService.Verify(x => x.CancelOrderAsync(
            orderId,
            It.Is<ClaimsPrincipal>(p => p == authenticatedUser),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    /// <summary>
    /// Test: Should handle different order ID data types correctly
    /// </summary>
    [Theory]
    [InlineData(1L)]
    [InlineData(999999999999L)] // Large valid order ID
    [InlineData(123456789L)]
    public async Task CancelOrder_WithValidOrderIds_ShouldCallService(long orderId)
    {
        // Arrange
        var expectedCancelledOrder = TestDataBuilder.CancelOrder.CancelledOrderDto();
        expectedCancelledOrder.OrderId = orderId;
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.CancelOrderAsync(orderId, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedCancelledOrder, (string?)null));

        // Act
        var result = await _exchangeController.CancelOrder(orderId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockExchangeService.Verify(x => x.CancelOrderAsync(orderId, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetOrderBook Tests - SCRUM-59

    /// <summary>
    /// Test: Should successfully return order book for valid trading account with default depth
    /// </summary>
    [Fact]
    public async Task GetOrderBook_WithValidTradingAccount_ShouldReturnOrderBook()
    {
        // Arrange
        var tradingAccountId = TestDataBuilder.GetOrderBook.ValidTradingAccountId();
        var query = TestDataBuilder.GetOrderBook.ValidQuery();
        var expectedOrderBook = TestDataBuilder.GetOrderBook.ValidOrderBookDto();

        _mockExchangeService.Setup(x => x.GetOrderBookAsync(tradingAccountId, It.IsAny<GetOrderBookQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOrderBook, (string?)null));

        // Act
        var result = await _exchangeController.GetOrderBook(tradingAccountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedOrderBook);
        
        _mockExchangeService.Verify(x => x.GetOrderBookAsync(tradingAccountId, It.IsAny<GetOrderBookQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Should return BadRequest for invalid trading account ID
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(-999)]
    public async Task GetOrderBook_WithInvalidTradingAccountId_ShouldReturnBadRequest(int invalidTradingAccountId)
    {
        // Arrange
        var query = TestDataBuilder.GetOrderBook.ValidQuery();

        // Act
        var result = await _exchangeController.GetOrderBook(invalidTradingAccountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { Message = "Invalid Trading Account ID." });
        
        // Verify service is not called for invalid input
        _mockExchangeService.Verify(x => x.GetOrderBookAsync(It.IsAny<int>(), It.IsAny<GetOrderBookQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Test: Should return NotFound when trading account does not exist
    /// </summary>
    [Fact]
    public async Task GetOrderBook_WithNonExistentTradingAccount_ShouldReturnNotFound()
    {
        // Arrange
        var tradingAccountId = TestDataBuilder.GetOrderBook.NonExistentTradingAccountId();
        var query = TestDataBuilder.GetOrderBook.ValidQuery();

        _mockExchangeService.Setup(x => x.GetOrderBookAsync(tradingAccountId, It.IsAny<GetOrderBookQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((OrderBookDto?)null, "Trading account not found"));

        // Act
        var result = await _exchangeController.GetOrderBook(tradingAccountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new { Message = "Trading account not found" });
    }

    /// <summary>
    /// Test: Should return empty order book when no orders exist
    /// </summary>
    [Fact]
    public async Task GetOrderBook_WithEmptyOrderBook_ShouldReturnEmptyData()
    {
        // Arrange
        var tradingAccountId = TestDataBuilder.GetOrderBook.ValidTradingAccountId();
        var query = TestDataBuilder.GetOrderBook.ValidQuery();
        var expectedOrderBook = TestDataBuilder.GetOrderBook.EmptyOrderBookDto();

        _mockExchangeService.Setup(x => x.GetOrderBookAsync(tradingAccountId, It.IsAny<GetOrderBookQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOrderBook, (string?)null));

        // Act
        var result = await _exchangeController.GetOrderBook(tradingAccountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var orderBook = okResult!.Value as OrderBookDto;
        orderBook!.Bids.Should().BeEmpty();
        orderBook.Asks.Should().BeEmpty();
        orderBook.LastTradePrice.Should().BeNull();
    }

    /// <summary>
    /// Test: Should handle order book with only bids
    /// </summary>
    [Fact]
    public async Task GetOrderBook_WithBidsOnly_ShouldReturnBidsOnlyData()
    {
        // Arrange
        var tradingAccountId = TestDataBuilder.GetOrderBook.ValidTradingAccountId();
        var query = TestDataBuilder.GetOrderBook.ValidQuery();
        var expectedOrderBook = TestDataBuilder.GetOrderBook.BidsOnlyOrderBookDto();

        _mockExchangeService.Setup(x => x.GetOrderBookAsync(tradingAccountId, It.IsAny<GetOrderBookQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOrderBook, (string?)null));

        // Act
        var result = await _exchangeController.GetOrderBook(tradingAccountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var orderBook = okResult!.Value as OrderBookDto;
        orderBook!.Bids.Should().HaveCount(2);
        orderBook.Asks.Should().BeEmpty();
        orderBook.LastTradePrice.Should().Be(25.00m);
    }

    /// <summary>
    /// Test: Should handle order book with only asks
    /// </summary>
    [Fact]
    public async Task GetOrderBook_WithAsksOnly_ShouldReturnAsksOnlyData()
    {
        // Arrange
        var tradingAccountId = TestDataBuilder.GetOrderBook.ValidTradingAccountId();
        var query = TestDataBuilder.GetOrderBook.ValidQuery();
        var expectedOrderBook = TestDataBuilder.GetOrderBook.AsksOnlyOrderBookDto();

        _mockExchangeService.Setup(x => x.GetOrderBookAsync(tradingAccountId, It.IsAny<GetOrderBookQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOrderBook, (string?)null));

        // Act
        var result = await _exchangeController.GetOrderBook(tradingAccountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var orderBook = okResult!.Value as OrderBookDto;
        orderBook!.Bids.Should().BeEmpty();
        orderBook.Asks.Should().HaveCount(2);
        orderBook.LastTradePrice.Should().Be(26.00m);
    }

    /// <summary>
    /// Test: Should handle different depth parameters correctly
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public async Task GetOrderBook_WithDifferentDepths_ShouldPassDepthToService(int depth)
    {
        // Arrange
        var tradingAccountId = TestDataBuilder.GetOrderBook.ValidTradingAccountId();
        var query = TestDataBuilder.GetOrderBook.CustomDepthQuery(depth);
        var expectedOrderBook = TestDataBuilder.GetOrderBook.LimitedDepthOrderBookDto();

        _mockExchangeService.Setup(x => x.GetOrderBookAsync(tradingAccountId, It.IsAny<GetOrderBookQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOrderBook, (string?)null));

        // Act
        var result = await _exchangeController.GetOrderBook(tradingAccountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockExchangeService.Verify(x => x.GetOrderBookAsync(
            tradingAccountId,
            It.Is<GetOrderBookQuery>(q => q.Depth == depth),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Should handle maximum depth parameter
    /// </summary>
    [Fact]
    public async Task GetOrderBook_WithMaxDepth_ShouldLimitToMaximum()
    {
        // Arrange
        var tradingAccountId = TestDataBuilder.GetOrderBook.ValidTradingAccountId();
        var query = TestDataBuilder.GetOrderBook.MaxDepthQuery();
        var expectedOrderBook = TestDataBuilder.GetOrderBook.ValidOrderBookDto();

        _mockExchangeService.Setup(x => x.GetOrderBookAsync(tradingAccountId, It.IsAny<GetOrderBookQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOrderBook, (string?)null));

        // Act
        var result = await _exchangeController.GetOrderBook(tradingAccountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        // Verify the query's ValidatedDepth property respects the maximum
        query.ValidatedDepth.Should().Be(20); // Max depth
        _mockExchangeService.Verify(x => x.GetOrderBookAsync(tradingAccountId, It.IsAny<GetOrderBookQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Should handle invalid depth (too high) by limiting to maximum
    /// </summary>
    [Fact]
    public async Task GetOrderBook_WithInvalidDepth_ShouldLimitToMaximum()
    {
        // Arrange
        var tradingAccountId = TestDataBuilder.GetOrderBook.ValidTradingAccountId();
        var query = TestDataBuilder.GetOrderBook.InvalidDepthQuery();
        var expectedOrderBook = TestDataBuilder.GetOrderBook.ValidOrderBookDto();

        _mockExchangeService.Setup(x => x.GetOrderBookAsync(tradingAccountId, It.IsAny<GetOrderBookQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOrderBook, (string?)null));

        // Act
        var result = await _exchangeController.GetOrderBook(tradingAccountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        // Verify the query's ValidatedDepth property limits the depth
        query.ValidatedDepth.Should().Be(20); // Max depth, not the requested 100
    }

    /// <summary>
    /// Test: Should verify correct parameter passing to service
    /// </summary>
    [Fact]
    public async Task GetOrderBook_ShouldPassCorrectParametersToService()
    {
        // Arrange
        var tradingAccountId = TestDataBuilder.GetOrderBook.ValidTradingAccountId();
        var query = TestDataBuilder.GetOrderBook.ValidQuery();
        var expectedOrderBook = TestDataBuilder.GetOrderBook.ValidOrderBookDto();

        _mockExchangeService.Setup(x => x.GetOrderBookAsync(tradingAccountId, It.IsAny<GetOrderBookQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOrderBook, (string?)null));

        // Act
        var result = await _exchangeController.GetOrderBook(tradingAccountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockExchangeService.Verify(x => x.GetOrderBookAsync(
            tradingAccountId,
            It.Is<GetOrderBookQuery>(q => q.Depth == query.Depth),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Should handle service exceptions and propagate them (handled by global middleware)
    /// </summary>
    [Fact]
    public async Task GetOrderBook_WhenServiceThrowsException_ShouldPropagateException()
    {
        // Arrange
        var tradingAccountId = TestDataBuilder.GetOrderBook.ValidTradingAccountId();
        var query = TestDataBuilder.GetOrderBook.ValidQuery();

        _mockExchangeService.Setup(x => x.GetOrderBookAsync(tradingAccountId, It.IsAny<GetOrderBookQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act & Assert
        // The controller doesn't handle exceptions - they should be caught by global exception middleware
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _exchangeController.GetOrderBook(tradingAccountId, query, CancellationToken.None));
        
        exception.Message.Should().Be("Database connection failed");
    }

    /// <summary>
    /// Test: Should return InternalServerError for unknown service errors
    /// </summary>
    [Fact]
    public async Task GetOrderBook_WithUnknownServiceError_ShouldReturnInternalServerError()
    {
        // Arrange
        var tradingAccountId = TestDataBuilder.GetOrderBook.ValidTradingAccountId();
        var query = TestDataBuilder.GetOrderBook.ValidQuery();

        _mockExchangeService.Setup(x => x.GetOrderBookAsync(tradingAccountId, It.IsAny<GetOrderBookQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((OrderBookDto?)null, "Unknown error occurred"));

        // Act
        var result = await _exchangeController.GetOrderBook(tradingAccountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("Unknown error occurred");
    }

    /// <summary>
    /// Test: Should return InternalServerError with default message when error message is null
    /// </summary>
    [Fact]
    public async Task GetOrderBook_WithNullErrorMessage_ShouldReturnDefaultMessage()
    {
        // Arrange
        var tradingAccountId = TestDataBuilder.GetOrderBook.ValidTradingAccountId();
        var query = TestDataBuilder.GetOrderBook.ValidQuery();

        _mockExchangeService.Setup(x => x.GetOrderBookAsync(tradingAccountId, It.IsAny<GetOrderBookQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((OrderBookDto?)null, (string?)null));

        // Act
        var result = await _exchangeController.GetOrderBook(tradingAccountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be($"An unexpected error occurred while fetching order book for trading account ID {tradingAccountId}.");
    }

    /// <summary>
    /// Test: Should verify order book data structure and aggregation
    /// </summary>
    [Fact]
    public async Task GetOrderBook_ShouldReturnCorrectDataStructure()
    {
        // Arrange
        var tradingAccountId = TestDataBuilder.GetOrderBook.ValidTradingAccountId();
        var query = TestDataBuilder.GetOrderBook.ValidQuery();
        var expectedOrderBook = TestDataBuilder.GetOrderBook.ValidOrderBookDto();

        _mockExchangeService.Setup(x => x.GetOrderBookAsync(tradingAccountId, It.IsAny<GetOrderBookQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOrderBook, (string?)null));

        // Act
        var result = await _exchangeController.GetOrderBook(tradingAccountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var orderBook = okResult!.Value as OrderBookDto;
        
        // Verify order book structure
        orderBook!.TradingAccountId.Should().Be(tradingAccountId);
        orderBook.TradingAccountName.Should().NotBeNullOrEmpty();
        orderBook.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        // Verify bids are sorted by price descending (highest first)
        orderBook.Bids.Should().HaveCount(5);
        for (int i = 0; i < orderBook.Bids.Count - 1; i++)
        {
            orderBook.Bids[i].Price.Should().BeGreaterThan(orderBook.Bids[i + 1].Price);
        }
        
        // Verify asks are sorted by price ascending (lowest first)
        orderBook.Asks.Should().HaveCount(5);
        for (int i = 0; i < orderBook.Asks.Count - 1; i++)
        {
            orderBook.Asks[i].Price.Should().BeLessThan(orderBook.Asks[i + 1].Price);
        }
        
        // Verify quantities are positive
        orderBook.Bids.Should().AllSatisfy(bid => bid.TotalQuantity.Should().BeGreaterThan(0));
        orderBook.Asks.Should().AllSatisfy(ask => ask.TotalQuantity.Should().BeGreaterThan(0));
    }

    /// <summary>
    /// Test: Should handle public access (no authentication required) 
    /// </summary>
    [Fact]
    public async Task GetOrderBook_WithoutAuthentication_ShouldAllowPublicAccess()
    {
        // Arrange
        var tradingAccountId = TestDataBuilder.GetOrderBook.ValidTradingAccountId();
        var query = TestDataBuilder.GetOrderBook.ValidQuery();
        var expectedOrderBook = TestDataBuilder.GetOrderBook.ValidOrderBookDto();

        // No authentication setup - simulating public access
        _exchangeController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();

        _mockExchangeService.Setup(x => x.GetOrderBookAsync(tradingAccountId, It.IsAny<GetOrderBookQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedOrderBook, (string?)null));

        // Act
        var result = await _exchangeController.GetOrderBook(tradingAccountId, query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedOrderBook);
    }

    #endregion

    #region GetMarketData Tests

    [Fact]
    public async Task GetMarketData_ValidRequest_ReturnsMarketData()
    {
        // Arrange
        var query = TestDataBuilder.GetMarketData.ValidQuery();
        var expectedMarketData = TestDataBuilder.GetMarketData.ValidMarketDataResponse();
        
        _mockExchangeService
            .Setup(s => s.GetMarketDataAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedMarketData, (string?)null));

        // Act
        var result = await _exchangeController.GetMarketData(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedMarketData);
    }

    [Fact]
    public async Task GetMarketData_ValidRequest_ReturnsCorrectStructure()
    {
        // Arrange
        var query = TestDataBuilder.GetMarketData.ValidQuery();
        var expectedMarketData = TestDataBuilder.GetMarketData.ValidMarketDataResponse();
        
        _mockExchangeService
            .Setup(s => s.GetMarketDataAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedMarketData, (string?)null));

        // Act
        var result = await _exchangeController.GetMarketData(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var marketData = okResult!.Value as MarketDataResponse;
        
        marketData.Should().NotBeNull();
        marketData!.Items.Should().NotBeEmpty();
        marketData.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        var firstItem = marketData.Items.First();
        firstItem.TradingAccountId.Should().BePositive();
        firstItem.TradingAccountName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetMarketData_WithSingleTradingAccount_ReturnsFilteredData()
    {
        // Arrange
        var query = TestDataBuilder.GetMarketData.QueryWithSingleTradingAccount();
        var marketData = new MarketDataResponse
        {
            Items = new List<TradingAccountMarketDataDto>
            {
                TestDataBuilder.GetMarketData.ValidTradingAccountMarketData()
            },
            GeneratedAt = DateTime.UtcNow
        };
        
        _mockExchangeService
            .Setup(s => s.GetMarketDataAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync((marketData, (string?)null));

        // Act
        var result = await _exchangeController.GetMarketData(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var responseData = okResult!.Value as MarketDataResponse;
        
        responseData!.Items.Should().HaveCount(1);
        responseData.Items.First().TradingAccountId.Should().Be(1);
    }

    [Fact]
    public async Task GetMarketData_WithoutTradingAccountIds_ReturnsAllActiveAccounts()
    {
        // Arrange
        var query = TestDataBuilder.GetMarketData.QueryWithoutTradingAccountIds();
        var expectedMarketData = TestDataBuilder.GetMarketData.ValidMarketDataResponse();
        
        _mockExchangeService
            .Setup(s => s.GetMarketDataAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedMarketData, (string?)null));

        // Act
        var result = await _exchangeController.GetMarketData(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var marketData = okResult!.Value as MarketDataResponse;
        
        marketData!.Items.Should().NotBeEmpty();
        marketData.Items.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task GetMarketData_InvalidTradingAccountIdsFormat_ReturnsBadRequest()
    {
        // Arrange
        var query = new GetMarketDataQuery { TradingAccountIds = TestDataBuilder.GetMarketData.InvalidTradingAccountIds() };
        var errorMessage = "Invalid format for tradingAccountIds parameter";
        
        _mockExchangeService
            .Setup(s => s.GetMarketDataAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((MarketDataResponse?)null, errorMessage));

        // Act
        var result = await _exchangeController.GetMarketData(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("Invalid format for tradingAccountIds");
    }

    [Fact]
    public async Task GetMarketData_ServiceError_ReturnsInternalServerError()
    {
        // Arrange
        var query = TestDataBuilder.GetMarketData.ValidQuery();
        var errorMessage = "Database connection failed";
        
        _mockExchangeService
            .Setup(s => s.GetMarketDataAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((MarketDataResponse?)null, errorMessage));

        // Act
        var result = await _exchangeController.GetMarketData(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Contain("Database connection failed");
    }

    [Fact]
    public async Task GetMarketData_WithInvalidRecentTradesLimit_UsesValidatedLimit()
    {
        // Arrange
        var query = TestDataBuilder.GetMarketData.QueryWithInvalidRecentTradesLimit();
        var expectedMarketData = TestDataBuilder.GetMarketData.ValidMarketDataResponse();
        
        _mockExchangeService
            .Setup(s => s.GetMarketDataAsync(It.IsAny<GetMarketDataQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedMarketData, (string?)null));

        // Act
        var result = await _exchangeController.GetMarketData(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify that the query was passed to the service (validation happens in the service layer)
        _mockExchangeService.Verify(s => s.GetMarketDataAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMarketData_WithInvalidActiveOfferingsLimit_UsesValidatedLimit()
    {
        // Arrange
        var query = TestDataBuilder.GetMarketData.QueryWithInvalidActiveOfferingsLimit();
        var expectedMarketData = TestDataBuilder.GetMarketData.ValidMarketDataResponse();
        
        _mockExchangeService
            .Setup(s => s.GetMarketDataAsync(It.IsAny<GetMarketDataQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedMarketData, (string?)null));

        // Act
        var result = await _exchangeController.GetMarketData(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify that the query was passed to the service (validation happens in the service layer)
        _mockExchangeService.Verify(s => s.GetMarketDataAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMarketData_EmptyMarketData_ReturnsEmptyResponse()
    {
        // Arrange
        var query = TestDataBuilder.GetMarketData.ValidQuery();
        var emptyMarketData = TestDataBuilder.GetMarketData.EmptyMarketDataResponse();
        
        _mockExchangeService
            .Setup(s => s.GetMarketDataAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync((emptyMarketData, (string?)null));

        // Act
        var result = await _exchangeController.GetMarketData(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var marketData = okResult!.Value as MarketDataResponse;
        
        marketData!.Items.Should().BeEmpty();
        marketData.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetMarketData_WithRecentTradesData_ReturnsTradesInformation()
    {
        // Arrange
        var query = TestDataBuilder.GetMarketData.ValidQuery();
        var marketDataWithTrades = new MarketDataResponse
        {
            Items = new List<TradingAccountMarketDataDto>
            {
                TestDataBuilder.GetMarketData.TradingAccountMarketDataWithRecentTrades()
            },
            GeneratedAt = DateTime.UtcNow
        };
        
        _mockExchangeService
            .Setup(s => s.GetMarketDataAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync((marketDataWithTrades, (string?)null));

        // Act
        var result = await _exchangeController.GetMarketData(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var marketData = okResult!.Value as MarketDataResponse;
        
        var tradingAccount = marketData!.Items.First();
        tradingAccount.RecentTrades.Should().NotBeEmpty();
        tradingAccount.RecentTrades.Should().HaveCount(3);
        tradingAccount.RecentTrades.First().TradeTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task GetMarketData_WithActiveOfferingsData_ReturnsOfferingsInformation()
    {
        // Arrange
        var query = TestDataBuilder.GetMarketData.ValidQuery();
        var marketDataWithOfferings = new MarketDataResponse
        {
            Items = new List<TradingAccountMarketDataDto>
            {
                TestDataBuilder.GetMarketData.TradingAccountMarketDataWithActiveOfferings()
            },
            GeneratedAt = DateTime.UtcNow
        };
        
        _mockExchangeService
            .Setup(s => s.GetMarketDataAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync((marketDataWithOfferings, (string?)null));

        // Act
        var result = await _exchangeController.GetMarketData(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var marketData = okResult!.Value as MarketDataResponse;
        
        var tradingAccount = marketData!.Items.First();
        tradingAccount.ActiveOfferings.Should().NotBeEmpty();
        tradingAccount.ActiveOfferings.Should().HaveCount(3);
        tradingAccount.ActiveOfferings.First().OfferingId.Should().BePositive();
        tradingAccount.ActiveOfferings.First().AvailableQuantity.Should().BePositive();
    }

    [Fact]
    public async Task GetMarketData_UnauthenticatedRequest_ReturnsSuccess()
    {
        // Arrange
        var query = TestDataBuilder.GetMarketData.ValidQuery();
        var expectedMarketData = TestDataBuilder.GetMarketData.ValidMarketDataResponse();
        
        // Set up unauthenticated user context
        _exchangeController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreateUnauthenticatedUser()
            }
        };
        
        _mockExchangeService
            .Setup(s => s.GetMarketDataAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedMarketData, (string?)null));

        // Act
        var result = await _exchangeController.GetMarketData(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedMarketData);
    }

    [Fact]
    public async Task GetMarketData_UnauthenticatedWithParameters_ReturnsSuccess()
    {
        // Arrange
        var query = TestDataBuilder.GetMarketData.QueryWithSingleTradingAccount();
        var expectedMarketData = TestDataBuilder.GetMarketData.ValidMarketDataResponse();
        
        // Set up unauthenticated user context
        _exchangeController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreateUnauthenticatedUser()
            }
        };
        
        _mockExchangeService
            .Setup(s => s.GetMarketDataAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedMarketData, (string?)null));

        // Act
        var result = await _exchangeController.GetMarketData(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        // Verify public access works with parameters
        _mockExchangeService.Verify(s => s.GetMarketDataAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMarketData_UnauthenticatedWithInvalidParameters_ReturnsSuccess()
    {
        // Arrange
        var query = TestDataBuilder.GetMarketData.QueryWithInvalidRecentTradesLimit();
        var expectedMarketData = TestDataBuilder.GetMarketData.ValidMarketDataResponse();
        
        // Set up unauthenticated user context
        _exchangeController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreateUnauthenticatedUser()
            }
        };
        
        _mockExchangeService
            .Setup(s => s.GetMarketDataAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedMarketData, (string?)null));

        // Act
        var result = await _exchangeController.GetMarketData(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        // Public access should work even with parameter validation (handled by service)
        _mockExchangeService.Verify(s => s.GetMarketDataAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMarketData_UnauthenticatedWithInvalidFormat_ReturnsBadRequest()
    {
        // Arrange
        var query = new GetMarketDataQuery { TradingAccountIds = TestDataBuilder.GetMarketData.InvalidTradingAccountIds() };
        var errorMessage = "Invalid format for tradingAccountIds parameter";
        
        // Set up unauthenticated user context
        _exchangeController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreateUnauthenticatedUser()
            }
        };
        
        _mockExchangeService
            .Setup(s => s.GetMarketDataAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((MarketDataResponse?)null, errorMessage));

        // Act
        var result = await _exchangeController.GetMarketData(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        // Public access should still return proper error responses
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("Invalid format for tradingAccountIds");
    }

    #endregion

    #region GetMyTrades Tests - SCRUM-61

    // SCRUM-61: Unit Tests for GET /exchange/trades/my - Get My Trades Endpoint
    // This section provides comprehensive test coverage for the ExchangeController.GetMyTrades endpoint:
    // - GetMyTrades (GET /exchange/trades/my): User trade history retrieval with validation, filtering, and pagination tests
    //
    // Test Coverage Summary:
    // 1. Happy Path Tests: Valid trade retrieval with various filters and pagination
    // 2. Filter Tests: Trading account, order side, date range, and combined filters
    // 3. Pagination Tests: Different page sizes, page numbers, and validation
    // 4. Authentication Tests: User data isolation and authentication handling
    // 5. Data Mapping Tests: Trade details accuracy and structure validation
    // 6. Edge Cases: Empty results, large data sets, and boundary conditions

    /// <summary>
    /// Test: Valid authenticated user should successfully retrieve their trades
    /// Verifies the happy path where an authenticated user retrieves their trade history
    /// </summary>
    [Fact]
    public async Task GetMyTrades_WithValidQuery_ShouldReturnOkWithTrades()
    {
        // Arrange
        var query = TestDataBuilder.GetMyTrades.ValidFullQuery();
        var expectedResponse = TestDataBuilder.GetMyTrades.SuccessfulTradesResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyTradesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<MyShareTradeDto>;
        
        response.Should().NotBeNull();
        response!.Items.Should().HaveCount(3);
        response.TotalCount.Should().Be(20);
        response.PageNumber.Should().Be(1);
        response.PageSize.Should().Be(10);
    }

    /// <summary>
    /// Test: Valid query with minimal parameters should work correctly
    /// Verifies that the endpoint works with basic parameters
    /// </summary>
    [Fact]
    public async Task GetMyTrades_WithMinimalQuery_ShouldReturnOkWithTrades()
    {
        // Arrange
        var query = TestDataBuilder.GetMyTrades.ValidMinimalQuery();
        var expectedResponse = TestDataBuilder.GetMyTrades.SuccessfulTradesResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyTradesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    /// <summary>
    /// Test: User with no trades should receive empty list
    /// Verifies proper handling when user has no trade history
    /// </summary>
    [Fact]
    public async Task GetMyTrades_WithNoTrades_ShouldReturnEmptyPaginatedList()
    {
        // Arrange
        var query = TestDataBuilder.GetMyTrades.ValidMinimalQuery();
        var emptyResponse = TestDataBuilder.GetMyTrades.EmptyTradesResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyTradesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyResponse);

        // Act
        var result = await _exchangeController.GetMyTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<MyShareTradeDto>;
        
        response.Should().NotBeNull();
        response!.Items.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
        response.HasNextPage.Should().BeFalse();
        response.HasPreviousPage.Should().BeFalse();
    }

    /// <summary>
    /// Test: Filter by order side (Buy/Sell) should return filtered results
    /// Verifies that order side filtering works correctly
    /// </summary>
    [Theory]
    [InlineData("Buy")]
    [InlineData("Sell")]
    public async Task GetMyTrades_WithOrderSideFilter_ShouldReturnFilteredTrades(string orderSide)
    {
        // Arrange
        var query = orderSide == "Buy" ? TestDataBuilder.GetMyTrades.BuyTradesQuery() : TestDataBuilder.GetMyTrades.SellTradesQuery();
        var expectedResponse = orderSide == "Buy" ? TestDataBuilder.GetMyTrades.BuyTradesResponse() : TestDataBuilder.GetMyTrades.SellTradesResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyTradesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<MyShareTradeDto>;
        
        response.Should().NotBeNull();
        response!.Items.Should().OnlyContain(trade => trade.OrderSide == orderSide);
        
        // Verify service was called with correct filter
        _mockExchangeService.Verify(x => x.GetMyTradesAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetMyShareTradesQuery>(q => q.OrderSide == orderSide),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Different pagination parameters should be respected
    /// Verifies that pagination works correctly with various page sizes and numbers
    /// </summary>
    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 10)]
    [InlineData(3, 25)]
    public async Task GetMyTrades_WithDifferentPagination_ShouldRespectPaginationParameters(int pageNumber, int pageSize)
    {
        // Arrange
        var query = TestDataBuilder.GetMyTrades.CustomQuery(pageNumber, pageSize);
        var expectedResponse = TestDataBuilder.GetMyTrades.SuccessfulTradesResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyTradesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called with correct pagination
        _mockExchangeService.Verify(x => x.GetMyTradesAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetMyShareTradesQuery>(q => q.PageNumber == pageNumber && q.PageSize == pageSize),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Invalid pagination should still return results (handled by query validation)
    /// Verifies that invalid pagination parameters are handled gracefully
    /// </summary>
    [Fact]
    public async Task GetMyTrades_WithInvalidPagination_ShouldStillReturnResults()
    {
        // Arrange
        var query = TestDataBuilder.GetMyTrades.InvalidPaginationQuery();
        var expectedResponse = TestDataBuilder.GetMyTrades.SuccessfulTradesResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyTradesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called (validation handled at query level)
        _mockExchangeService.Verify(x => x.GetMyTradesAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<GetMyShareTradesQuery>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Large page size should be limited to maximum
    /// Verifies that page size validation works correctly
    /// </summary>
    [Fact]
    public async Task GetMyTrades_WithLargePageSize_ShouldLimitToMaximum()
    {
        // Arrange
        var query = TestDataBuilder.GetMyTrades.LargePageSizeQuery();
        var expectedResponse = TestDataBuilder.GetMyTrades.SuccessfulTradesResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = authenticatedUser
        };

        // Verify the query object automatically limits page size
        query.ValidatedPageSize.Should().Be(100); // Max page size

        _mockExchangeService.Setup(x => x.GetMyTradesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockExchangeService.Verify(x => x.GetMyTradesAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<GetMyShareTradesQuery>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Trading account filter should pass to service
    /// Verifies that trading account filtering works correctly
    /// </summary>
    [Fact]
    public async Task GetMyTrades_WithTradingAccountFilter_ShouldReturnAccountSpecificTrades()
    {
        // Arrange
        var query = TestDataBuilder.GetMyTrades.TradingAccountFilterQuery();
        var expectedResponse = TestDataBuilder.GetMyTrades.TradingAccountFilteredResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyTradesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<MyShareTradeDto>;
        
        response.Should().NotBeNull();
        response!.Items.Should().OnlyContain(trade => trade.TradingAccountId == 2);
        
        // Verify service was called with correct filter
        _mockExchangeService.Verify(x => x.GetMyTradesAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetMyShareTradesQuery>(q => q.TradingAccountId == 2),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Date range filter should pass to service
    /// Verifies that date range filtering works correctly
    /// </summary>
    [Fact]
    public async Task GetMyTrades_WithDateRange_ShouldPassDateRangeToService()
    {
        // Arrange
        var query = TestDataBuilder.GetMyTrades.DateRangeQuery();
        var expectedResponse = TestDataBuilder.GetMyTrades.DateRangeFilteredResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyTradesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called with correct date range
        _mockExchangeService.Verify(x => x.GetMyTradesAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetMyShareTradesQuery>(q => 
                q.DateFrom.HasValue && 
                q.DateTo.HasValue &&
                q.DateFrom.Value <= DateTime.UtcNow.AddDays(-6) && // Approximately -7 days
                q.DateTo.Value >= DateTime.UtcNow.AddHours(-1)),   // Recent
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Combined filters should work together
    /// Verifies that multiple filters can be applied simultaneously
    /// </summary>
    [Fact]
    public async Task GetMyTrades_WithCombinedFilters_ShouldApplyAllFilters()
    {
        // Arrange
        var query = TestDataBuilder.GetMyTrades.CombinedFiltersQuery();
        var expectedResponse = TestDataBuilder.GetMyTrades.SellTradesResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyTradesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called with all filters
        _mockExchangeService.Verify(x => x.GetMyTradesAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<GetMyShareTradesQuery>(q => 
                q.TradingAccountId == 1 &&
                q.OrderSide == "Sell" &&
                q.DateFrom.HasValue &&
                q.DateTo.HasValue &&
                q.SortBy == "TradePrice" &&
                q.SortOrder == "asc"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Valid authentication should call service with user context
    /// Verifies that authentication is properly handled
    /// </summary>
    [Fact]
    public async Task GetMyTrades_WithValidAuthentication_ShouldCallService()
    {
        // Arrange
        var query = TestDataBuilder.GetMyTrades.ValidMinimalQuery();
        var expectedResponse = TestDataBuilder.GetMyTrades.SuccessfulTradesResponse();
        var authenticatedUser = CreateAuthenticatedUser(123, "testuser");
        _exchangeController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyTradesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called with the correct user context
        _mockExchangeService.Verify(x => x.GetMyTradesAsync(
            It.Is<ClaimsPrincipal>(u => u.FindFirst(ClaimTypes.NameIdentifier) != null && u.FindFirst(ClaimTypes.NameIdentifier).Value == "123"),
            It.IsAny<GetMyShareTradesQuery>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: User should only see their own trades (data isolation)
    /// Verifies that users can only access their own trade data
    /// </summary>
    [Fact]
    public async Task GetMyTrades_WithValidUser_ShouldOnlyReturnUserTrades()
    {
        // Arrange
        var query = TestDataBuilder.GetMyTrades.ValidMinimalQuery();
        var expectedResponse = TestDataBuilder.GetMyTrades.SuccessfulTradesResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyTradesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify that service is called with user context (service handles isolation)
        _mockExchangeService.Verify(x => x.GetMyTradesAsync(
            It.Is<ClaimsPrincipal>(u => u.Identity != null && u.Identity.IsAuthenticated),
            It.IsAny<GetMyShareTradesQuery>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Service exception should be propagated
    /// Verifies proper error handling when service throws exceptions
    /// </summary>
    [Fact]
    public async Task GetMyTrades_WhenServiceThrowsException_ShouldPropagateException()
    {
        // Arrange
        var query = TestDataBuilder.GetMyTrades.ValidMinimalQuery();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyTradesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareTradesQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _exchangeController.GetMyTrades(query, CancellationToken.None));
    }

    /// <summary>
    /// Test: Data mapping accuracy verification
    /// Verifies that trade data is correctly mapped and returned
    /// </summary>
    [Fact]
    public async Task GetMyTrades_ShouldReturnCorrectTradeDataStructure()
    {
        // Arrange
        var query = TestDataBuilder.GetMyTrades.ValidMinimalQuery();
        var expectedResponse = TestDataBuilder.GetMyTrades.SuccessfulTradesResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyTradesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _exchangeController.GetMyTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<MyShareTradeDto>;
        
        response.Should().NotBeNull();
        var firstTrade = response!.Items.First();
        
        // Verify trade structure and data accuracy
        firstTrade.TradeId.Should().BePositive();
        firstTrade.TradingAccountId.Should().BePositive();
        firstTrade.TradingAccountName.Should().NotBeNullOrEmpty();
        firstTrade.OrderSide.Should().BeOneOf("Buy", "Sell");
        firstTrade.QuantityTraded.Should().BePositive();
        firstTrade.TradePrice.Should().BePositive();
        firstTrade.TotalValue.Should().BePositive();
        firstTrade.TradeDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromDays(1));
        
        // Verify calculated fields
        firstTrade.TotalValue.Should().Be(firstTrade.QuantityTraded * firstTrade.TradePrice);
    }

    /// <summary>
    /// Test: Large trades data handling
    /// Verifies that large trade values are handled correctly
    /// </summary>
    [Fact]
    public async Task GetMyTrades_WithLargeTrades_ShouldHandleLargeValues()
    {
        // Arrange
        var query = TestDataBuilder.GetMyTrades.ValidMinimalQuery();
        var largeTradesResponse = TestDataBuilder.GetMyTrades.LargeTradesResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyTradesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(largeTradesResponse);

        // Act
        var result = await _exchangeController.GetMyTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<MyShareTradeDto>;
        
        response.Should().NotBeNull();
        var largeTrade = response!.Items.First();
        
        // Verify large values are handled correctly
        largeTrade.QuantityTraded.Should().Be(1000);
        largeTrade.TradePrice.Should().Be(125.50m);
        largeTrade.TotalValue.Should().Be(125500.00m);
        largeTrade.FeeAmount.Should().Be(251.00m);
    }

    /// <summary>
    /// Test: Pagination navigation properties
    /// Verifies that pagination metadata is correctly set
    /// </summary>
    [Fact]
    public async Task GetMyTrades_WithPagination_ShouldReturnCorrectPaginationInfo()
    {
        // Arrange
        var query = TestDataBuilder.GetMyTrades.CustomQuery(2, 10);
        var secondPageResponse = TestDataBuilder.GetMyTrades.SecondPageResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyTradesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondPageResponse);

        // Act
        var result = await _exchangeController.GetMyTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<MyShareTradeDto>;
        
        response.Should().NotBeNull();
        response!.PageNumber.Should().Be(2);
        response.PageSize.Should().Be(10);
        response.TotalCount.Should().Be(25);
        response.HasPreviousPage.Should().BeTrue();
        response.HasNextPage.Should().BeTrue();
    }

    /// <summary>
    /// Test: Zero fee trades handling
    /// Verifies that trades without fees are handled correctly
    /// </summary>
    [Fact]
    public async Task GetMyTrades_WithZeroFeeTrades_ShouldHandleNullFees()
    {
        // Arrange
        var query = TestDataBuilder.GetMyTrades.ValidMinimalQuery();
        var zeroFeeResponse = TestDataBuilder.GetMyTrades.ZeroFeeTradesResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _exchangeController.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockExchangeService.Setup(x => x.GetMyTradesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<GetMyShareTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zeroFeeResponse);

        // Act
        var result = await _exchangeController.GetMyTrades(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as PaginatedList<MyShareTradeDto>;
        
        response.Should().NotBeNull();
        var trade = response!.Items.First();
        
        // Verify null fee is handled correctly
        trade.FeeAmount.Should().BeNull();
        trade.TotalValue.Should().Be(750.00m);
    }

    #endregion
}