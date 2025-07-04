﻿// QuantumBands.Application/Features/Admin/ExchangeMonitor/Queries/GetAdminAllOrdersQueryValidator.cs
using FluentValidation;
using QuantumBands.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantumBands.Application.Features.Admin.ExchangeMonitor.Queries;

public class GetAdminAllOrdersQueryValidator : AbstractValidator<GetAdminAllOrdersQuery>
{
    private readonly List<string> _allowedSortByFields = new List<string>
    {
        "orderdate", "username", "tradingaccountname", "quantityordered", "limitprice", "status", "userid"
    };
    private readonly List<string> _allowedStatusValues = Enum.GetNames(typeof(ShareOrderStatusName)).Select(s => s.ToLowerInvariant()).ToList();
    private readonly List<string> _allowedOrderSides = new List<string> { "buy", "sell" };
    private readonly List<string> _allowedOrderTypes = new List<string> { "market", "limit" };

    public GetAdminAllOrdersQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.TradingAccountId).GreaterThan(0).When(x => x.TradingAccountId.HasValue);
        RuleFor(x => x.UserId).GreaterThan(0).When(x => x.UserId.HasValue);

        RuleFor(x => x.Status)
            .Must(statusString => {
                if (string.IsNullOrEmpty(statusString)) return true;
                var statuses = statusString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return statuses.All(s => _allowedStatusValues.Contains(s.ToLowerInvariant()));
            })
            .WithMessage($"Invalid status value(s) provided. Allowed statuses are: {string.Join(", ", Enum.GetNames(typeof(ShareOrderStatusName)))}.")
            .When(x => !string.IsNullOrEmpty(x.Status));

        RuleFor(x => x.OrderSide)
            .Must(side => string.IsNullOrEmpty(side) || _allowedOrderSides.Contains(side.ToLowerInvariant()))
            .WithMessage("OrderSide must be 'Buy' or 'Sell'.")
            .When(x => !string.IsNullOrEmpty(x.OrderSide));

        RuleFor(x => x.OrderType)
           .Must(type => string.IsNullOrEmpty(type) || _allowedOrderTypes.Contains(type.ToLowerInvariant()))
           .WithMessage("OrderType must be 'Market' or 'Limit'.")
           .When(x => !string.IsNullOrEmpty(x.OrderType));

        RuleFor(x => x.DateFrom).LessThanOrEqualTo(x => x.DateTo.Value)
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