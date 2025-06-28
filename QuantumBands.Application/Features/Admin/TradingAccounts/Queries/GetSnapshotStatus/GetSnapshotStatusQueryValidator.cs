using FluentValidation;

namespace QuantumBands.Application.Features.Admin.TradingAccounts.Queries.GetSnapshotStatus;

public class GetSnapshotStatusQueryValidator : AbstractValidator<GetSnapshotStatusQuery>
{
    public GetSnapshotStatusQueryValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Date is required")
            .Must(date => date.Date <= DateTime.UtcNow.Date)
            .WithMessage("Cannot get snapshot status for future dates");

        RuleFor(x => x.TradingAccountIds)
            .Must(ids => ids == null || ids.All(id => id > 0))
            .WithMessage("All trading account IDs must be positive numbers");
    }
}