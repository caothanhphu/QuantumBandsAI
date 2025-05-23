// QuantumBands.Application/Features/Exchange/Queries/GetOrderBook/GetOrderBookQueryValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Exchange.Queries;

public class GetOrderBookQueryValidator : AbstractValidator<GetOrderBookQuery>
{
    public GetOrderBookQueryValidator()
    {
        RuleFor(x => x.Depth)
            .InclusiveBetween(1, 20).WithMessage("Depth must be between 1 and 20.");
    }
}