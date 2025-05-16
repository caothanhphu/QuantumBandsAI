// QuantumBands.Application/Features/Wallets/Commands/BankDeposit/InitiateBankDepositRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Wallets.Commands.BankDeposit;

public class InitiateBankDepositRequestValidator : AbstractValidator<InitiateBankDepositRequest>
{
    public InitiateBankDepositRequestValidator()
    {
        RuleFor(x => x.AmountUSD)
            .GreaterThan(0).WithMessage("Amount USD must be greater than 0.");
    }
}