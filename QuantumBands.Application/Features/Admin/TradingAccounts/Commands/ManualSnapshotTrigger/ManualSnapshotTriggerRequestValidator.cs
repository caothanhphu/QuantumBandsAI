using FluentValidation;

namespace QuantumBands.Application.Features.Admin.TradingAccounts.Commands.ManualSnapshotTrigger;

public class ManualSnapshotTriggerRequestValidator : AbstractValidator<ManualSnapshotTriggerRequest>
{
    public ManualSnapshotTriggerRequestValidator()
    {
        RuleFor(x => x.TargetDate)
            .NotEmpty()
            .WithMessage("Target date is required")
            .Must(date => date.Date <= DateTime.UtcNow.Date)
            .WithMessage("Cannot trigger snapshot for future dates");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required for audit purposes")
            .Length(10, 500)
            .WithMessage("Reason must be between 10 and 500 characters");

        RuleFor(x => x.AdminNotes)
            .MaximumLength(1000)
            .WithMessage("Admin notes cannot exceed 1000 characters");

        RuleFor(x => x.TradingAccountIds)
            .Must(ids => ids == null || ids.All(id => id > 0))
            .WithMessage("All trading account IDs must be positive numbers");
    }
} 