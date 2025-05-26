// QuantumBands.Application/Features/Admin/TradingAccounts/Commands/CancelInitialShareOfferingRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Admin.TradingAccounts.Commands;

public class CancelInitialShareOfferingRequestValidator : AbstractValidator<CancelInitialShareOfferingRequest>
{
    public CancelInitialShareOfferingRequestValidator()
    {
        RuleFor(x => x.AdminNotes)
            .MaximumLength(500).WithMessage("Admin notes cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.AdminNotes));
    }
}