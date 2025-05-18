// QuantumBands.Application/Features/Wallets/Commands/AdminActions/RejectWithdrawalRequest.cs
namespace QuantumBands.Application.Features.Wallets.Commands.AdminActions;

public class RejectWithdrawalRequest
{
    public long TransactionId { get; set; }
    public required string AdminNotes { get; set; } // Bắt buộc
}