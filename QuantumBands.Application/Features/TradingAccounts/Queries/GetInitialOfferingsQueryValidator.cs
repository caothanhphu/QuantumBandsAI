// QuantumBands.Application/Features/TradingAccounts/Queries/GetInitialOfferingsQueryValidator.cs
using FluentValidation;
using QuantumBands.Domain.Entities; // For OfferingStatus enum
using QuantumBands.Domain.Entities.Enums;
using System.Collections.Generic;
using System.Linq;

namespace QuantumBands.Application.Features.TradingAccounts.Queries;

public class GetInitialOfferingsQueryValidator : AbstractValidator<GetInitialOfferingsQuery>
{
    private readonly List<string> _allowedSortByFields = new List<string>
    {
        "offeringstartdate", "offeringpricepershare", "sharesoffered"
    };

    private readonly List<string> _allowedStatusValues = System.Enum.GetNames(typeof(OfferingStatus))
                                                                .Select(s => s.ToLowerInvariant())
                                                                .ToList();

    public GetInitialOfferingsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Page size must be between 1 and 50.");

        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrEmpty(sortBy) || _allowedSortByFields.Contains(sortBy.ToLowerInvariant()))
            .WithMessage(x => $"SortBy field '{x.SortBy}' is not allowed. Allowed fields are: {string.Join(", ", _allowedSortByFields)}.")
            .When(x => !string.IsNullOrEmpty(x.SortBy));

        RuleFor(x => x.SortOrder)
            .Must(sortOrder => string.IsNullOrEmpty(sortOrder) || sortOrder.ToLowerInvariant() == "asc" || sortOrder.ToLowerInvariant() == "desc")
            .WithMessage("SortOrder must be 'asc' or 'desc'.")
            .When(x => !string.IsNullOrEmpty(x.SortOrder));

        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrEmpty(status) || _allowedStatusValues.Contains(status.ToLowerInvariant()))
            .WithMessage(x => $"Status field '{x.Status}' is not allowed. Allowed values are: {string.Join(", ", System.Enum.GetNames(typeof(OfferingStatus)))}.")
            .When(x => !string.IsNullOrEmpty(x.Status));
    }
}