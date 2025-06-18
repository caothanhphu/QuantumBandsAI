// QuantumBands.Application/Features/TradingAccounts/Dtos/OpenPositionsRealtimeDto.cs
// SCRUM-98: Open Positions Real-time API response DTOs with comprehensive position details,
// summary metrics, and market data for real-time trading position monitoring
namespace QuantumBands.Application.Features.TradingAccounts.Dtos;

public class OpenPositionsRealtimeDto
{
    public required List<OpenPositionDetailDto> Positions { get; set; }
    public required PositionsSummaryDto Summary { get; set; }
    public required MarketDataDto MarketData { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class OpenPositionDetailDto
{
    public long OpenPositionId { get; set; }
    public required string EaTicketId { get; set; }
    public required string Symbol { get; set; }
    public required string TradeType { get; set; }
    public decimal VolumeLots { get; set; }
    public decimal OpenPrice { get; set; }
    public DateTime OpenTime { get; set; }
    public decimal CurrentMarketPrice { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal Swap { get; set; }
    public decimal Commission { get; set; }
    public decimal MarginRequired { get; set; }
    public decimal PercentageReturn { get; set; }
    public int DaysOpen { get; set; }
    public DateTime LastUpdateTime { get; set; }
}

public class PositionsSummaryDto
{
    public int TotalPositions { get; set; }
    public decimal TotalUnrealizedPnL { get; set; }
    public decimal TotalMarginUsed { get; set; }
    public decimal FreeMargin { get; set; }
    public decimal MarginLevel { get; set; }
    public decimal TotalVolume { get; set; }
    public int LongPositions { get; set; }
    public int ShortPositions { get; set; }
    public decimal LongVolume { get; set; }
    public decimal ShortVolume { get; set; }
    public decimal DailyPnL { get; set; }
    public decimal WeeklyPnL { get; set; }
    public decimal MonthlyPnL { get; set; }
}

public class MarketDataDto
{
    public DateTime LastPriceUpdate { get; set; }
    public required List<SymbolQuoteDto> Quotes { get; set; }
    public decimal AccountEquity { get; set; }
    public decimal AccountBalance { get; set; }
    public decimal DrawdownPercent { get; set; }
}

public class SymbolQuoteDto
{
    public required string Symbol { get; set; }
    public decimal Bid { get; set; }
    public decimal Ask { get; set; }
    public decimal Spread { get; set; }
    public decimal DailyChange { get; set; }
    public decimal DailyChangePercent { get; set; }
    public DateTime LastUpdate { get; set; }
}