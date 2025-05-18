// QuantumBands.Application/Features/Wallets/Commands/InternalTransfer/VerifyRecipientRequest.cs
namespace QuantumBands.Application.Features.Wallets.Commands.InternalTransfer;

public class VerifyRecipientRequest
{
    public required string RecipientEmail { get; set; }
}