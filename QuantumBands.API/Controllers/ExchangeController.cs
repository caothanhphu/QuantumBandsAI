// QuantumBands.API/Controllers/ExchangeController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuantumBands.Application.Features.Exchange.Commands.CreateOrder;
using QuantumBands.Application.Features.Exchange.Dtos;
using QuantumBands.Application.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;

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

    // ... (Các actions khác: GetMyOrders, CancelOrder, GetOrderBook, GetMyTrades, GetMarketData)
}