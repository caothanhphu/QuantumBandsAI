using FluentValidation;
using System;
using System.Collections.Generic; // For List
namespace QuantumBands.Application.Features.Wallets.Queries.GetTransactions;


public class GetAdminPendingBankDepositsQueryValidator : AbstractValidator<GetAdminPendingBankDepositsQuery>
{
    private readonly List<string> _allowedSortByFields = new List<string>
    {
        "transactiondate", // Case-insensitive comparison will be used
        "amount",
        "userid",
        "referenceid"
    };

    public GetAdminPendingBankDepositsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("Page size must be at least 1.")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100."); // Assuming MaxPageSize is 100 from DTO

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

        RuleFor(x => x.ReferenceCode)
            .MaximumLength(100).WithMessage("Reference code cannot exceed 100 characters.") // Giả sử độ dài tối đa cho ReferenceID
            .When(x => !string.IsNullOrEmpty(x.ReferenceCode));

        RuleFor(x => x.MinAmountUSD)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum amount cannot be negative.")
            .When(x => x.MinAmountUSD.HasValue);

        RuleFor(x => x.MaxAmountUSD)
            .GreaterThanOrEqualTo(0).WithMessage("Maximum amount cannot be negative.")
            .GreaterThanOrEqualTo(x => x.MinAmountUSD.Value)
                .WithMessage("Maximum amount must be greater than or equal to minimum amount.")
                .When(x => x.MaxAmountUSD.HasValue && x.MinAmountUSD.HasValue);

        RuleFor(x => x.DateFrom)
            .LessThanOrEqualTo(x => x.DateTo.Value)
                .WithMessage("DateFrom must be earlier than or equal to DateTo.")
                .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);

        // (Tùy chọn) Kiểm tra định dạng ngày nếu bạn nhận DateFrom/DateTo là string và parse sau
        // Nếu chúng đã là DateTime? thì .NET model binding đã xử lý việc parse rồi.
    }
}