// QuantumBands.Application/Features/Admin/TradingAccounts/Commands/CreateTradingAccountRequestValidator.cs
using FluentValidation;
namespace QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
public class CreateTradingAccountRequestValidator : AbstractValidator<CreateTradingAccountRequest>
{
    public CreateTradingAccountRequestValidator()
    {
        RuleFor(x => x.AccountName)
            .NotEmpty().WithMessage("Account name is required.")
            .MaximumLength(100).WithMessage("Account name cannot exceed 100 characters.");
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
        RuleFor(x => x.EaName)
            .MaximumLength(100).WithMessage("EA name cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.EaName));
        RuleFor(x => x.BrokerPlatformIdentifier)
            .MaximumLength(100).WithMessage("Broker platform identifier cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.BrokerPlatformIdentifier));
        RuleFor(x => x.InitialCapital)
            .GreaterThan(0).WithMessage("Initial capital must be greater than 0.");
        RuleFor(x => x.TotalSharesIssued)
            .GreaterThan(0).WithMessage("Total shares issued must be greater than 0.");
        RuleFor(x => x.ManagementFeeRate)
            .InclusiveBetween(0, 0.9999m).WithMessage("Management fee rate must be between 0 and 0.9999 (e.g., 0.02 for 2%).");
    }
}