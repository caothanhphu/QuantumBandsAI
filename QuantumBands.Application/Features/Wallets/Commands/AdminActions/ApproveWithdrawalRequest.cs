// QuantumBands.Application/Features/Wallets/Commands/AdminActions/ApproveWithdrawalRequest.cs
namespace QuantumBands.Application.Features.Wallets.Commands.AdminActions;

public class ApproveWithdrawalRequest
{
    public long TransactionId { get; set; }
    public string? AdminNotes { get; set; }
    public string? ExternalTransactionReference { get; set; }
}