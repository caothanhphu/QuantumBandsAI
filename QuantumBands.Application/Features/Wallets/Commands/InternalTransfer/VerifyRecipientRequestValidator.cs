// QuantumBands.Application/Features/Wallets/Commands/InternalTransfer/VerifyRecipientRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Wallets.Commands.InternalTransfer;

public class VerifyRecipientRequestValidator : AbstractValidator<VerifyRecipientRequest>
{
    public VerifyRecipientRequestValidator()
    {
        RuleFor(x => x.RecipientEmail)
            .NotEmpty().WithMessage("Recipient email is required.")
            .EmailAddress().WithMessage("A valid recipient email address is required.")
            .MaximumLength(255).WithMessage("Recipient email cannot exceed 255 characters.");
    }
}