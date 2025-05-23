// QuantumBands.Application/Features/TradingAccounts/Dtos/EAClosedTradeDto.cs
namespace QuantumBands.Application.Features.TradingAccounts.Dtos;
public class EAClosedTradeDto
{
    public long ClosedTradeId { get; set; }
    public required string EaTicketId { get; set; }
    public required string Symbol { get; set; }
    public required string TradeType { get; set; }
    public decimal VolumeLots { get; set; }
    public decimal OpenPrice { get; set; }
    public DateTime OpenTime { get; set; }
    public decimal ClosePrice { get; set; }
    public DateTime CloseTime { get; set; }
    public decimal Swap { get; set; }
    public decimal Commission { get; set; }
    public decimal RealizedPAndL { get; set; }
    public DateTime RecordedAt { get; set; }
}