// QuantumBands.Application/Interfaces/IDailySnapshotService.cs
using System.Threading.Tasks;
using System.Threading;

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
}