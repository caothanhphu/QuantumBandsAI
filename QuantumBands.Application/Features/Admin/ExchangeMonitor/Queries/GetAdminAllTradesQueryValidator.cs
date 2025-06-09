// QuantumBands.Application/Features/Admin/ExchangeMonitor/Queries/GetAdminAllTradesQueryValidator.cs
using FluentValidation;
using System;
using System.Collections.Generic;

namespace QuantumBands.Application.Features.Admin.ExchangeMonitor.Queries;

public class GetAdminAllTradesQueryValidator : AbstractValidator<GetAdminAllTradesQuery>
{
    private readonly List<string> _allowedSortByFields = new List<string>
    {
        "tradedate", "tradingaccountname", "quantitytraded", "tradeprice", "totalvalue"
    };

    public GetAdminAllTradesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.TradingAccountId).GreaterThan(0).When(x => x.TradingAccountId.HasValue);
        RuleFor(x => x.BuyerUserId).GreaterThan(0).When(x => x.BuyerUserId.HasValue);
        RuleFor(x => x.SellerUserId).GreaterThan(0).When(x => x.SellerUserId.HasValue);

        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrEmpty(sortBy) || _allowedSortByFields.Contains(sortBy.ToLowerInvariant()))
            .WithMessage(x => $"SortBy field '{x.SortBy}' is not allowed. Allowed fields are: {string.Join(", ", _allowedSortByFields)}.")
            .When(x => !string.IsNullOrEmpty(x.SortBy));

        RuleFor(x => x.SortOrder)
            .Must(sortOrder => string.IsNullOrEmpty(sortOrder) || sortOrder.ToLowerInvariant() == "asc" || sortOrder.ToLowerInvariant() == "desc")
            .WithMessage("SortOrder must be 'asc' or 'desc'.")
            .When(x => !string.IsNullOrEmpty(x.SortOrder));

        RuleFor(x => x.MinAmount).GreaterThanOrEqualTo(0).When(x => x.MinAmount.HasValue);
        RuleFor(x => x.MaxAmount).GreaterThanOrEqualTo(x => x.MinAmount.Value)
            .WithMessage("Maximum amount must be greater than or equal to minimum amount.")
            .When(x => x.MaxAmount.HasValue && x.MinAmount.HasValue);

        RuleFor(x => x.DateFrom).LessThanOrEqualTo(x => x.DateTo.Value)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);
    }
}