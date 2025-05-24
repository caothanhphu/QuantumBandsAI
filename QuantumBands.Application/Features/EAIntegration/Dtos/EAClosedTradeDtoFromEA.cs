// QuantumBands.Application/Features/EAIntegration/Commands/PushClosedTrades/EAClosedTradeDtoFromEA.cs
namespace QuantumBands.Application.Features.EAIntegration.Commands.PushClosedTrades;

public class EAClosedTradeDtoFromEA
{
    public required string EaTicketId { get; set; }
    public required string Symbol { get; set; }
    public required string TradeType { get; set; } // "Buy" or "Sell"
    public decimal VolumeLots { get; set; }
    public decimal OpenPrice { get; set; }
    public DateTime OpenTime { get; set; } // Nên là DateTime UTC
    public decimal ClosePrice { get; set; }
    public DateTime CloseTime { get; set; } // Nên là DateTime UTC
    public decimal Swap { get; set; }
    public decimal Commission { get; set; }
    public decimal RealizedPAndL { get; set; }
}