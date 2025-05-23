// QuantumBands.Application/Features/TradingAccounts/Dtos/EAOpenPositionDto.cs
namespace QuantumBands.Application.Features.TradingAccounts.Dtos;
public class EAOpenPositionDto
{
    public long OpenPositionId { get; set; }
    public required string EaTicketId { get; set; }
    public required string Symbol { get; set; }
    public required string TradeType { get; set; }
    public decimal VolumeLots { get; set; }
    public decimal OpenPrice { get; set; }
    public DateTime OpenTime { get; set; }
    public decimal? CurrentMarketPrice { get; set; }
    public decimal Swap { get; set; }
    public decimal Commission { get; set; }
    public decimal? FloatingPAndL { get; set; }
    public DateTime LastUpdateTime { get; set; }
}