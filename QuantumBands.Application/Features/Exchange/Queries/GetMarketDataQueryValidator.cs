// QuantumBands.Application/Features/Exchange/Queries/GetMarketData/GetMarketDataQueryValidator.cs
using FluentValidation;
using System.Linq;

namespace QuantumBands.Application.Features.Exchange.Queries;

public class GetMarketDataQueryValidator : AbstractValidator<GetMarketDataQuery>
{
    public GetMarketDataQueryValidator()
    {
        RuleFor(x => x.RecentTradesLimit)
            .InclusiveBetween(1, 20).WithMessage("Recent trades limit must be between 1 and 20.");

        RuleFor(x => x.TradingAccountIds)
            .Must(ids => BeValidCommaSeparatedIntegers(ids))
            .WithMessage("TradingAccountIds must be a valid comma-separated list of integers, or empty.")
            .When(x => !string.IsNullOrEmpty(x.TradingAccountIds));
    }

    private bool BeValidCommaSeparatedIntegers(string? ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
        {
            return true; // Empty or null is considered valid (means all accounts)
        }
        var parts = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.All(part => int.TryParse(part, out _));
    }
}