// QuantumBands.Application/Interfaces/IExchangeService.cs
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.ExchangeMonitor.Dtos;
using QuantumBands.Application.Features.Admin.ExchangeMonitor.Queries;
using QuantumBands.Application.Features.Exchange.Commands.CreateOrder;
using QuantumBands.Application.Features.Exchange.Dtos;
using QuantumBands.Application.Features.Exchange.Queries;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.Application.Interfaces;

public interface IExchangeService
{
    Task<(ShareOrderDto? Order, string? ErrorMessage)> PlaceOrderAsync(CreateShareOrderRequest request, ClaimsPrincipal currentUser, CancellationToken cancellationToken = default);
    // ... các phương thức khác của IExchangeService (GetMyOrders, CancelOrder, GetOrderBook, GetMyTrades) ...
    Task<bool> TryMatchOrderAsync(long orderId, CancellationToken cancellationToken = default);
    Task<PaginatedList<ShareOrderDto>> GetMyOrdersAsync(ClaimsPrincipal currentUser, GetMyShareOrdersQuery query, CancellationToken cancellationToken = default);
    Task<(ShareOrderDto? CancelledOrder, string? ErrorMessage)> CancelOrderAsync(long orderId, ClaimsPrincipal currentUser, CancellationToken cancellationToken = default);
    Task<(OrderBookDto? OrderBook, string? ErrorMessage)> GetOrderBookAsync(int tradingAccountId, GetOrderBookQuery query, CancellationToken cancellationToken = default);
    Task<(MarketDataResponse? Data, string? ErrorMessage)> GetMarketDataAsync(GetMarketDataQuery query, CancellationToken cancellationToken = default);
    Task<PaginatedList<MyShareTradeDto>> GetMyTradesAsync(ClaimsPrincipal currentUser, GetMyShareTradesQuery query, CancellationToken cancellationToken = default);
    Task<PaginatedList<AdminShareOrderViewDto>> GetAdminAllOrdersAsync(GetAdminAllOrdersQuery query, CancellationToken cancellationToken = default);
    Task<PaginatedList<AdminShareTradeViewDto>> GetAdminAllTradesAsync(GetAdminAllTradesQuery query, CancellationToken cancellationToken = default);


}