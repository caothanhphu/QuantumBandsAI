namespace QuantumBands.Application.Features.TradingAccounts.Dtos;

public class TradingHistoryDto
{
    public int ClosedTradeId { get; set; }
    public long EaTicketId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string TradeType { get; set; } = string.Empty;
    public decimal VolumeLots { get; set; }
    public decimal OpenPrice { get; set; }
    public DateTime OpenTime { get; set; }
    public decimal ClosePrice { get; set; }
    public DateTime CloseTime { get; set; }
    public decimal Swap { get; set; }
    public decimal Commission { get; set; }
    public decimal RealizedPandL { get; set; }
    public DateTime RecordedAt { get; set; }
    
    // Additional computed fields for enhanced trading history
    public decimal NetProfit => RealizedPandL - Commission - Swap;
    public string Duration { get; set; } = string.Empty;
    public decimal Pips { get; set; }
    public decimal VolumeInUnits { get; set; }
    public string Comment { get; set; } = string.Empty;
    public int MagicNumber { get; set; }
    public decimal? StopLoss { get; set; }
    public decimal? TakeProfit { get; set; }
}