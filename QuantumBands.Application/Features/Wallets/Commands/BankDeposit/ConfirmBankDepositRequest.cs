// QuantumBands.Application/Features/Wallets/Commands/BankDeposit/ConfirmBankDepositRequest.cs
namespace QuantumBands.Application.Features.Wallets.Commands.BankDeposit;

public class ConfirmBankDepositRequest
{
    public long TransactionId { get; set; }
    public decimal? ActualAmountVNDReceived { get; set; } // Số tiền VND thực nhận (tùy chọn)
    public string? AdminNotes { get; set; } // Ghi chú của Admin (tùy chọn)
}