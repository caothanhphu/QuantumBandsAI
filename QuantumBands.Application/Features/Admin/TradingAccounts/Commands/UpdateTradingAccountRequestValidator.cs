// QuantumBands.Application/Features/Admin/TradingAccounts/Commands/UpdateTradingAccountRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Admin.TradingAccounts.Commands;

public class UpdateTradingAccountRequestValidator : AbstractValidator<UpdateTradingAccountRequest>
{    public UpdateTradingAccountRequestValidator()
    {
        // Các trường đều là optional, chỉ validate nếu được cung cấp giá trị
        RuleFor(x => x.AccountName)
            .NotEmpty().WithMessage("Account name cannot be empty.")
            .MaximumLength(100).WithMessage("Account name cannot exceed 100 characters.")
            .When(x => x.AccountName != null); // Validate khi AccountName được cung cấp (không phải là null)

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.")
            .When(x => x.Description != null); // Validate khi Description được cung cấp (không phải là null)

        RuleFor(x => x.EaName)
            .MaximumLength(100).WithMessage("EA name cannot exceed 100 characters.")
            .When(x => x.EaName != null);

        RuleFor(x => x.ManagementFeeRate)
            .InclusiveBetween(0, 0.9999m).WithMessage("Management fee rate must be between 0 and 0.9999 (e.g., 0.02 for 2%).")
            .When(x => x.ManagementFeeRate.HasValue);

        // Không cần rule cụ thể cho IsActive (boolean?) trừ khi có logic đặc biệt
    }
}