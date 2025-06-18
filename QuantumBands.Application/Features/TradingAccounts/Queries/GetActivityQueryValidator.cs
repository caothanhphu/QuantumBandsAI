using FluentValidation;

namespace QuantumBands.Application.Features.TradingAccounts.Queries
{
    public class GetActivityQueryValidator : AbstractValidator<GetActivityQuery>
    {
        public GetActivityQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("Page must be greater than 0");

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .LessThanOrEqualTo(200)
                .WithMessage("PageSize must be between 1 and 200");

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("StartDate must be less than or equal to EndDate");

            RuleFor(x => x.EndDate)
                .LessThanOrEqualTo(DateTime.UtcNow)
                .When(x => x.EndDate.HasValue)
                .WithMessage("EndDate cannot be in the future");
        }
    }
}