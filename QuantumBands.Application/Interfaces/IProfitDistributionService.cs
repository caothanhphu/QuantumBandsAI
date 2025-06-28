// QuantumBands.Application/Interfaces/IProfitDistributionService.cs
using QuantumBands.Domain.Entities;
using System.Threading.Tasks;
using System.Threading;
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands.RecalculateProfitDistribution;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;
using QuantumBands.Application.Common.Models;

namespace QuantumBands.Application.Interfaces;

public interface IProfitDistributionService
{
    /// <summary>
    /// Calculates management fees and distributes profits to shareholders for a given trading account and date.
    /// This method assumes it's called within a larger transaction managed by DailySnapshotService.
    /// </summary>
    /// <param name="tradingAccount">The trading account to process.</param>
    /// <param name="realizedPAndLForTheDay">The realized P&L for the snapshot date.</param>
    /// <param name="snapshotDate">The date of the snapshot for which profit is being distributed.</param>
    /// <param name="tradingAccountSnapshotId">The ID of the TradingAccountSnapshot being created.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A tuple containing the total management fee deducted and total profit distributed.</returns>
    Task<(decimal TotalManagementFeeDeducted, decimal TotalProfitDistributed)> CalculateAndDistributeProfitAsync(
        TradingAccount tradingAccount,
        decimal realizedPAndLForTheDay,
        DateTime snapshotDate,
        long tradingAccountSnapshotId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates profit distribution for an existing snapshot
    /// </summary>
    /// <param name="accountId">Trading account ID</param>
    /// <param name="date">Snapshot date</param>
    /// <param name="request">Recalculation request parameters</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Recalculation response with old and new distribution details</returns>
    Task<RecalculateProfitDistributionResponse> RecalculateProfitDistributionAsync(
        int accountId, 
        DateTime date, 
        RecalculateProfitDistributionRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets profit distribution history for a trading account
    /// </summary>
    /// <param name="accountId">Trading account ID (0 for all accounts)</param>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Paginated list of profit distribution logs</returns>
    Task<PaginatedList<ProfitDistributionLogDto>> GetProfitDistributionHistoryAsync(
        int accountId,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}