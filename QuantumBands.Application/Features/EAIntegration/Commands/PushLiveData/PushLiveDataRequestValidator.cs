// QuantumBands.Application/Features/EAIntegration/Commands/PushLiveData/PushLiveDataRequestValidator.cs
using FluentValidation;
using QuantumBands.Application.Features.EAIntegration.Dtos;
using System.Linq;

namespace QuantumBands.Application.Features.EAIntegration.Commands.PushLiveData;

public class PushLiveDataRequestValidator : AbstractValidator<PushLiveDataRequest>
{
    public PushLiveDataRequestValidator()
    {
        RuleFor(x => x.AccountEquity)
            .NotEmpty().WithMessage("Account equity is required.");
        // .GreaterThan(0).WithMessage("Account equity must be positive."); // Có thể âm nếu tài khoản lỗ nặng

        RuleFor(x => x.AccountBalance)
            .NotEmpty().WithMessage("Account balance is required.");

        RuleFor(x => x.OpenPositions)
            .NotNull().WithMessage("Open positions list cannot be null (can be empty).");

        RuleForEach(x => x.OpenPositions).SetValidator(new EAOpenPositionDtoFromEAValidator());
    }
}

public class EAOpenPositionDtoFromEAValidator : AbstractValidator<EAOpenPositionDtoFromEA>
{
    private readonly List<string> _allowedTradeTypes = new List<string> { "buy", "sell" };

    public EAOpenPositionDtoFromEAValidator()
    {
        RuleFor(x => x.EaTicketId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(20);
        RuleFor(x => x.TradeType)
            .NotEmpty()
            .Must(tt => _allowedTradeTypes.Contains(tt.ToLowerInvariant()))
            .WithMessage("TradeType must be 'Buy' or 'Sell'.");
        RuleFor(x => x.VolumeLots).GreaterThan(0);
        RuleFor(x => x.OpenPrice).GreaterThan(0);
        //RuleFor(x => x.OpenTime).NotEmpty().LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(15)); // Allow 15 minutes for clock skew
        RuleFor(x => x.CurrentMarketPrice).GreaterThanOrEqualTo(0);
        // Swap, Commission, FloatingPAndL có thể âm, dương hoặc 0
    }
}