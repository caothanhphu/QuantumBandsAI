// QuantumBands.Application/Features/Wallets/Commands/CreateWithdrawal/CreateWithdrawalRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Wallets.Commands.CreateWithdrawal;

public class CreateWithdrawalRequestValidator : AbstractValidator<CreateWithdrawalRequest>
{
    public CreateWithdrawalRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Withdrawal amount must be greater than 0.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required.")
            .Length(3).WithMessage("Currency code must be 3 characters long (e.g., USD).")
            .Must(code => string.Equals(code, "USD", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Currently, only USD withdrawals are supported.");

        RuleFor(x => x.WithdrawalMethodDetails)
            .NotEmpty().WithMessage("Withdrawal method details are required.")
            .MaximumLength(1000).WithMessage("Withdrawal method details cannot exceed 1000 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}