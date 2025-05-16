// QuantumBands.Application/Features/Wallets/Commands/AdminDeposit/AdminDirectDepositRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Wallets.Commands.AdminDeposit;

public class AdminDirectDepositRequestValidator : AbstractValidator<AdminDirectDepositRequest>
{
    public AdminDirectDepositRequestValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID must be valid.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required.")
            .Length(3).WithMessage("Currency code must be 3 characters long (e.g., USD).");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.ReferenceId)
            .MaximumLength(100).WithMessage("Reference ID cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.ReferenceId));
    }
}