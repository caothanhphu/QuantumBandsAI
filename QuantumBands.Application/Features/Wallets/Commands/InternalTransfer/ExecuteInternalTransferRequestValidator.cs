// QuantumBands.Application/Features/Wallets/Commands/InternalTransfer/ExecuteInternalTransferRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Wallets.Commands.InternalTransfer;

public class ExecuteInternalTransferRequestValidator : AbstractValidator<ExecuteInternalTransferRequest>
{
    public ExecuteInternalTransferRequestValidator()
    {
        RuleFor(x => x.RecipientUserId)
            .GreaterThan(0).WithMessage("Recipient User ID must be valid.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Transfer amount must be greater than 0.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required.")
            .Length(3).WithMessage("Currency code must be 3 characters long.")
            .Must(code => string.Equals(code, "USD", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Currently, only USD transfers are supported.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}