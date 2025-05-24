// QuantumBands.Application/Services/DailySnapshotService.cs
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Interfaces.Repositories; // If you have specific repositories
using QuantumBands.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace QuantumBands.Application.Services;

public class DailySnapshotService : IDailySnapshotService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DailySnapshotService> _logger;
    // private readonly IProfitDistributionService _profitDistributionService; // Inject if BE-PROFIT-DIST is ready

    public DailySnapshotService(
        IUnitOfWork unitOfWork,
        ILogger<DailySnapshotService> logger
        /* IProfitDistributionService profitDistributionService */)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        // _profitDistributionService = profitDistributionService;
    }

    public async Task<string> CreateDailySnapshotsAsync(DateTime snapshotDateInput, CancellationToken cancellationToken = default)
    {
        var snapshotDate = snapshotDateInput.Date; // Ensure it's just the date part
        _logger.LogInformation("Starting daily snapshot creation process for date: {SnapshotDate}", snapshotDate);

        var activeTradingAccounts = await _unitOfWork.TradingAccounts.Query()
            .Where(ta => ta.IsActive)
            .ToListAsync(cancellationToken);

        if (!activeTradingAccounts.Any())
        {
            _logger.LogInformation("No active trading accounts found to create snapshots for.");
            return "No active trading accounts.";
        }

        int snapshotsCreated = 0;
        int accountsProcessed = 0;
        var errors = new List<string>();

        foreach (var account in activeTradingAccounts)
        {
            accountsProcessed++;
            _logger.LogInformation("Processing snapshot for TradingAccountID: {TradingAccountId}, Name: {AccountName}", account.TradingAccountId, account.AccountName);

            try
            {
                // Fix for CS0019: Convert DateTime to DateOnly for comparison
                bool snapshotExists = await _unitOfWork.TradingAccountSnapshots.Query()
                    .AnyAsync(s => s.TradingAccountId == account.TradingAccountId &&
                                   s.SnapshotDate == DateOnly.FromDateTime(snapshotDate), cancellationToken);
                if (snapshotExists)
                {
                    _logger.LogWarning("Snapshot for TradingAccountID {TradingAccountId} on {SnapshotDate} already exists. Skipping.", account.TradingAccountId, snapshotDate);
                    errors.Add($"TA_ID {account.TradingAccountId}: Snapshot already exists for {snapshotDate:yyyy-MM-dd}.");
                    continue;
                }

                // 1. OpeningNAV
                var previousDaySnapshot = await _unitOfWork.TradingAccountSnapshots.Query()
                    .Where(s => s.TradingAccountId == account.TradingAccountId && s.SnapshotDate < DateOnly.FromDateTime(snapshotDate))
                    .OrderByDescending(s => s.SnapshotDate)
                    .FirstOrDefaultAsync(cancellationToken);

                decimal openingNAV = previousDaySnapshot?.ClosingNav ?? account.InitialCapital;
                decimal previousDayUnrealizedPAndL = previousDaySnapshot?.UnrealizedPandLforTheDay ?? 0;

                // 2. RealizedPAndLForTheDay
                var tradesToProcess = await _unitOfWork.EAClosedTrades.Query()
                    .Where(ct => ct.TradingAccountId == account.TradingAccountId &&
                                 !ct.IsProcessedForDailyPandL &&
                                 ct.CloseTime.Date == snapshotDate) // Trades closed on the snapshot date
                    .ToListAsync(cancellationToken);

                decimal realizedPAndLForTheDay = tradesToProcess.Sum(ct => ct.RealizedPAndL);

                // 3. UnrealizedPAndLForTheDay (Sum of FloatingPAndL from current open positions)
                // This value is taken at the moment the snapshot is created.
                decimal currentTotalFloatingPAndL = await _unitOfWork.EAOpenPositions.Query()
                    .Where(op => op.TradingAccountId == account.TradingAccountId)
                    .SumAsync(op => (decimal?)op.FloatingPAndL ?? 0, cancellationToken); // Cast to nullable decimal

                // 4. ManagementFeeDeducted
                decimal managementFeeDeducted = 0;
                if (realizedPAndLForTheDay > 0 && account.ManagementFeeRate > 0)
                {
                    managementFeeDeducted = Math.Round(realizedPAndLForTheDay * account.ManagementFeeRate, 2);
                    // TODO: Create a WalletTransaction for this fee if applicable
                    _logger.LogInformation("Calculated management fee for TA_ID {TradingAccountId}: {FeeAmount}", account.TradingAccountId, managementFeeDeducted);
                }

                // 5. ProfitDistributed (Placeholder - from separate epic)
                decimal profitDistributed = 0;
                // if (realizedPAndLForTheDay - managementFeeDeducted > 0)
                // {
                //     profitDistributed = await _profitDistributionService.CalculateAndDistributeProfitAsync(
                //         account.TradingAccountId,
                //         realizedPAndLForTheDay - managementFeeDeducted,
                //         snapshotDate,
                //         cancellationToken);
                // }

                // 6. ClosingNAV
                // Option: Simpler ClosingNAV based on pushed Equity
                // ClosingNAV_Snapshot = CurrentNetAssetValue_from_latest_push - ManagementFeeDeducted_today - ProfitDistributed_today
                // The TradingAccount.CurrentNetAssetValue is the most up-to-date equity from MT5 (pushed by BE-EA-001)
                decimal closingNAV = account.CurrentNetAssetValue - managementFeeDeducted - profitDistributed;
                // This assumes CurrentNetAssetValue already reflects the day's realized and unrealized P&L from MT5.
                // The RealizedPAndLForTheDay and UnrealizedPAndLForTheDay calculated here are for breaking down
                // the components of NAV change within our system's accounting.

                // 7. ClosingSharePrice
                decimal closingSharePrice = (account.TotalSharesIssued > 0)
                    ? Math.Round(closingNAV / account.TotalSharesIssued, 8) // Assuming 8 decimal places for share price
                    : 0;

                // 8. Save to TradingAccountSnapshots
                var newSnapshot = new TradingAccountSnapshot
                {
                    TradingAccountId = account.TradingAccountId,
                    SnapshotDate = DateOnly.FromDateTime(snapshotDate),
                    OpeningNAV = openingNAV,
                    RealizedPandLforTheDay = realizedPAndLForTheDay,
                    UnrealizedPandLforTheDay = currentTotalFloatingPAndL, // This is the sum at snapshot time
                    ManagementFeeDeducted = managementFeeDeducted,
                    ProfitDistributed = profitDistributed,
                    ClosingNav = closingNAV,
                    ClosingSharePrice = closingSharePrice,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.TradingAccountSnapshots.AddAsync(newSnapshot);

                // 9. Mark EAClosedTrades as processed
                foreach (var trade in tradesToProcess)
                {
                    trade.IsProcessedForDailyPandL = true;
                    _unitOfWork.EAClosedTrades.Update(trade);
                }

                // 10. TradingAccounts.CurrentNetAssetValue is primarily updated by BE-EA-001.
                // This snapshot records the NAV *after* internal accounting (fees, distributions).
                // No direct update to TradingAccount.CurrentNetAssetValue here, as it reflects MT5 equity.

                await _unitOfWork.CompleteAsync(cancellationToken); // Save snapshot and trade updates
                snapshotsCreated++;
                _logger.LogInformation("Snapshot created successfully for TradingAccountID: {TradingAccountId} for date {SnapshotDate}. ClosingNAV: {ClosingNAV}",
                                       account.TradingAccountId, snapshotDate, closingNAV);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create snapshot for TradingAccountID: {TradingAccountId}, Name: {AccountName} on {SnapshotDate}",
                                 account.TradingAccountId, account.AccountName, snapshotDate);
                errors.Add($"TA_ID {account.TradingAccountId}: {ex.Message}");
            }
        }

        string summary = $"Daily snapshot process completed for {snapshotDate:yyyy-MM-dd}. Accounts processed: {accountsProcessed}. Snapshots created: {snapshotsCreated}.";
        if (errors.Any())
        {
            summary += $" Errors: {string.Join("; ", errors)}";
        }
        _logger.LogInformation(summary);
        return summary;
    }
}