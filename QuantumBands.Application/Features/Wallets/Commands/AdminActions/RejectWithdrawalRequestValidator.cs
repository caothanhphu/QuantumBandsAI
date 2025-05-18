// QuantumBands.Application/Features/Wallets/Commands/AdminActions/RejectWithdrawalRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Wallets.Commands.AdminActions;

public class RejectWithdrawalRequestValidator : AbstractValidator<RejectWithdrawalRequest>
{
    public RejectWithdrawalRequestValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0).WithMessage("Transaction ID must be valid.");

        RuleFor(x => x.AdminNotes)
            .NotEmpty().WithMessage("Admin notes (reason for rejection) are required.")
            .MaximumLength(500).WithMessage("Admin notes cannot exceed 500 characters.");
    }
}