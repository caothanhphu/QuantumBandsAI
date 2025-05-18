using FluentValidation;
using System;
using System.Collections.Generic;

namespace QuantumBands.Application.Features.Wallets.Queries.GetTransactions;
public class GetAdminPendingWithdrawalsQueryValidator : AbstractValidator<GetAdminPendingWithdrawalsQuery>
{
    private readonly List<string> _allowedSortByFields = new List<string>
    {
        "requestedat", // Sẽ map sang TransactionDate
        "amount",
        "userid",
        "username",
        "useremail"
    };

    public GetAdminPendingWithdrawalsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("Page size must be at least 1.")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100.");

        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrEmpty(sortBy) || _allowedSortByFields.Contains(sortBy.ToLowerInvariant()))
            .WithMessage(x => $"SortBy field '{x.SortBy}' is not allowed. Allowed fields are: {string.Join(", ", _allowedSortByFields)}.")
            .When(x => !string.IsNullOrEmpty(x.SortBy));

        RuleFor(x => x.SortOrder)
            .Must(sortOrder => string.IsNullOrEmpty(sortOrder) || sortOrder.ToLowerInvariant() == "asc" || sortOrder.ToLowerInvariant() == "desc")
            .WithMessage("SortOrder must be 'asc' or 'desc'.")
            .When(x => !string.IsNullOrEmpty(x.SortOrder));

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID must be a positive number.")
            .When(x => x.UserId.HasValue);

        RuleFor(x => x.UsernameOrEmail)
            .MaximumLength(255).WithMessage("Username or Email search term cannot exceed 255 characters.")
            .When(x => !string.IsNullOrEmpty(x.UsernameOrEmail));

        RuleFor(x => x.MinAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum amount cannot be negative.")
            .When(x => x.MinAmount.HasValue);

        RuleFor(x => x.MaxAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Maximum amount cannot be negative.")
            .GreaterThanOrEqualTo(x => x.MinAmount.Value)
                .WithMessage("Maximum amount must be greater than or equal to minimum amount.")
                .When(x => x.MaxAmount.HasValue && x.MinAmount.HasValue);

        RuleFor(x => x.DateFrom)
            .LessThanOrEqualTo(x => x.DateTo.Value)
                .WithMessage("DateFrom must be earlier than or equal to DateTo.")
                .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);
    }
}