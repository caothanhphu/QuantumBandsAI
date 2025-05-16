// QuantumBands.Application/Features/Wallets/Commands/BankDeposit/CancelBankDepositRequest.cs
namespace QuantumBands.Application.Features.Wallets.Commands.BankDeposit;

public class CancelBankDepositRequest
{
    public long TransactionId { get; set; }
    public required string AdminNotes { get; set; } // Lý do hủy
}