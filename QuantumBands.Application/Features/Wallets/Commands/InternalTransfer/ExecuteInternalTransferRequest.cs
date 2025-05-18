// QuantumBands.Application/Features/Wallets/Commands/InternalTransfer/ExecuteInternalTransferRequest.cs
namespace QuantumBands.Application.Features.Wallets.Commands.InternalTransfer;

public class ExecuteInternalTransferRequest
{
    public int RecipientUserId { get; set; }
    public decimal Amount { get; set; }
    public required string CurrencyCode { get; set; } // Mặc định "USD"
    public string? Description { get; set; }
}