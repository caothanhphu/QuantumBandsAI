// QuantumBands.Application/Services/AdminDashboardService.cs
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.Dashboard.Dtos;
using QuantumBands.Application.Interfaces;
using QuantumBands.Domain.Entities.Enums; // For ShareOrderStatusName
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace QuantumBands.Application.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminDashboardService> _logger;

    public AdminDashboardService(IUnitOfWork unitOfWork, ILogger<AdminDashboardService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<(AdminDashboardSummaryDto? Summary, string? ErrorMessage)> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching admin dashboard summary data.");
        try
        {
            var summary = new AdminDashboardSummaryDto();
            var thirtyDaysAgo = DateTime.UtcNow.Date.AddDays(-30);

            summary.TotalUsers = await _unitOfWork.Users.Query().LongCountAsync(cancellationToken);
            summary.TotalActiveFunds = await _unitOfWork.TradingAccounts.Query().CountAsync(ta => ta.IsActive, cancellationToken);
            summary.TotalPlatformNAV = await _unitOfWork.TradingAccounts.Query().Where(ta => ta.IsActive).SumAsync(ta => ta.CurrentNetAssetValue, cancellationToken);

            summary.PendingDepositsCount = await _unitOfWork.WalletTransactions.Query()
                .CountAsync(t => t.Status == "PendingBankTransfer" || t.Status == "PendingAdminConfirmation", cancellationToken);

            summary.PendingWithdrawalsCount = await _unitOfWork.WalletTransactions.Query()
                .CountAsync(t => t.Status == "PendingAdminApproval", cancellationToken);

            summary.RecentTrades = await _unitOfWork.ShareTrades.Query()
                .Include(st => st.TradingAccount)
                .OrderByDescending(st => st.TradeDate)
                .Take(5)
                .Select(st => new SimpleTradeInfoDto
                {
                    TradeId = st.TradeId,
                    TradingAccountName = st.TradingAccount.AccountName,
                    TradeTime = st.TradeDate,
                    QuantityTraded = st.QuantityTraded,
                    TradePrice = st.TradePrice
                })
                .ToListAsync(cancellationToken);

            // For UserGrowthData
            var userGrowthRawData = await _unitOfWork.Users.Query()
                .Where(u => u.CreatedAt >= thirtyDaysAgo)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.LongCount() })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);

            summary.UserGrowthData = userGrowthRawData
                .Select(g => new ChartDataPoint<long> { 
                    Date = g.Date.ToString("yyyy-MM-dd"), 
                    Value = g.Count 
                })
                .ToList();

            // For PlatformNavHistory
            var platformNavRawData = await _unitOfWork.TradingAccountSnapshots.Query()
                .Where(s => s.SnapshotDate >= DateOnly.FromDateTime(thirtyDaysAgo))
                .GroupBy(s => s.SnapshotDate)
                .Select(g => new { Date = g.Key, Value = g.Sum(s => s.ClosingNav) })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);

            summary.PlatformNavHistory = platformNavRawData
                .Select(g => new ChartDataPoint<decimal> {
                    Date = g.Date.ToString("yyyy-MM-dd"),
                    Value = g.Value
                })
                .ToList();

            var lastTrade = await _unitOfWork.ShareTrades.Query()
                .OrderByDescending(st => st.TradeDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastTrade != null)
            {
                summary.LastMatchedPrice = lastTrade.TradePrice;
                summary.LastMatchedVolume = lastTrade.QuantityTraded;
            }

            return (summary, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching admin dashboard summary data.");
            return (null, "An error occurred while fetching dashboard data");
        }
    }
}