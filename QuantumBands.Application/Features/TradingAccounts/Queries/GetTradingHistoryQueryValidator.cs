using FluentValidation;

namespace QuantumBands.Application.Features.TradingAccounts.Queries;

public class GetTradingHistoryQueryValidator : AbstractValidator<GetTradingHistoryQuery>
{
    public GetTradingHistoryQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.Symbol)
            .MaximumLength(10)
            .When(x => !string.IsNullOrWhiteSpace(x.Symbol))
            .WithMessage("Symbol must not exceed 10 characters");

        RuleFor(x => x.Type)
            .Must(type => type == null || type == "BUY" || type == "SELL")
            .WithMessage("Type must be either 'BUY' or 'SELL'");

        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(x => x.EndDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("Start date must be less than or equal to end date");

        RuleFor(x => x.EndDate)
            .LessThanOrEqualTo(DateTime.UtcNow.Date.AddDays(1))
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date cannot be in the future");

        RuleFor(x => x.MinProfit)
            .LessThanOrEqualTo(x => x.MaxProfit)
            .When(x => x.MinProfit.HasValue && x.MaxProfit.HasValue)
            .WithMessage("Minimum profit must be less than or equal to maximum profit");

        RuleFor(x => x.MinVolume)
            .GreaterThan(0)
            .When(x => x.MinVolume.HasValue)
            .WithMessage("Minimum volume must be greater than 0");

        RuleFor(x => x.MinVolume)
            .LessThanOrEqualTo(x => x.MaxVolume)
            .When(x => x.MinVolume.HasValue && x.MaxVolume.HasValue)
            .WithMessage("Minimum volume must be less than or equal to maximum volume");

        RuleFor(x => x.MaxVolume)
            .GreaterThan(0)
            .When(x => x.MaxVolume.HasValue)
            .WithMessage("Maximum volume must be greater than 0");

        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrEmpty(sortBy) || 
                          new[] { "closetime", "opentime", "profit", "symbol", "volume" }
                              .Contains(sortBy.ToLowerInvariant()))
            .WithMessage("SortBy must be one of: closeTime, openTime, profit, symbol, volume");

        RuleFor(x => x.SortOrder)
            .Must(sortOrder => string.IsNullOrEmpty(sortOrder) || 
                             new[] { "asc", "desc" }.Contains(sortOrder.ToLowerInvariant()))
            .WithMessage("SortOrder must be either 'asc' or 'desc'");
    }
}