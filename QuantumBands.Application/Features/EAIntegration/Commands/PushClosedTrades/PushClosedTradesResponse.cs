// QuantumBands.Application/Features/EAIntegration/Commands/PushClosedTrades/PushClosedTradesResponse.cs
namespace QuantumBands.Application.Features.EAIntegration.Commands.PushClosedTrades;

public class PushClosedTradesResponse
{
    public required string Message { get; set; }
    public int TradingAccountId { get; set; }
    public int TradesReceived { get; set; }
    public int TradesAdded { get; set; }
    public int TradesSkipped { get; set; }
}