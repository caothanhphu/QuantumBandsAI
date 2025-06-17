// QuantumBands.Application/Interfaces/IWalletService.cs
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Wallets.Commands.AdminActions;
using QuantumBands.Application.Features.Wallets.Commands.AdminDeposit; // Thêm using này
using QuantumBands.Application.Features.Wallets.Commands.BankDeposit; // Thêm using này
using QuantumBands.Application.Features.Wallets.Commands.CreateWithdrawal;
using QuantumBands.Application.Features.Wallets.Commands.InternalTransfer;
using QuantumBands.Application.Features.Wallets.Dtos;
using QuantumBands.Application.Features.Wallets.Queries.GetTransactions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.Application.Interfaces;

public interface IWalletService
{
    Task<(WalletDto? WalletProfile, string? ErrorMessage)> GetUserWalletAsync(ClaimsPrincipal currentUser, CancellationToken cancellationToken = default);
    Task<PaginatedList<WalletTransactionDto>> GetUserWalletTransactionsAsync(ClaimsPrincipal currentUser, GetWalletTransactionsQuery query, CancellationToken cancellationToken = default);
    Task<(BankDepositInfoResponse? Response, string? ErrorMessage)> InitiateBankDepositAsync(ClaimsPrincipal currentUser, InitiateBankDepositRequest request, CancellationToken cancellationToken = default);
    Task<(WalletTransactionDto? Transaction, string? ErrorMessage)> ConfirmBankDepositAsync(ClaimsPrincipal adminUser, ConfirmBankDepositRequest request, CancellationToken cancellationToken = default);
    Task<(WalletTransactionDto? Transaction, string? ErrorMessage)> CancelBankDepositAsync(ClaimsPrincipal adminUser, CancelBankDepositRequest request, CancellationToken cancellationToken = default);
    Task<(WalletTransactionDto? Transaction, string? ErrorMessage)> AdminDirectDepositAsync(ClaimsPrincipal adminUser, AdminDirectDepositRequest request, CancellationToken cancellationToken = default);
    Task<(WithdrawalRequestDto? Response, string? ErrorMessage)> CreateWithdrawalRequestAsync(ClaimsPrincipal currentUser, CreateWithdrawalRequest request, CancellationToken cancellationToken = default);
    Task<(WalletTransactionDto? Transaction, string? ErrorMessage)> ApproveWithdrawalAsync(ClaimsPrincipal adminUser, ApproveWithdrawalRequest request, CancellationToken cancellationToken = default);
    Task<(WalletTransactionDto? Transaction, string? ErrorMessage)> RejectWithdrawalAsync(ClaimsPrincipal adminUser, RejectWithdrawalRequest request, CancellationToken cancellationToken = default);
    Task<(RecipientInfoResponse? RecipientInfo, string? ErrorMessage)> VerifyRecipientForTransferAsync(VerifyRecipientRequest request, CancellationToken cancellationToken = default);
    Task<(WalletTransactionDto? SenderTransaction, string? ErrorMessage)> ExecuteInternalTransferAsync(ClaimsPrincipal senderUser, ExecuteInternalTransferRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedList<AdminPendingBankDepositDto>> GetAdminPendingBankDepositsAsync(ClaimsPrincipal adminUser, GetAdminPendingBankDepositsQuery query, CancellationToken cancellationToken = default);
    Task<PaginatedList<WithdrawalRequestAdminViewDto>> GetAdminPendingWithdrawalsAsync(ClaimsPrincipal adminUser, GetAdminPendingWithdrawalsQuery query, CancellationToken cancellationToken = default);
    Task<(bool Success, string? ErrorMessage, WalletTransactionDto? Transaction)> ReleaseHeldFundsForOrderAsync(
    int userId,
    long cancelledOrderId,
    decimal amountToRelease,
    string currencyCode,
    string reason,
    CancellationToken cancellationToken = default);

    Task<(decimal TotalDeposits, decimal TotalWithdrawals, decimal InitialDeposit)> GetFinancialSummaryAsync(int tradingAccountId, CancellationToken cancellationToken = default);

    // Các phương thức khác liên quan đến wallet sẽ được thêm vào đây sau
}