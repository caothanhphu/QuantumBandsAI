// QuantumBands.Application/Features/Wallets/Commands/AdminActions/ApproveWithdrawalRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Wallets.Commands.AdminActions;

public class ApproveWithdrawalRequestValidator : AbstractValidator<ApproveWithdrawalRequest>
{
    public ApproveWithdrawalRequestValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0).WithMessage("Transaction ID must be valid.");

        RuleFor(x => x.AdminNotes)
            .MaximumLength(500).WithMessage("Admin notes cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.AdminNotes));

        RuleFor(x => x.ExternalTransactionReference)
            .MaximumLength(255).WithMessage("External transaction reference cannot exceed 255 characters.")
            .When(x => !string.IsNullOrEmpty(x.ExternalTransactionReference));
    }
}