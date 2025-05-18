// QuantumBands.Application/Features/Wallets/Commands/InternalTransfer/RecipientInfoResponse.cs
namespace QuantumBands.Application.Features.Wallets.Commands.InternalTransfer;

public class RecipientInfoResponse
{
    public int RecipientUserId { get; set; }
    public required string RecipientUsername { get; set; }
    public string? RecipientFullName { get; set; }
}