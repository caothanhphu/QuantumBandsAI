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
}