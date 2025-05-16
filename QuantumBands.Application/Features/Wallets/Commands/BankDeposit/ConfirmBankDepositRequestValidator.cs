// QuantumBands.Application/Features/Wallets/Commands/BankDeposit/ConfirmBankDepositRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Wallets.Commands.BankDeposit;

public class ConfirmBankDepositRequestValidator : AbstractValidator<ConfirmBankDepositRequest>
{
    public ConfirmBankDepositRequestValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0).WithMessage("Transaction ID must be valid.");

        RuleFor(x => x.ActualAmountVNDReceived)
            .GreaterThanOrEqualTo(0).When(x => x.ActualAmountVNDReceived.HasValue)
            .WithMessage("Actual amount received cannot be negative.");
    }
}