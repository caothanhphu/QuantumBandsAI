using FluentValidation;

namespace QuantumBands.Application.Features.Admin.TradingAccounts.Commands.RecalculateProfitDistribution;

public class RecalculateProfitDistributionRequestValidator : AbstractValidator<RecalculateProfitDistributionRequest>
{
    public RecalculateProfitDistributionRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required for audit purposes")
            .Length(10, 500)
            .WithMessage("Reason must be between 10 and 500 characters");

        RuleFor(x => x.AdminNotes)
            .MaximumLength(1000)
            .WithMessage("Admin notes cannot exceed 1000 characters");
    }
} 