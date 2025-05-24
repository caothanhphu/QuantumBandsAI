// QuantumBands.Application/Interfaces/IProfitDistributionService.cs
using QuantumBands.Domain.Entities;
using System.Threading.Tasks;
using System.Threading;

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
}