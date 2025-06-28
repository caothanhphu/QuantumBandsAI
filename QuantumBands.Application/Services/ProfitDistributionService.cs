// QuantumBands.Application/Services/ProfitDistributionService.cs
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Interfaces.Repositories; // For ITransactionTypeRepository
using QuantumBands.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands.RecalculateProfitDistribution;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;
using QuantumBands.Application.Common.Models;

namespace QuantumBands.Application.Services;

public class ProfitDistributionService : IProfitDistributionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProfitDistributionService> _logger;
    private readonly ITransactionTypeRepository _transactionTypeRepository;
    // private readonly IWalletService _walletService; // Có thể inject IWalletService để tạo WalletTransaction

    public ProfitDistributionService(
        IUnitOfWork unitOfWork,
        ILogger<ProfitDistributionService> logger,
        ITransactionTypeRepository transactionTypeRepository
        /* IWalletService walletService */ )
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _transactionTypeRepository = transactionTypeRepository;
        // _walletService = walletService;
    }

    public async Task<(decimal TotalManagementFeeDeducted, decimal TotalProfitDistributed)> CalculateAndDistributeProfitAsync(
        TradingAccount tradingAccount,
        decimal realizedPAndLForTheDay,
        DateTime snapshotDate,
        long tradingAccountSnapshotId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting profit distribution for TradingAccountID: {TradingAccountId}, Date: {SnapshotDate}, RealizedP&L: {RealizedPAndL}",
                               tradingAccount.TradingAccountId, snapshotDate, realizedPAndLForTheDay);

        decimal totalManagementFeeDeducted = 0;
        decimal totalProfitDistributedToShareholders = 0;

        // 1. Calculate Management Fee
        if (realizedPAndLForTheDay > 0 && tradingAccount.ManagementFeeRate > 0)
        {
            totalManagementFeeDeducted = Math.Round(realizedPAndLForTheDay * tradingAccount.ManagementFeeRate, 2); // Giả sử 2 chữ số thập phân
            _logger.LogInformation("Calculated Management Fee for TA_ID {TradingAccountId}: {FeeAmount} (Rate: {Rate} on P&L: {PAndL})",
                                   tradingAccount.TradingAccountId, totalManagementFeeDeducted, tradingAccount.ManagementFeeRate, realizedPAndLForTheDay);
            // TODO: Ghi nhận phí này vào đâu đó nếu cần (ví dụ: vào một tài khoản doanh thu của hệ thống)
            // Hiện tại, nó chỉ được trừ khỏi lợi nhuận có thể phân phối.
        }

        // 2. Calculate Distributable Profit
        decimal distributableProfit = realizedPAndLForTheDay - totalManagementFeeDeducted;

        if (distributableProfit <= 0)
        {
            _logger.LogInformation("No distributable profit for TradingAccountID {TradingAccountId} on {SnapshotDate}. Distributable: {DistributableProfit}",
                                   tradingAccount.TradingAccountId, snapshotDate, distributableProfit);
            return (totalManagementFeeDeducted, 0);
        }

        if (tradingAccount.TotalSharesIssued <= 0)
        {
            _logger.LogWarning("TradingAccountID {TradingAccountId} has TotalSharesIssued <= 0. Cannot calculate profit per share.", tradingAccount.TradingAccountId);
            return (totalManagementFeeDeducted, 0);
        }

        // 3. Calculate Profit Per Share
        decimal profitPerShare = distributableProfit / tradingAccount.TotalSharesIssued;
        // Làm tròn profitPerShare đến nhiều chữ số thập phân để giảm sai số làm tròn khi nhân với số lượng lớn cổ phần
        profitPerShare = Math.Round(profitPerShare, 8); // Ví dụ 8 chữ số thập phân

        _logger.LogInformation("Distributable Profit for TA_ID {TradingAccountId}: {DistributableProfit}. Profit Per Share: {ProfitPerShare}",
                               tradingAccount.TradingAccountId, distributableProfit, profitPerShare);

        // 4. Get Shareholders
        var shareholders = await _unitOfWork.SharePortfolios.Query()
            .Include(sp => sp.User) // Nạp User để lấy Wallet
                .ThenInclude(u => u.Wallet)
            .Where(sp => sp.TradingAccountId == tradingAccount.TradingAccountId && sp.Quantity > 0)
            .ToListAsync(cancellationToken);

        if (!shareholders.Any())
        {
            _logger.LogInformation("No shareholders found for TradingAccountID {TradingAccountId} to distribute profit to.", tradingAccount.TradingAccountId);
            return (totalManagementFeeDeducted, 0);
        }

        var profitDistributionReceivedType = await _transactionTypeRepository.GetByNameAsync("ProfitDistributionReceived", cancellationToken);
        if (profitDistributionReceivedType == null)
        {
            _logger.LogError("TransactionType 'ProfitDistributionReceived' not found. Cannot distribute profits.");
            return (totalManagementFeeDeducted, 0); // Lỗi hệ thống
        }

        foreach (var shareholderPortfolio in shareholders)
        {
            if (shareholderPortfolio.User?.Wallet == null)
            {
                _logger.LogWarning("UserID {UserId} (PortfolioID: {PortfolioId}) does not have an associated wallet. Skipping profit distribution.",
                                   shareholderPortfolio.UserId, shareholderPortfolio.PortfolioId);
                continue;
            }

            decimal amountToDistributeToUser = Math.Round(profitPerShare * shareholderPortfolio.Quantity, 2); // Làm tròn đến 2 chữ số cho giao dịch tiền tệ

            if (amountToDistributeToUser <= 0)
            {
                _logger.LogInformation("Calculated distribution amount for UserID {UserId} is zero or negative. Skipping.", shareholderPortfolio.UserId);
                continue;
            }

            var now = DateTime.UtcNow;

            // a. Create WalletTransaction
            var walletTransaction = new WalletTransaction
            {
                WalletId = shareholderPortfolio.User.Wallet.WalletId,
                TransactionTypeId = profitDistributionReceivedType.TransactionTypeId,
                Amount = amountToDistributeToUser,
                CurrencyCode = shareholderPortfolio.User.Wallet.CurrencyCode, // Giả sử cùng currency
                BalanceBefore = shareholderPortfolio.User.Wallet.Balance,
                BalanceAfter = shareholderPortfolio.User.Wallet.Balance + amountToDistributeToUser,
                Description = $"Profit distribution from {tradingAccount.AccountName} for {snapshotDate:yyyy-MM-dd}. Shares: {shareholderPortfolio.Quantity} @ {profitPerShare:F8}/share.",
                ReferenceId = $"SNAP_{tradingAccountSnapshotId}_USR_{shareholderPortfolio.UserId}",
                Status = "Completed",
                PaymentMethod = "SystemDistribution",
                TransactionDate = now,
                UpdatedAt = now
            };
            await _unitOfWork.WalletTransactions.AddAsync(walletTransaction);
            // Phải lưu WalletTransaction trước để có ID cho ProfitDistributionLog nếu cần
            // Tuy nhiên, UnitOfWork sẽ xử lý việc này khi gọi CompleteAsync một lần.
            // Nếu cần ID ngay, phải gọi CompleteAsync ở đây (không khuyến khích trong vòng lặp).

            // b. Update Wallet Balance
            shareholderPortfolio.User.Wallet.Balance += amountToDistributeToUser;
            shareholderPortfolio.User.Wallet.UpdatedAt = now;
            _unitOfWork.Wallets.Update(shareholderPortfolio.User.Wallet);

            // c. Create ProfitDistributionLog
            // Để đơn giản, WalletTransactionID sẽ được gán sau khi tất cả được lưu và có ID.
            // Hoặc, nếu bạn không cần WalletTransactionID trong log ngay, có thể bỏ qua.
            // Nếu cần, bạn phải gọi SaveChangesAsync sau khi Add WalletTransaction để lấy ID.
            // Hiện tại, chúng ta sẽ không link trực tiếp WalletTransactionID trong log này để tránh SaveChangesAsync trong vòng lặp.
            var logEntry = new ProfitDistributionLog
            {
                TradingAccountSnapshotId = tradingAccountSnapshotId,
                TradingAccountId = tradingAccount.TradingAccountId,
                UserId = shareholderPortfolio.UserId,
                DistributionDate = DateOnly.FromDateTime(snapshotDate),
                SharesHeldAtDistribution = shareholderPortfolio.Quantity,
                ProfitPerShareDistributed = profitPerShare,
                TotalAmountDistributed = amountToDistributeToUser,
                // WalletTransactionId = walletTransaction.TransactionId, // Sẽ có ID sau khi SaveChanges
                CreatedAt = now
            };
            await _unitOfWork.ProfitDistributionLogs.AddAsync(logEntry); // Giả sử có repo này trong UoW

            totalProfitDistributedToShareholders += amountToDistributeToUser;
            _logger.LogInformation("Profit of {AmountDistributed} {Currency} distributed to UserID {UserId} for TA_ID {TradingAccountId}.",
                                   amountToDistributeToUser, walletTransaction.CurrencyCode, shareholderPortfolio.UserId, tradingAccount.TradingAccountId);
        }

        _logger.LogInformation("Total profit distributed to shareholders for TA_ID {TradingAccountId} on {SnapshotDate}: {TotalDistributed}",
                               tradingAccount.TradingAccountId, snapshotDate, totalProfitDistributedToShareholders);

        return (totalManagementFeeDeducted, totalProfitDistributedToShareholders);
    }

    public async Task<RecalculateProfitDistributionResponse> RecalculateProfitDistributionAsync(
        int accountId, 
        DateTime date, 
        RecalculateProfitDistributionRequest request, 
        CancellationToken cancellationToken = default)
    {
        var snapshotDate = date.Date;
        _logger.LogInformation("Starting profit distribution recalculation for TradingAccountID: {AccountId}, Date: {Date}, Reason: {Reason}",
            accountId, snapshotDate, request.Reason);

        var response = new RecalculateProfitDistributionResponse
        {
            Success = false,
            Message = "Recalculation failed"
        };

        try
        {
            // Get the trading account
            var tradingAccount = await _unitOfWork.TradingAccounts.Query()
                .FirstOrDefaultAsync(ta => ta.TradingAccountId == accountId, cancellationToken);

            if (tradingAccount == null)
            {
                response.Message = $"Trading account with ID {accountId} not found";
                return response;
            }

            // Get the existing snapshot
            var existingSnapshot = await _unitOfWork.TradingAccountSnapshots.Query()
                .FirstOrDefaultAsync(s => s.TradingAccountId == accountId &&
                                         s.SnapshotDate == DateOnly.FromDateTime(snapshotDate), cancellationToken);

            if (existingSnapshot == null)
            {
                response.Message = $"No snapshot found for date {snapshotDate:yyyy-MM-dd}";
                return response;
            }

            // Get existing distribution information
            var existingDistribution = await GetExistingDistributionSummary(existingSnapshot.SnapshotId, cancellationToken);
            response.OldDistribution = existingDistribution;

            if (request.ReverseExisting)
            {
                // Reverse existing profit distribution
                await ReverseExistingProfitDistributionAsync(existingSnapshot, cancellationToken);
                _logger.LogInformation("Reversed existing profit distribution for snapshot {SnapshotId}", existingSnapshot.SnapshotId);
            }

            // Recalculate profit distribution
            var (newManagementFee, newProfitDistributed) = await CalculateAndDistributeProfitAsync(
                tradingAccount,
                existingSnapshot.RealizedPandLforTheDay,
                snapshotDate,
                existingSnapshot.SnapshotId,
                cancellationToken);

            // Update snapshot with new values
            existingSnapshot.ManagementFeeDeducted = newManagementFee;
            existingSnapshot.ProfitDistributed = newProfitDistributed;
            existingSnapshot.ClosingNav = tradingAccount.CurrentNetAssetValue - newManagementFee - newProfitDistributed;
            existingSnapshot.ClosingSharePrice = (tradingAccount.TotalSharesIssued > 0)
                ? Math.Round(existingSnapshot.ClosingNav / tradingAccount.TotalSharesIssued, 8)
                : 0;

            _unitOfWork.TradingAccountSnapshots.Update(existingSnapshot);
            await _unitOfWork.CompleteAsync(cancellationToken);

            // Get new distribution information
            var newDistribution = await GetExistingDistributionSummary(existingSnapshot.SnapshotId, cancellationToken);
            response.NewDistribution = newDistribution;
            response.AdjustmentAmount = newDistribution.TotalDistributed - existingDistribution.TotalDistributed;

            response.Success = true;
            response.Message = "Profit distribution recalculated successfully";

            _logger.LogInformation("Profit distribution recalculation completed for TA_ID {AccountId}. Old: {OldAmount}, New: {NewAmount}, Adjustment: {Adjustment}",
                accountId, existingDistribution.TotalDistributed, newDistribution.TotalDistributed, response.AdjustmentAmount);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recalculate profit distribution for TradingAccountID: {AccountId}, Date: {Date}",
                accountId, snapshotDate);
            response.Message = $"Recalculation failed: {ex.Message}";
            response.Errors.Add(ex.Message);
            return response;
        }
    }

    public async Task<PaginatedList<ProfitDistributionLogDto>> GetProfitDistributionHistoryAsync(
        int accountId,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting profit distribution history for AccountID: {AccountId}, FromDate: {FromDate}, ToDate: {ToDate}",
            accountId, fromDate, toDate);

        var query = _unitOfWork.ProfitDistributionLogs.Query()
            .Include(p => p.User)
            .Include(p => p.TradingAccount)
            .AsQueryable();

        // Filter by account if specified (0 means all accounts)
        if (accountId > 0)
        {
            query = query.Where(p => p.TradingAccountId == accountId);
        }

        // Filter by date range
        if (fromDate.HasValue)
        {
            query = query.Where(p => p.DistributionDate >= DateOnly.FromDateTime(fromDate.Value));
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.DistributionDate <= DateOnly.FromDateTime(toDate.Value));
        }

        // Order by date descending
        query = query.OrderByDescending(p => p.DistributionDate)
                    .ThenByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProfitDistributionLogDto
            {
                DistributionLogId = p.DistributionLogId,
                TradingAccountSnapshotId = p.TradingAccountSnapshotId,
                TradingAccountId = p.TradingAccountId,
                TradingAccountName = p.TradingAccount.AccountName,
                UserId = p.UserId,
                UserEmail = p.User.Email,
                UserFullName = p.User.FullName ?? "Unknown User",
                DistributionDate = p.DistributionDate.ToDateTime(TimeOnly.MinValue),
                SharesHeldAtDistribution = p.SharesHeldAtDistribution,
                ProfitPerShareDistributed = p.ProfitPerShareDistributed,
                TotalAmountDistributed = p.TotalAmountDistributed,
                WalletTransactionId = p.WalletTransactionId,
                CreatedAt = p.CreatedAt,
                CurrencyCode = "USD" // Default currency
            })
            .ToListAsync(cancellationToken);

        return new PaginatedList<ProfitDistributionLogDto>(items, totalCount, pageNumber, pageSize);
    }

    private async Task<ProfitDistributionSummary> GetExistingDistributionSummary(long snapshotId, CancellationToken cancellationToken)
    {
        var logs = await _unitOfWork.ProfitDistributionLogs.Query()
            .Where(p => p.TradingAccountSnapshotId == snapshotId)
            .ToListAsync(cancellationToken);

        var snapshot = await _unitOfWork.TradingAccountSnapshots.Query()
            .FirstOrDefaultAsync(s => s.SnapshotId == snapshotId, cancellationToken);

        return new ProfitDistributionSummary
        {
            TotalDistributed = logs.Sum(l => l.TotalAmountDistributed),
            ShareholdersCount = logs.Count,
            ManagementFee = snapshot?.ManagementFeeDeducted ?? 0,
            RealizedPAndL = snapshot?.RealizedPandLforTheDay ?? 0,
            ProfitPerShare = logs.FirstOrDefault()?.ProfitPerShareDistributed ?? 0
        };
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
        var reversalType = await _transactionTypeRepository.GetByNameAsync("ProfitDistributionReversal", cancellationToken);

        if (reversalType == null)
        {
            _logger.LogWarning("TransactionType 'ProfitDistributionReversal' not found. Will use default transaction type.");
            // Try to get a default transaction type
            reversalType = await _transactionTypeRepository.GetByNameAsync("SystemAdjustment", cancellationToken);
        }

        var now = DateTime.UtcNow;

        foreach (var log in distributionLogs)
        {
            if (log.User?.Wallet == null)
            {
                _logger.LogWarning("User or Wallet is null for DistributionLogId {LogId}", log.DistributionLogId);
                continue;
            }

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
                ReferenceId = $"REV_SNAP_{snapshot.SnapshotId}_USR_{log.UserId}_{now:yyyyMMddHHmmss}",
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

            _logger.LogInformation("Reversed profit distribution of {Amount} for UserID {UserId} from snapshot {SnapshotId}",
                log.TotalAmountDistributed, log.UserId, snapshot.SnapshotId);
        }

        // Remove the distribution logs
        foreach (var log in distributionLogs)
        {
            _unitOfWork.ProfitDistributionLogs.Remove(log);
        }

        await _unitOfWork.CompleteAsync(cancellationToken);
        _logger.LogInformation("Completed reversal of {Count} profit distribution entries for snapshot {SnapshotId}",
            distributionLogs.Count, snapshot.SnapshotId);
    }
}