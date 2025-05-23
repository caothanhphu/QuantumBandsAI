// QuantumBands.Application/Services/SharePortfolioService.cs
using QuantumBands.Application.Features.Portfolio.Dtos;
using QuantumBands.Application.Interfaces;
using QuantumBands.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore; // For Include, FirstOrDefaultAsync
using System;
using System.IdentityModel.Tokens.Jwt;

namespace QuantumBands.Application.Services;

public class SharePortfolioService : ISharePortfolioService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SharePortfolioService> _logger;

    public SharePortfolioService(IUnitOfWork unitOfWork, ILogger<SharePortfolioService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    private int? GetUserIdFromPrincipal(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }
        var userIdString = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "uid" || c.Type == JwtRegisteredClaimNames.Sub)?.Value;

        if (int.TryParse(userIdString, out var userId))
        {
            return userId;
        }
        return null;
    }

    // BE-PORTFOLIO-001
    public async Task<(List<SharePortfolioItemDto>? PortfolioItems, string? ErrorMessage)> GetMyPortfolioAsync(ClaimsPrincipal currentUser, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromPrincipal(currentUser);
        if (!userId.HasValue)
        {
            _logger.LogWarning("GetMyPortfolioAsync: User is not authenticated or UserId claim is missing.");
            return (null, "User not authenticated or identity is invalid.");
        }

        _logger.LogInformation("Fetching portfolio for UserID: {UserId}", userId.Value);

        var portfolioItems = await _unitOfWork.SharePortfolios.Query()
            .Include(sp => sp.TradingAccount) // Nạp thông tin TradingAccount để lấy tên và giá hiện tại
            .Where(sp => sp.UserId == userId.Value && sp.Quantity > 0) // Chỉ lấy các mục có số lượng > 0
            .ToListAsync(cancellationToken);

        if (portfolioItems == null) // Sẽ là list rỗng nếu không có, không phải null
        {
            // Trường hợp này hiếm khi xảy ra với ToListAsync, nó sẽ trả về list rỗng.
            _logger.LogInformation("No portfolio items found for UserID: {UserId}", userId.Value);
            return (new List<SharePortfolioItemDto>(), null);
        }

        var dtos = portfolioItems.Select(sp =>
        {
            decimal currentSharePrice = sp.TradingAccount?.CurrentSharePrice ?? 0; // Lấy giá hiện tại từ TradingAccount
                                                                                   // Nếu TradingAccount.CurrentSharePrice là computed column trong DB, nó sẽ được nạp
                                                                                   // Nếu không, bạn cần đảm bảo TradingAccount entity có trường này và nó được cập nhật
            decimal currentValue = sp.Quantity * currentSharePrice;
            decimal unrealizedPAndL = (currentSharePrice - sp.AverageBuyPrice) * sp.Quantity;

            return new SharePortfolioItemDto
            {
                PortfolioId = sp.PortfolioId,
                TradingAccountId = sp.TradingAccountId,
                TradingAccountName = sp.TradingAccount?.AccountName ?? "N/A",
                Quantity = sp.Quantity,
                AverageBuyPrice = sp.AverageBuyPrice,
                CurrentSharePrice = currentSharePrice,
                CurrentValue = currentValue,
                UnrealizedPAndL = unrealizedPAndL,
                LastUpdatedAt = sp.LastUpdatedAt
            };
        }).ToList();

        return (dtos, null);
    }

    // BE-PORTFOLIO-002 (Internal Logic)
    public async Task<(bool Success, string? ErrorMessage)> UpdatePortfolioOnBuyAsync(
        int userId, int tradingAccountId, long boughtQuantity, decimal buyPrice, CancellationToken cancellationToken = default)
    {
        if (boughtQuantity <= 0) return (false, "Bought quantity must be positive.");
        if (buyPrice <= 0) return (false, "Buy price must be positive.");

        _logger.LogInformation("Updating portfolio for UserID {UserId}, TradingAccountID {TradingAccountId} after buying {BoughtQuantity} shares at {BuyPrice}.",
                               userId, tradingAccountId, boughtQuantity, buyPrice);

        var portfolioItem = await _unitOfWork.SharePortfolios.Query()
                                .FirstOrDefaultAsync(sp => sp.UserId == userId && sp.TradingAccountId == tradingAccountId, cancellationToken);

        if (portfolioItem != null) // User đã có cổ phần của quỹ này
        {
            decimal totalCostBefore = portfolioItem.Quantity * portfolioItem.AverageBuyPrice;
            decimal newTotalCost = totalCostBefore + (boughtQuantity * buyPrice);
            long newTotalQuantity = portfolioItem.Quantity + boughtQuantity;

            portfolioItem.AverageBuyPrice = newTotalQuantity > 0 ? newTotalCost / newTotalQuantity : 0;
            portfolioItem.Quantity = newTotalQuantity;
            portfolioItem.LastUpdatedAt = DateTime.UtcNow;
            _unitOfWork.SharePortfolios.Update(portfolioItem);
        }
        else // User chưa có cổ phần của quỹ này, tạo mới
        {
            portfolioItem = new SharePortfolio
            {
                UserId = userId,
                TradingAccountId = tradingAccountId,
                Quantity = boughtQuantity,
                AverageBuyPrice = buyPrice,
                LastUpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.SharePortfolios.AddAsync(portfolioItem);
        }

        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Portfolio updated successfully for UserID {UserId}, TradingAccountID {TradingAccountId} after buy.", userId, tradingAccountId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating portfolio for UserID {UserId}, TradingAccountID {TradingAccountId} after buy.", userId, tradingAccountId);
            return (false, "Failed to update portfolio after buy transaction.");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdatePortfolioOnSellAsync(
        int userId, int tradingAccountId, long soldQuantity, decimal sellPrice, CancellationToken cancellationToken = default)
    {
        // sellPrice có thể dùng để tính Realized P&L và lưu vào một bảng khác nếu cần
        if (soldQuantity <= 0) return (false, "Sold quantity must be positive.");

        _logger.LogInformation("Updating portfolio for UserID {UserId}, TradingAccountID {TradingAccountId} after selling {SoldQuantity} shares at {SellPrice}.",
                               userId, tradingAccountId, soldQuantity, sellPrice);

        var portfolioItem = await _unitOfWork.SharePortfolios.Query()
                                .FirstOrDefaultAsync(sp => sp.UserId == userId && sp.TradingAccountId == tradingAccountId, cancellationToken);

        if (portfolioItem == null || portfolioItem.Quantity < soldQuantity)
        {
            _logger.LogWarning("Attempted to sell more shares than owned or no shares owned. UserID {UserId}, TradingAccountID {TradingAccountId}, Has: {OwnedQty}, Sells: {SoldQty}",
                               userId, tradingAccountId, portfolioItem?.Quantity ?? 0, soldQuantity);
            return (false, "Cannot sell more shares than owned or portfolio item not found.");
        }

        // Tính Realized P&L cho số lượng bán (nếu cần lưu trữ)
        // decimal realizedPnlForThisSale = (sellPrice - portfolioItem.AverageBuyPrice) * soldQuantity;
        // _logger.LogInformation("Realized P&L for this sale: {RealizedPnlForThisSale}", realizedPnlForThisSale);
        // Logic lưu Realized P&L sẽ ở một service khác hoặc bảng khác.

        portfolioItem.Quantity -= soldQuantity;
        portfolioItem.LastUpdatedAt = DateTime.UtcNow;

        if (portfolioItem.Quantity == 0)
        {
            // Tùy chọn: Xóa bản ghi portfolio nếu số lượng về 0, hoặc giữ lại.
            // Để đơn giản, chúng ta giữ lại với Quantity = 0.
            // Nếu muốn xóa: _unitOfWork.SharePortfolios.Remove(portfolioItem);
            _logger.LogInformation("UserID {UserId} sold all shares for TradingAccountID {TradingAccountId}. Quantity is now 0.", userId, tradingAccountId);
        }
        _unitOfWork.SharePortfolios.Update(portfolioItem);

        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Portfolio updated successfully for UserID {UserId}, TradingAccountID {TradingAccountId} after sell.", userId, tradingAccountId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating portfolio for UserID {UserId}, TradingAccountID {TradingAccountId} after sell.", userId, tradingAccountId);
            return (false, "Failed to update portfolio after sell transaction.");
        }
    }
}