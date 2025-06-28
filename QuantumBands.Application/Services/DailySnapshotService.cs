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
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands.ManualSnapshotTrigger;
using QuantumBands.Application.Features.Admin.TradingAccounts.Queries.GetSnapshotStatus;

namespace QuantumBands.Application.Services;

public class DailySnapshotService : IDailySnapshotService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DailySnapshotService> _logger;
    private readonly IProfitDistributionService _profitDistributionService; // Inject if BE-PROFIT-DIST is ready

    public DailySnapshotService(
        IUnitOfWork unitOfWork,
        ILogger<DailySnapshotService> logger,
        IProfitDistributionService profitDistributionService )
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _profitDistributionService = profitDistributionService;
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

                decimal realizedPAndLForTheDay = tradesToProcess.Sum(ct => ct.RealizedPandL);

                // 3. UnrealizedPAndLForTheDay (Sum of FloatingPAndL from current open positions)
                // This value is taken at the moment the snapshot is created.
                decimal currentTotalFloatingPAndL = await _unitOfWork.EAOpenPositions.Query()
                    .Where(op => op.TradingAccountId == account.TradingAccountId)
                    .SumAsync(op => (decimal?)op.FloatingPandL ?? 0, cancellationToken); // Cast to nullable decimal

                // 4. ManagementFeeDeducted
                decimal managementFeeDeducted = 0;
                if (realizedPAndLForTheDay > 0 && account.ManagementFeeRate > 0)
                {
                    managementFeeDeducted = Math.Round(realizedPAndLForTheDay * account.ManagementFeeRate, 2);
                    // TODO: Create a WalletTransaction for this fee if applicable
                    _logger.LogInformation("Calculated management fee for TA_ID {TradingAccountId}: {FeeAmount}", account.TradingAccountId, managementFeeDeducted);
                }

                // --- BẮT ĐẦU TÍCH HỢP PROFIT DISTRIBUTION ---
                // Tạo bản ghi snapshot nháp để lấy ID (nếu cần ID trước khi gọi ProfitDistribution)
                // Hoặc truyền null/0 và cập nhật sau. Để đơn giản, sẽ tạo snapshot trước.
                var tempSnapshotForId = new TradingAccountSnapshot { TradingAccountId = account.TradingAccountId, SnapshotDate = DateOnly.FromDateTime(snapshotDate), CreatedAt = DateTime.UtcNow };
                // Không AddAsync và CompleteAsync ở đây nếu không muốn có ID ngay.
                // ProfitDistributionService sẽ cần TradingAccountSnapshotID.
                // Cách tiếp cận: Tạo snapshot, lưu để lấy ID, rồi mới gọi ProfitDistribution.

                // Bước 1: Tạo và lưu snapshot ban đầu (chưa có ProfitDistributed và ManagementFee)
                var initialSnapshot = new TradingAccountSnapshot
                {
                    TradingAccountId = account.TradingAccountId,
                    SnapshotDate = DateOnly.FromDateTime(snapshotDate),
                    OpeningNav = openingNAV,
                    RealizedPandLforTheDay = realizedPAndLForTheDay,
                    UnrealizedPandLforTheDay = currentTotalFloatingPAndL,
                    ManagementFeeDeducted = 0, // Sẽ được cập nhật bởi ProfitDistributionService
                    ProfitDistributed = 0,     // Sẽ được cập nhật bởi ProfitDistributionService
                    ClosingNav = 0,            // Sẽ được tính toán lại sau
                    ClosingSharePrice = 0,     // Sẽ được tính toán lại sau
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.TradingAccountSnapshots.AddAsync(initialSnapshot);
                await _unitOfWork.CompleteAsync(cancellationToken); // Lưu để lấy initialSnapshot.SnapshotId

                // Gọi ProfitDistributionService
                var (managementFee, profitDistributed) = await _profitDistributionService.CalculateAndDistributeProfitAsync(
                    account,
                    realizedPAndLForTheDay,
                    snapshotDate,
                    initialSnapshot.SnapshotId, // Truyền ID của snapshot vừa tạo
                    cancellationToken);

                // Cập nhật lại snapshot với thông tin phí và lợi nhuận đã chia
                initialSnapshot.ManagementFeeDeducted = managementFee;
                initialSnapshot.ProfitDistributed = profitDistributed;

                // Tính lại ClosingNAV và ClosingSharePrice
                initialSnapshot.ClosingNav = account.CurrentNetAssetValue - initialSnapshot.ManagementFeeDeducted - initialSnapshot.ProfitDistributed;
                initialSnapshot.ClosingSharePrice = (account.TotalSharesIssued > 0)
                    ? Math.Round(initialSnapshot.ClosingNav / account.TotalSharesIssued, 8)
                    : 0;

                _unitOfWork.TradingAccountSnapshots.Update(initialSnapshot);
                // --- KẾT THÚC TÍCH HỢP PROFIT DISTRIBUTION ---


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
                    OpeningNav = openingNAV,
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

    public async Task<ManualSnapshotTriggerResponse> TriggerManualSnapshotAsync(ManualSnapshotTriggerRequest request, CancellationToken cancellationToken = default)
    {
        var snapshotDate = request.TargetDate.Date;
        _logger.LogInformation("Starting manual snapshot trigger for date: {SnapshotDate}, Reason: {Reason}", snapshotDate, request.Reason);

        var response = new ManualSnapshotTriggerResponse
        {
            Success = true,
            Message = "Manual snapshot trigger completed"
        };

        try
        {
            // Get trading accounts to process
            var query = _unitOfWork.TradingAccounts.Query().Where(ta => ta.IsActive);
            
            if (request.TradingAccountIds?.Any() == true)
            {
                query = query.Where(ta => request.TradingAccountIds.Contains(ta.TradingAccountId));
            }

            var accountsToProcess = await query.ToListAsync(cancellationToken);

            if (!accountsToProcess.Any())
            {
                response.Success = false;
                response.Message = "No active trading accounts found to process";
                return response;
            }

            decimal totalProfitDistributed = 0;

            foreach (var account in accountsToProcess)
            {
                var accountResult = new ManualSnapshotAccountResult
                {
                    TradingAccountId = account.TradingAccountId,
                    AccountName = account.AccountName
                };

                try
                {
                    // Check if snapshot already exists
                    var existingSnapshot = await _unitOfWork.TradingAccountSnapshots.Query()
                        .FirstOrDefaultAsync(s => s.TradingAccountId == account.TradingAccountId &&
                                                 s.SnapshotDate == DateOnly.FromDateTime(snapshotDate), cancellationToken);

                    if (existingSnapshot != null && !request.ForceRecalculate)
                    {
                        accountResult.Status = "Skipped";
                        accountResult.Message = "Snapshot already exists for this date";
                        accountResult.SnapshotId = existingSnapshot.SnapshotId;
                        accountResult.ProfitDistributed = existingSnapshot.ProfitDistributed;
                        response.AccountsSkipped++;
                    }
                    else
                    {
                        // Remove existing snapshot if force recalculate
                        if (existingSnapshot != null && request.ForceRecalculate)
                        {
                            // First reverse existing profit distribution
                            await ReverseExistingProfitDistributionAsync(existingSnapshot, cancellationToken);
                            _unitOfWork.TradingAccountSnapshots.Remove(existingSnapshot);
                            await _unitOfWork.CompleteAsync(cancellationToken);
                        }

                        // Create new snapshot using the same logic as automatic process
                        var snapshotResult = await CreateSnapshotForAccount(account, snapshotDate, cancellationToken);
                        
                        if (snapshotResult.Success)
                        {
                            accountResult.Status = "Success";
                            accountResult.Message = "Snapshot created successfully";
                            accountResult.SnapshotId = snapshotResult.SnapshotId;
                            accountResult.ProfitDistributed = snapshotResult.ProfitDistributed;
                            accountResult.ShareholdersCount = snapshotResult.ShareholdersCount;
                            totalProfitDistributed += snapshotResult.ProfitDistributed ?? 0;
                            response.AccountsProcessed++;
                        }
                        else
                        {
                            accountResult.Status = "Failed";
                            accountResult.Message = snapshotResult.ErrorMessage;
                            response.AccountsFailed++;
                            response.Errors.Add($"TA_ID {account.TradingAccountId}: {snapshotResult.ErrorMessage}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process manual snapshot for TradingAccountID: {TradingAccountId}", account.TradingAccountId);
                    accountResult.Status = "Failed";
                    accountResult.Message = ex.Message;
                    response.AccountsFailed++;
                    response.Errors.Add($"TA_ID {account.TradingAccountId}: {ex.Message}");
                }

                response.AccountResults.Add(accountResult);
            }

            response.TotalProfitDistributed = totalProfitDistributed;
            response.Success = response.AccountsFailed == 0;

            if (!response.Success)
            {
                response.Message = $"Manual snapshot completed with {response.AccountsFailed} failures";
            }

            _logger.LogInformation("Manual snapshot trigger completed. Processed: {Processed}, Skipped: {Skipped}, Failed: {Failed}",
                response.AccountsProcessed, response.AccountsSkipped, response.AccountsFailed);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual snapshot trigger failed for date: {SnapshotDate}", snapshotDate);
            response.Success = false;
            response.Message = $"Manual snapshot trigger failed: {ex.Message}";
            response.Errors.Add(ex.Message);
            return response;
        }
    }

    public async Task<SnapshotStatusResponse> GetSnapshotStatusAsync(GetSnapshotStatusQuery query, CancellationToken cancellationToken = default)
    {
        var snapshotDate = query.Date.Date;
        _logger.LogInformation("Getting snapshot status for date: {SnapshotDate}", snapshotDate);

        var response = new SnapshotStatusResponse
        {
            Date = snapshotDate
        };

        try
        {
            // Get trading accounts to check
            var accountsQuery = _unitOfWork.TradingAccounts.Query().Where(ta => ta.IsActive);
            
            if (query.TradingAccountIds?.Any() == true)
            {
                accountsQuery = accountsQuery.Where(ta => query.TradingAccountIds.Contains(ta.TradingAccountId));
            }

            var accounts = await accountsQuery.ToListAsync(cancellationToken);
            response.TotalAccounts = accounts.Count;

            foreach (var account in accounts)
            {
                var accountStatus = new AccountSnapshotStatus
                {
                    TradingAccountId = account.TradingAccountId,
                    AccountName = account.AccountName
                };

                var snapshot = await _unitOfWork.TradingAccountSnapshots.Query()
                    .FirstOrDefaultAsync(s => s.TradingAccountId == account.TradingAccountId &&
                                             s.SnapshotDate == DateOnly.FromDateTime(snapshotDate), cancellationToken);

                if (snapshot != null)
                {
                    accountStatus.SnapshotExists = true;
                    accountStatus.SnapshotId = snapshot.SnapshotId;
                    accountStatus.ProfitDistributed = snapshot.ProfitDistributed;
                    accountStatus.CreatedAt = snapshot.CreatedAt;
                    accountStatus.Status = "Completed";
                    accountStatus.OpeningNAV = snapshot.OpeningNav;
                    accountStatus.ClosingNAV = snapshot.ClosingNav;
                    accountStatus.RealizedPAndL = snapshot.RealizedPandLforTheDay;
                    accountStatus.ManagementFee = snapshot.ManagementFeeDeducted;

                    // Get shareholders count for this account
                    var shareholdersCount = await _unitOfWork.ProfitDistributionLogs.Query()
                        .Where(p => p.TradingAccountSnapshotId == snapshot.SnapshotId)
                        .CountAsync(cancellationToken);
                    accountStatus.ShareholdersCount = shareholdersCount;

                    response.CompletedCount++;
                    response.TotalProfitDistributed += snapshot.ProfitDistributed;
                    response.TotalShareholdersAffected += shareholdersCount;
                }
                else
                {
                    accountStatus.SnapshotExists = false;
                    accountStatus.Status = "Pending";
                    accountStatus.Reason = "No snapshot created for this date";
                    response.PendingCount++;
                }

                response.Accounts.Add(accountStatus);
            }

            _logger.LogInformation("Snapshot status retrieved for {Date}. Total: {Total}, Completed: {Completed}, Pending: {Pending}",
                snapshotDate, response.TotalAccounts, response.CompletedCount, response.PendingCount);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get snapshot status for date: {SnapshotDate}", snapshotDate);
            throw;
        }
    }

    private async Task<SnapshotCreationResult> CreateSnapshotForAccount(TradingAccount account, DateTime snapshotDate, CancellationToken cancellationToken)
    {
        try
        {
            // Reuse the same logic from CreateDailySnapshotsAsync
            var previousDaySnapshot = await _unitOfWork.TradingAccountSnapshots.Query()
                .Where(s => s.TradingAccountId == account.TradingAccountId && s.SnapshotDate < DateOnly.FromDateTime(snapshotDate))
                .OrderByDescending(s => s.SnapshotDate)
                .FirstOrDefaultAsync(cancellationToken);

            decimal openingNAV = previousDaySnapshot?.ClosingNav ?? account.InitialCapital;

            var tradesToProcess = await _unitOfWork.EAClosedTrades.Query()
                .Where(ct => ct.TradingAccountId == account.TradingAccountId &&
                             !ct.IsProcessedForDailyPandL &&
                             ct.CloseTime.Date == snapshotDate)
                .ToListAsync(cancellationToken);

            decimal realizedPAndLForTheDay = tradesToProcess.Sum(ct => ct.RealizedPandL);

            decimal currentTotalFloatingPAndL = await _unitOfWork.EAOpenPositions.Query()
                .Where(op => op.TradingAccountId == account.TradingAccountId)
                .SumAsync(op => (decimal?)op.FloatingPandL ?? 0, cancellationToken);

            // Create initial snapshot
            var initialSnapshot = new TradingAccountSnapshot
            {
                TradingAccountId = account.TradingAccountId,
                SnapshotDate = DateOnly.FromDateTime(snapshotDate),
                OpeningNav = openingNAV,
                RealizedPandLforTheDay = realizedPAndLForTheDay,
                UnrealizedPandLforTheDay = currentTotalFloatingPAndL,
                ManagementFeeDeducted = 0,
                ProfitDistributed = 0,
                ClosingNav = 0,
                ClosingSharePrice = 0,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.TradingAccountSnapshots.AddAsync(initialSnapshot);
            await _unitOfWork.CompleteAsync(cancellationToken);

            // Calculate and distribute profit
            var (managementFee, profitDistributed) = await _profitDistributionService.CalculateAndDistributeProfitAsync(
                account,
                realizedPAndLForTheDay,
                snapshotDate,
                initialSnapshot.SnapshotId,
                cancellationToken);

            // Update snapshot with final values
            initialSnapshot.ManagementFeeDeducted = managementFee;
            initialSnapshot.ProfitDistributed = profitDistributed;
            initialSnapshot.ClosingNav = account.CurrentNetAssetValue - managementFee - profitDistributed;
            initialSnapshot.ClosingSharePrice = (account.TotalSharesIssued > 0)
                ? Math.Round(initialSnapshot.ClosingNav / account.TotalSharesIssued, 8)
                : 0;

            _unitOfWork.TradingAccountSnapshots.Update(initialSnapshot);

            // Mark trades as processed
            foreach (var trade in tradesToProcess)
            {
                trade.IsProcessedForDailyPandL = true;
                _unitOfWork.EAClosedTrades.Update(trade);
            }

            await _unitOfWork.CompleteAsync(cancellationToken);

            // Get shareholders count
            var shareholdersCount = await _unitOfWork.ProfitDistributionLogs.Query()
                .Where(p => p.TradingAccountSnapshotId == initialSnapshot.SnapshotId)
                .CountAsync(cancellationToken);

            return new SnapshotCreationResult
            {
                Success = true,
                SnapshotId = initialSnapshot.SnapshotId,
                ProfitDistributed = profitDistributed,
                ShareholdersCount = shareholdersCount
            };
        }
        catch (Exception ex)
        {
            return new SnapshotCreationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task ReverseExistingProfitDistributionAsync(TradingAccountSnapshot snapshot, CancellationToken cancellationToken)
    {
        // Get all profit distribution logs for this snapshot
        var distributionLogs = await _unitOfWork.ProfitDistributionLogs.Query()
            .Include(p => p.User.Wallet)
            .Where(p => p.TradingAccountSnapshotId == snapshot.SnapshotId)
            .ToListAsync(cancellationToken);

        if (!distributionLogs.Any()) return;

        // Get transaction type for reversal
        var reversalType = await _unitOfWork.TransactionTypes.Query()
            .FirstOrDefaultAsync(t => t.TypeName == "ProfitDistributionReversal", cancellationToken);

        if (reversalType == null)
        {
            _logger.LogWarning("TransactionType 'ProfitDistributionReversal' not found. Creating manual reversal transactions.");
        }

        var now = DateTime.UtcNow;

        foreach (var log in distributionLogs)
        {
            if (log.User?.Wallet == null) continue;

            // Create reversal wallet transaction
            var reversalTransaction = new WalletTransaction
            {
                WalletId = log.User.Wallet.WalletId,
                TransactionTypeId = reversalType?.TransactionTypeId ?? 1, // Fallback to a default type
                Amount = -log.TotalAmountDistributed, // Negative amount for reversal
                CurrencyCode = log.User.Wallet.CurrencyCode,
                BalanceBefore = log.User.Wallet.Balance,
                BalanceAfter = log.User.Wallet.Balance - log.TotalAmountDistributed,
                Description = $"Reversal of profit distribution from snapshot {snapshot.SnapshotId} due to recalculation",
                ReferenceId = $"REV_SNAP_{snapshot.SnapshotId}_USR_{log.UserId}",
                Status = "Completed",
                PaymentMethod = "SystemReversal",
                TransactionDate = now,
                UpdatedAt = now
            };
            await _unitOfWork.WalletTransactions.AddAsync(reversalTransaction);

            // Update wallet balance
            log.User.Wallet.Balance -= log.TotalAmountDistributed;
            log.User.Wallet.UpdatedAt = now;
            _unitOfWork.Wallets.Update(log.User.Wallet);
        }

        // Remove the distribution logs
        foreach (var log in distributionLogs)
        {
            _unitOfWork.ProfitDistributionLogs.Remove(log);
        }

        await _unitOfWork.CompleteAsync(cancellationToken);
    }

    private class SnapshotCreationResult
    {
        public bool Success { get; set; }
        public long? SnapshotId { get; set; }
        public decimal? ProfitDistributed { get; set; }
        public int? ShareholdersCount { get; set; }
        public string? ErrorMessage { get; set; }
    }
}