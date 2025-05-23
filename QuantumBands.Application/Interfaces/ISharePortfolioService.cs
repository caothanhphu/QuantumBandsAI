// QuantumBands.Application/Interfaces/ISharePortfolioService.cs
using QuantumBands.Application.Features.Portfolio.Dtos;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic; // For List

namespace QuantumBands.Application.Interfaces;

public interface ISharePortfolioService
{
    // BE-PORTFOLIO-001
    Task<(List<SharePortfolioItemDto>? PortfolioItems, string? ErrorMessage)> GetMyPortfolioAsync(ClaimsPrincipal currentUser, CancellationToken cancellationToken = default);

    // BE-PORTFOLIO-002 (Internal Logic - được gọi bởi các service khác, ví dụ ExchangeService)
    Task<(bool Success, string? ErrorMessage)> UpdatePortfolioOnBuyAsync(int userId, int tradingAccountId, long boughtQuantity, decimal buyPrice, CancellationToken cancellationToken = default);
    Task<(bool Success, string? ErrorMessage)> UpdatePortfolioOnSellAsync(int userId, int tradingAccountId, long soldQuantity, decimal sellPrice, CancellationToken cancellationToken = default); // sellPrice có thể dùng để tính realized P&L nếu cần
    Task<(bool Success, string? ErrorMessage)> ReleaseHeldSharesAsync(
        int userId,
        int tradingAccountId,
        long quantityToRelease,
        string reason, // Ví dụ: "Order Cancelled"
        CancellationToken cancellationToken = default);

}