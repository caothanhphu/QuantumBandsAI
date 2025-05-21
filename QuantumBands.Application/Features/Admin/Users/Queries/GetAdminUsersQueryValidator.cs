// QuantumBands.Application/Features/Admin/Users/Queries/GetAdminUsersQueryValidator.cs
using FluentValidation;
using System;
using System.Collections.Generic;

namespace QuantumBands.Application.Features.Admin.Users.Queries;

public class GetAdminUsersQueryValidator : AbstractValidator<GetAdminUsersQuery>
{
    private readonly List<string> _allowedSortByFields = new List<string>
    {
        "userid", "username", "email", "fullname", "rolename",
        "isactive", "isemailverified", "createdat"
    };

    public GetAdminUsersQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

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

        RuleFor(x => x.RoleId)
            .GreaterThan(0).WithMessage("Role ID must be a positive number.")
            .When(x => x.RoleId.HasValue);

        RuleFor(x => x.DateFrom)
            .LessThanOrEqualTo(x => x.DateTo.Value)
            .WithMessage("DateFrom must be earlier than or equal to DateTo.")
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);
    }
}