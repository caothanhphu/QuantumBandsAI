// QuantumBands.Application/Features/Admin/TradingAccounts/Commands/UpdateInitialShareOfferingRequestValidator.cs
using FluentValidation;
using QuantumBands.Domain.Entities.Enums; // For OfferingStatus enum
using System;
using System.Linq;

namespace QuantumBands.Application.Features.Admin.TradingAccounts.Commands;

public class UpdateInitialShareOfferingRequestValidator : AbstractValidator<UpdateInitialShareOfferingRequest>
{
    private readonly List<string> _allowedStatusValues = System.Enum.GetNames(typeof(OfferingStatus))
                                                                .Select(s => s.ToLowerInvariant())
                                                                .ToList();
    public UpdateInitialShareOfferingRequestValidator()
    {
        RuleFor(x => x.SharesOffered)
            .GreaterThan(0).WithMessage("Shares offered must be greater than 0.")
            .When(x => x.SharesOffered.HasValue);

        RuleFor(x => x.OfferingPricePerShare)
            .GreaterThan(0).WithMessage("Offering price per share must be greater than 0.")
            .When(x => x.OfferingPricePerShare.HasValue);

        RuleFor(x => x.FloorPricePerShare)
            .GreaterThan(0).WithMessage("Floor price per share must be greater than 0.")
            .LessThanOrEqualTo(x => x.OfferingPricePerShare.Value)
                .WithMessage("Floor price must be less than or equal to offering price.")
                .When(x => x.FloorPricePerShare.HasValue && x.OfferingPricePerShare.HasValue)
            .LessThanOrEqualTo(x => x.CeilingPricePerShare.Value)
                .WithMessage("Floor price must be less than or equal to ceiling price.")
                .When(x => x.FloorPricePerShare.HasValue && x.CeilingPricePerShare.HasValue);


        RuleFor(x => x.CeilingPricePerShare)
            .GreaterThan(0).WithMessage("Ceiling price per share must be greater than 0.")
            .GreaterThanOrEqualTo(x => x.OfferingPricePerShare.Value)
                .WithMessage("Ceiling price must be greater than or equal to offering price.")
                .When(x => x.CeilingPricePerShare.HasValue && x.OfferingPricePerShare.HasValue)
            .GreaterThanOrEqualTo(x => x.FloorPricePerShare.Value)
                .WithMessage("Ceiling price must be greater than or equal to floor price.")
                .When(x => x.CeilingPricePerShare.HasValue && x.FloorPricePerShare.HasValue);

        RuleFor(x => x.OfferingEndDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Offering end date must be in the future.")
            .When(x => x.OfferingEndDate.HasValue);

        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrEmpty(status) || _allowedStatusValues.Contains(status.ToLowerInvariant()))
            .WithMessage(x => $"Status field '{x.Status}' is not allowed. Allowed values are: {string.Join(", ", System.Enum.GetNames(typeof(OfferingStatus)))}.")
            .When(x => !string.IsNullOrEmpty(x.Status));

        // Rule to ensure at least one field is provided for update
        RuleFor(x => x)
            .Must(request => request.SharesOffered.HasValue ||
                             request.OfferingPricePerShare.HasValue ||
                             request.FloorPricePerShare.HasValue ||
                             request.CeilingPricePerShare.HasValue ||
                             request.OfferingEndDate.HasValue ||
                             !string.IsNullOrEmpty(request.Status))
            .WithMessage("At least one field must be provided to update the offering.");
    }
}