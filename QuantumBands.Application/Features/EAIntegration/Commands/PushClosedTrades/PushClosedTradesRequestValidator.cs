// QuantumBands.Application/Features/EAIntegration/Commands/PushClosedTrades/PushClosedTradesRequestValidator.cs
using FluentValidation;
using System.Linq;

namespace QuantumBands.Application.Features.EAIntegration.Commands.PushClosedTrades;

public class PushClosedTradesRequestValidator : AbstractValidator<PushClosedTradesRequest>
{
    public PushClosedTradesRequestValidator()
    {
        RuleFor(x => x.ClosedTrades)
            .NotNull().WithMessage("Closed trades list cannot be null.")
            .NotEmpty().WithMessage("Closed trades list cannot be empty when provided."); // Nếu script luôn gửi list, dù rỗng

        RuleForEach(x => x.ClosedTrades).SetValidator(new EAClosedTradeDtoFromEAValidator());
    }
}

public class EAClosedTradeDtoFromEAValidator : AbstractValidator<EAClosedTradeDtoFromEA>
{
    private readonly List<string> _allowedTradeTypes = new List<string> { "buy", "sell" };

    public EAClosedTradeDtoFromEAValidator()
    {
        RuleFor(x => x.EaTicketId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(20);
        RuleFor(x => x.TradeType)
            .NotEmpty()
            .Must(tt => _allowedTradeTypes.Contains(tt.ToLowerInvariant()))
            .WithMessage("TradeType must be 'Buy' or 'Sell'.");
        RuleFor(x => x.VolumeLots).GreaterThan(0);
        RuleFor(x => x.OpenPrice).GreaterThan(0);
        RuleFor(x => x.OpenTime).NotEmpty().LessThan(x => x.CloseTime)
            .WithMessage("Open time must be before close time and not empty.");
        RuleFor(x => x.ClosePrice).GreaterThan(0);
        //RuleFor(x => x.CloseTime).NotEmpty().LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5)) // Không quá xa tương lai
        //    .WithMessage("Close time cannot be in the distant future and must be after open time.");
        // RealizedPAndL, Swap, Commission có thể âm, dương hoặc 0
    }
}