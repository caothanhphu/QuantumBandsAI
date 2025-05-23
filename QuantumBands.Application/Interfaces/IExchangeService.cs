// QuantumBands.Application/Interfaces/IExchangeService.cs
using QuantumBands.Application.Features.Exchange.Commands.CreateOrder;
using QuantumBands.Application.Features.Exchange.Dtos;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;

namespace QuantumBands.Application.Interfaces;

public interface IExchangeService
{
    Task<(ShareOrderDto? Order, string? ErrorMessage)> PlaceOrderAsync(CreateShareOrderRequest request, ClaimsPrincipal currentUser, CancellationToken cancellationToken = default);
    // ... các phương thức khác của IExchangeService (GetMyOrders, CancelOrder, GetOrderBook, GetMyTrades) ...
    Task<bool> TryMatchOrderAsync(long orderId, CancellationToken cancellationToken = default);

}