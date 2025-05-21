// QuantumBands.Application/Features/TradingAccounts/Queries/GetPublicTradingAccountsQueryValidator.cs
using FluentValidation;
using System.Collections.Generic;

namespace QuantumBands.Application.Features.TradingAccounts.Queries;

public class GetPublicTradingAccountsQueryValidator : AbstractValidator<GetPublicTradingAccountsQuery>
{
    private readonly List<string> _allowedSortByFields = new List<string>
    {
        "accountname", "currentshareprice", "managementfeerate", "createdat"
    };

    public GetPublicTradingAccountsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrEmpty(sortBy) || _allowedSortByFields.Contains(sortBy.ToLowerInvariant()))
            .WithMessage(x => $"SortBy field '{x.SortBy}' is not allowed. Allowed fields are: {string.Join(", ", _allowedSortByFields)}.")
            .When(x => !string.IsNullOrEmpty(x.SortBy));

        RuleFor(x => x.SortOrder)
            .Must(sortOrder => string.IsNullOrEmpty(sortOrder) || sortOrder.ToLowerInvariant() == "asc" || sortOrder.ToLowerInvariant() == "desc")
            .WithMessage("SortOrder must be 'asc' or 'desc'.")
            .When(x => !string.IsNullOrEmpty(x.SortOrder));

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100).WithMessage("Search term cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.SearchTerm));
    }
}