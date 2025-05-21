// QuantumBands.Application/Features/Admin/TradingAccounts/Commands/CreateInitialShareOfferingRequestValidator.cs
using FluentValidation;
using System;
namespace QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
public class CreateInitialShareOfferingRequestValidator : AbstractValidator<CreateInitialShareOfferingRequest>
{
    public CreateInitialShareOfferingRequestValidator()
    {
        RuleFor(x => x.SharesOffered)
            .GreaterThan(0).WithMessage("Shares offered must be greater than 0.");
        RuleFor(x => x.OfferingPricePerShare)
            .GreaterThan(0).WithMessage("Offering price per share must be greater than 0.");
        RuleFor(x => x.FloorPricePerShare)
            .GreaterThan(0).WithMessage("Floor price per share must be greater than 0.")
            .LessThanOrEqualTo(x => x.OfferingPricePerShare).WithMessage("Floor price must be less than or equal to offering price.")
            .When(x => x.FloorPricePerShare.HasValue);
        RuleFor(x => x.CeilingPricePerShare)
            .GreaterThan(0).WithMessage("Ceiling price per share must be greater than 0.")
            .GreaterThanOrEqualTo(x => x.OfferingPricePerShare).WithMessage("Ceiling price must be greater than or equal to offering price.")
            .When(x => x.CeilingPricePerShare.HasValue);
        RuleFor(x => x.OfferingEndDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Offering end date must be in the future.")
            .When(x => x.OfferingEndDate.HasValue);
    }
}