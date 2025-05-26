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

        RuleFor(x => x.ActiveOfferingsLimit) // <<< THÊM MỚI
            .InclusiveBetween(1, 10).WithMessage("Active offerings limit must be between 1 and 10.");

        RuleFor(x => x.TradingAccountIds)
            .Must(BeValidCommaSeparatedIntegers)
            .WithMessage("TradingAccountIds must be a valid comma-separated list of integers, or empty.")
            .When(x => !string.IsNullOrEmpty(x.TradingAccountIds));
    }

    private bool BeValidCommaSeparatedIntegers(string? ids)
    {
        if (string.IsNullOrWhiteSpace(ids)) return true;
        var parts = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.All(part => int.TryParse(part, out _));
    }
}