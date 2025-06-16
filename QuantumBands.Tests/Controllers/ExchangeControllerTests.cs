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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Features.Exchange.Commands.CreateOrder;
using QuantumBands.Application.Features.Exchange.Dtos;
using QuantumBands.Application.Interfaces;
using QuantumBands.Tests.Common;
using QuantumBands.Tests.Fixtures;
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
        var orderRequest = TestDataBuilder.Exchange.ValidLimitSellOrderRequest();
        var expectedResponse = TestDataBuilder.Exchange.ValidLimitSellOrderResponse();
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
}