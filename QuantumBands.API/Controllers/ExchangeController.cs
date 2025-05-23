// QuantumBands.API/Controllers/ExchangeController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Exchange.Commands.CreateOrder;
using QuantumBands.Application.Features.Exchange.Dtos;
using QuantumBands.Application.Features.Exchange.Queries;
using QuantumBands.Application.Interfaces;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.API.Controllers;

[Authorize] // Yêu cầu xác thực cho tất cả actions
[ApiController]
[Route("api/v1/[controller]")] // Route: /api/v1/exchange
public class ExchangeController : ControllerBase
{
    private readonly IExchangeService _exchangeService;
    private readonly ILogger<ExchangeController> _logger;

    public ExchangeController(IExchangeService exchangeService, ILogger<ExchangeController> logger)
    {
        _exchangeService = exchangeService;
        _logger = logger;
    }

    [HttpPost("orders")] // Gộp endpoint: POST /api/v1/exchange/orders
    [ProducesResponseType(typeof(ShareOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PlaceOrder([FromBody] CreateShareOrderRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("User {UserId} attempting to place a {OrderSide} order.", userId, request.OrderSide);

        var (orderDto, errorMessage) = await _exchangeService.PlaceOrderAsync(request, User, cancellationToken);

        if (orderDto == null)
        {
            _logger.LogWarning("Failed to place order for User {UserId}. Error: {ErrorMessage}", userId, errorMessage);
            if (errorMessage != null)
            {
                if (errorMessage.Contains("not found")) return NotFound(new { Message = errorMessage });
                if (errorMessage.Contains("Insufficient") || errorMessage.Contains("Invalid")) return BadRequest(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? "Failed to place order." });
        }

        // Trả về 201 Created với thông tin lệnh và Location header (nếu có endpoint GetOrderById)
        // return CreatedAtAction("GetOrderById", new { orderId = orderDto.OrderId }, orderDto);
        // Nếu chưa có GetOrderById, dùng:
        return StatusCode(StatusCodes.Status201Created, orderDto);
    }

    /// <summary>
    /// Gets the current authenticated user's share orders with pagination and filtering.
    /// </summary>
    [HttpGet("orders/my")] // Endpoint: /api/v1/exchange/orders/my
    [ProducesResponseType(typeof(PaginatedList<ShareOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] // For invalid query parameters
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrders([FromQuery] GetMyShareOrdersQuery query, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("User {UserId} requesting their share orders with query: {@Query}", userId, query);

        // (Tùy chọn) Validate query DTO ở đây nếu không dùng auto-validation của FluentValidation cho [FromQuery]

        var result = await _exchangeService.GetMyOrdersAsync(User, query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cancels an open or partially filled share order belonging to the authenticated user.
    /// </summary>
    /// <param name="orderId">The ID of the share order to cancel.</param>
    /// <param name="cancellationToken"></param>
    [HttpDelete("orders/{orderId}")] // Endpoint: DELETE /api/v1/exchange/orders/{orderId}
    [ProducesResponseType(StatusCodes.Status204NoContent)] // Hoặc Status200OK với ShareOrderDto
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelOrder(long orderId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("User {UserId} attempting to cancel order with ID: {OrderId}", userId, orderId);

        if (orderId <= 0)
        {
            return BadRequest(new { Message = "Invalid Order ID." });
        }

        var (cancelledOrderDto, errorMessage) = await _exchangeService.CancelOrderAsync(orderId, User, cancellationToken);

        if (cancelledOrderDto == null)
        {
            _logger.LogWarning("Failed to cancel order {OrderId} for User {UserId}. Error: {ErrorMessage}", orderId, userId, errorMessage);
            if (errorMessage != null)
            {
                if (errorMessage.Contains("not found")) return NotFound(new { Message = errorMessage });
                if (errorMessage.Contains("not authorized")) return Forbid(); // Hoặc Unauthorized tùy ngữ cảnh
                if (errorMessage.Contains("cannot be cancelled") || errorMessage.Contains("Invalid")) return BadRequest(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? "Failed to cancel order." });
        }

        _logger.LogInformation("Order {OrderId} successfully cancelled by User {UserId}.", orderId, userId);
        // Lựa chọn 1: Trả về 204 No Content (phổ biến cho DELETE thành công)
        return NoContent();

        // Lựa chọn 2: Trả về 200 OK với thông tin lệnh đã hủy
        // return Ok(cancelledOrderDto);
    }
    /// <summary>
    /// Gets the order book for a specific trading account.
    /// </summary>
    /// <param name="tradingAccountId">The ID of the trading account.</param>
    /// <param name="query">Query parameters for depth.</param>
    /// <param name="cancellationToken"></param>
    [AllowAnonymous] // Cho phép truy cập công khai, hoặc bỏ đi nếu yêu cầu xác thực
    [HttpGet("order-book/{tradingAccountId}")] // Endpoint: /api/v1/exchange/order-book/{tradingAccountId}
    [ProducesResponseType(typeof(OrderBookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] // For invalid query parameters
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderBook(int tradingAccountId, [FromQuery] GetOrderBookQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request received for order book. TradingAccountID: {TradingAccountId}, Query: {@Query}", tradingAccountId, query);

        if (tradingAccountId <= 0)
        {
            return BadRequest(new { Message = "Invalid Trading Account ID." });
        }

        var (orderBook, errorMessage) = await _exchangeService.GetOrderBookAsync(tradingAccountId, query, cancellationToken);

        if (orderBook == null)
        {
            _logger.LogWarning("Order book not found or error retrieving for TradingAccountID {TradingAccountId}. Error: {ErrorMessage}", tradingAccountId, errorMessage);
            if (errorMessage != null && errorMessage.Contains("not found"))
            {
                return NotFound(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? $"An unexpected error occurred while fetching order book for trading account ID {tradingAccountId}." });
        }
        return Ok(orderBook);
    }
    /// <summary>
    /// Gets aggregated market data for specified (or all active) trading accounts,
    /// including best bids/asks and recent trades.
    /// </summary>
    [AllowAnonymous] // Cho phép truy cập công khai, hoặc bỏ đi nếu yêu cầu xác thực
    [HttpGet("market-data")] // Endpoint: /api/v1/exchange/market-data
    [ProducesResponseType(typeof(MarketDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] // For invalid query parameters
    public async Task<IActionResult> GetMarketData([FromQuery] GetMarketDataQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request received for market data with query: {@Query}", query);

        var (marketData, errorMessage) = await _exchangeService.GetMarketDataAsync(query, cancellationToken);

        if (marketData == null)
        {
            _logger.LogWarning("Failed to retrieve market data. Error: {ErrorMessage}", errorMessage);
            if (errorMessage != null && errorMessage.Contains("Invalid format for tradingAccountIds"))
            {
                return BadRequest(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? "An unexpected error occurred while fetching market data." });
        }
        return Ok(marketData);
    }

    /// <summary>
    /// Gets the current authenticated user's history of executed share trades.
    /// Supports pagination and filtering.
    /// </summary>
    [HttpGet("trades/my")] // Endpoint: /api/v1/exchange/trades/my
    [ProducesResponseType(typeof(PaginatedList<MyShareTradeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] // For invalid query parameters
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyTrades([FromQuery] GetMyShareTradesQuery query, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("User {UserId} requesting their share trades history with query: {@Query}", userId, query);

        var result = await _exchangeService.GetMyTradesAsync(User, query, cancellationToken);
        return Ok(result);
    }
    // ... (Các actions khác: GetMyOrders, CancelOrder, GetOrderBook, GetMyTrades, GetMarketData)
}