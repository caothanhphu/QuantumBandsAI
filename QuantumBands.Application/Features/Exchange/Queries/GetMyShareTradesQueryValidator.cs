// QuantumBands.Application/Features/Exchange/Queries/GetMyTrades/GetMyShareTradesQueryValidator.cs
using FluentValidation;
using System;
using System.Collections.Generic;

namespace QuantumBands.Application.Features.Exchange.Queries;

public class GetMyShareTradesQueryValidator : AbstractValidator<GetMyShareTradesQuery>
{
    private readonly List<string> _allowedSortByFields = new List<string>
    {
        "tradedate", "tradingaccountname", "quantitytraded", "tradeprice"
    };
    private readonly List<string> _allowedOrderSides = new List<string> { "buy", "sell" };

    public GetMyShareTradesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.TradingAccountId)
            .GreaterThan(0).WithMessage("Trading Account ID must be a positive number.")
            .When(x => x.TradingAccountId.HasValue);

        RuleFor(x => x.OrderSide)
            .Must(side => string.IsNullOrEmpty(side) || _allowedOrderSides.Contains(side.ToLowerInvariant()))
            .WithMessage("OrderSide must be 'Buy' or 'Sell'.")
            .When(x => !string.IsNullOrEmpty(x.OrderSide));

        RuleFor(x => x.DateFrom)
            .LessThanOrEqualTo(x => x.DateTo.Value)
            .WithMessage("DateFrom must be earlier than or equal to DateTo.")
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);

        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrEmpty(sortBy) || _allowedSortByFields.Contains(sortBy.ToLowerInvariant()))
            .WithMessage(x => $"SortBy field '{x.SortBy}' is not allowed. Allowed fields are: {string.Join(", ", _allowedSortByFields)}.")
            .When(x => !string.IsNullOrEmpty(x.SortBy));

        RuleFor(x => x.SortOrder)
            .Must(sortOrder => string.IsNullOrEmpty(sortOrder) || sortOrder.ToLowerInvariant() == "asc" || sortOrder.ToLowerInvariant() == "desc")
            .WithMessage("SortOrder must be 'asc' or 'desc'.")
            .When(x => !string.IsNullOrEmpty(x.SortOrder));
    }
}