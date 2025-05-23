// QuantumBands.Application/Features/TradingAccounts/Queries/GetTradingAccountDetailsQueryValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.TradingAccounts.Queries;

public class GetTradingAccountDetailsQueryValidator : AbstractValidator<GetTradingAccountDetailsQuery>
{
    public GetTradingAccountDetailsQueryValidator()
    {
        RuleFor(x => x.ClosedTradesPageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.ClosedTradesPageSize).InclusiveBetween(1, 50); // Max 50
        RuleFor(x => x.SnapshotsPageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.SnapshotsPageSize).InclusiveBetween(1, 30); // Max 30
        RuleFor(x => x.OpenPositionsLimit).InclusiveBetween(1, 50); // Max 50
    }
}