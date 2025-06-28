// QuantumBands.Application/Interfaces/IDailySnapshotService.cs
using System.Threading.Tasks;
using System.Threading;
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands.ManualSnapshotTrigger;
using QuantumBands.Application.Features.Admin.TradingAccounts.Queries.GetSnapshotStatus;

namespace QuantumBands.Application.Interfaces;

public interface IDailySnapshotService
{
    /// <summary>
    /// Creates daily snapshots for all active trading accounts.
    /// </summary>
    /// <param name="snapshotDate">The date for which to create snapshots (typically UtcNow.Date).</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A summary of the operation (e.g., number of snapshots created).</returns>
    Task<string> CreateDailySnapshotsAsync(DateTime snapshotDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually triggers snapshot creation for specified accounts or all active accounts
    /// </summary>
    /// <param name="request">Manual trigger request parameters</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Response containing processing results</returns>
    Task<ManualSnapshotTriggerResponse> TriggerManualSnapshotAsync(ManualSnapshotTriggerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets snapshot status for a specific date
    /// </summary>
    /// <param name="query">Query parameters including date and optional account filters</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Snapshot status response</returns>
    Task<SnapshotStatusResponse> GetSnapshotStatusAsync(GetSnapshotStatusQuery query, CancellationToken cancellationToken = default);
}