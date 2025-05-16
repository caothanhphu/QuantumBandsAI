// QuantumBands.Application/Features/Wallets/Commands/BankDeposit/CancelBankDepositRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Wallets.Commands.BankDeposit;

public class CancelBankDepositRequestValidator : AbstractValidator<CancelBankDepositRequest>
{
    public CancelBankDepositRequestValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0).WithMessage("Transaction ID must be valid.");

        RuleFor(x => x.AdminNotes)
            .NotEmpty().WithMessage("Admin notes are required for cancellation.")
            .MaximumLength(500).WithMessage("Admin notes cannot exceed 500 characters.");
    }
}