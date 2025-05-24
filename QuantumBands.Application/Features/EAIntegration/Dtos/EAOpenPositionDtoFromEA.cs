// QuantumBands.Application/Features/EAIntegration/Commands/PushLiveData/EAOpenPositionDtoFromEA.cs
namespace QuantumBands.Application.Features.EAIntegration.Dtos;

public class EAOpenPositionDtoFromEA
{
    public required string EaTicketId { get; set; }
    public required string Symbol { get; set; }
    public required string TradeType { get; set; } // "Buy" or "Sell"
    public decimal VolumeLots { get; set; }
    public decimal OpenPrice { get; set; }
    public DateTime OpenTime { get; set; } // Nên là DateTime UTC
    public decimal CurrentMarketPrice { get; set; }
    public decimal Swap { get; set; }
    public decimal Commission { get; set; }
    public decimal FloatingPAndL { get; set; }
}