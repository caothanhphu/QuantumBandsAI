// QuantumBands.Application/Interfaces/IWalletService.cs
using QuantumBands.Application.Features.Wallets.Dtos;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Wallets.Queries.GetTransactions;
using QuantumBands.Application.Features.Wallets.Commands.BankDeposit; // Thêm using này
using QuantumBands.Application.Features.Wallets.Commands.AdminDeposit; // Thêm using này

namespace QuantumBands.Application.Interfaces;

public interface IWalletService
{
    Task<(WalletDto? WalletProfile, string? ErrorMessage)> GetUserWalletAsync(ClaimsPrincipal currentUser, CancellationToken cancellationToken = default);
    Task<PaginatedList<WalletTransactionDto>> GetUserWalletTransactionsAsync(ClaimsPrincipal currentUser, GetWalletTransactionsQuery query, CancellationToken cancellationToken = default);
    Task<(BankDepositInfoResponse? Response, string? ErrorMessage)> InitiateBankDepositAsync(ClaimsPrincipal currentUser, InitiateBankDepositRequest request, CancellationToken cancellationToken = default);
    Task<(WalletTransactionDto? Transaction, string? ErrorMessage)> ConfirmBankDepositAsync(ClaimsPrincipal adminUser, ConfirmBankDepositRequest request, CancellationToken cancellationToken = default);
    Task<(WalletTransactionDto? Transaction, string? ErrorMessage)> CancelBankDepositAsync(ClaimsPrincipal adminUser, CancelBankDepositRequest request, CancellationToken cancellationToken = default);
    Task<(WalletTransactionDto? Transaction, string? ErrorMessage)> AdminDirectDepositAsync(ClaimsPrincipal adminUser, AdminDirectDepositRequest request, CancellationToken cancellationToken = default);

    // Các phương thức khác liên quan đến wallet sẽ được thêm vào đây sau
}