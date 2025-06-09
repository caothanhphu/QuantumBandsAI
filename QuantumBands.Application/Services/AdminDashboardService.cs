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

            // Sử dụng Task.WhenAll để chạy các truy vấn song song
            var totalUsersTask = _unitOfWork.Users.Query().LongCountAsync(cancellationToken);
            var totalActiveFundsTask = _unitOfWork.TradingAccounts.Query().CountAsync(ta => ta.IsActive, cancellationToken);
            var totalPlatformNAVTask = _unitOfWork.TradingAccounts.Query().Where(ta => ta.IsActive).SumAsync(ta => ta.CurrentNetAssetValue, cancellationToken);
            var pendingDepositsCountTask = _unitOfWork.WalletTransactions.Query()
                .CountAsync(t => t.Status == "PendingBankTransfer" || t.Status == "PendingAdminConfirmation", cancellationToken); // Hoặc các trạng thái chờ khác
            var pendingWithdrawalsCountTask = _unitOfWork.WalletTransactions.Query()
                .CountAsync(t => t.Status == "PendingAdminApproval", cancellationToken);

            var recentTradesTask = _unitOfWork.ShareTrades.Query()
                .Include(st => st.TradingAccount)
                .OrderByDescending(st => st.TradeDate)
                .Take(5) // Lấy 5 giao dịch gần nhất
                .Select(st => new SimpleTradeInfoDto
                {
                    TradeId = st.TradeId,
                    TradingAccountName = st.TradingAccount.AccountName,
                    TradeTime = st.TradeDate,
                    QuantityTraded = st.QuantityTraded,
                    TradePrice = st.TradePrice
                })
                .ToListAsync(cancellationToken);

            var userGrowthDataTask = _unitOfWork.Users.Query()
                .Where(u => u.CreatedAt >= thirtyDaysAgo)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new ChartDataPoint<long> { Date = g.Key.ToString("yyyy-MM-dd"), Value = g.LongCount() })
                .OrderBy(dp => dp.Date)
                .ToListAsync(cancellationToken);

            // Fixing the CS0019 error by converting DateOnly to DateTime for comparison
            var platformNavHistoryTask = _unitOfWork.TradingAccountSnapshots.Query()
                .Where(s => s.SnapshotDate.ToDateTime(TimeOnly.MinValue) >= DateTime.UtcNow.Date.AddDays(-30)) // Convert DateOnly to DateTime
                .GroupBy(s => s.SnapshotDate)
                .Select(g => new ChartDataPoint<decimal>
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    Value = g.Sum(s => s.ClosingNav)
                })
                .OrderBy(dp => dp.Date)
                .ToListAsync(cancellationToken);

            await Task.WhenAll(
                totalUsersTask,
                totalActiveFundsTask,
                totalPlatformNAVTask,
                pendingDepositsCountTask,
                pendingWithdrawalsCountTask,
                recentTradesTask,
                userGrowthDataTask,
                platformNavHistoryTask);

            summary.TotalUsers = totalUsersTask.Result;
            summary.TotalActiveFunds = totalActiveFundsTask.Result;
            summary.TotalPlatformNAV = totalPlatformNAVTask.Result;
            summary.PendingDepositsCount = pendingDepositsCountTask.Result;
            summary.PendingWithdrawalsCount = pendingWithdrawalsCountTask.Result;
            summary.RecentTrades = recentTradesTask.Result;
            summary.UserGrowthData = userGrowthDataTask.Result;
            summary.PlatformNavHistory = platformNavHistoryTask.Result;

            return (summary, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching admin dashboard summary data.");
            return (null, "An error occurred while fetching dashboard data");
        }
    }
}