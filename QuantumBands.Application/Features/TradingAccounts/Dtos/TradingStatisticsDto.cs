// QuantumBands.Application/Features/TradingAccounts/Dtos/TradingStatisticsDto.cs
using QuantumBands.Application.Features.TradingAccounts.Enums;

namespace QuantumBands.Application.Features.TradingAccounts.Dtos;

public class TradingStatisticsDto
{
    public required string Period { get; set; }
    public required DateRangeDto DateRange { get; set; }
    public required TradingStatsDto TradingStats { get; set; }
    public required FinancialStatsDto FinancialStats { get; set; }
    public required RiskMetricsDto RiskMetrics { get; set; }
    public AdvancedMetricsDto? AdvancedMetrics { get; set; }
    public required List<SymbolBreakdownDto> SymbolBreakdown { get; set; } = new();
    public required List<MonthlyPerformanceDto> MonthlyPerformance { get; set; } = new();
}

public class DateRangeDto
{
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required int TotalDays { get; set; }
    public required int TradingDays { get; set; }
}

public class TradingStatsDto
{
    public required int TotalTrades { get; set; }
    public required TradeCountDto ProfitableTrades { get; set; }
    public required TradeCountDto LosingTrades { get; set; }
    public required TradeCountDto BreakEvenTrades { get; set; }
    public required decimal BestTrade { get; set; }
    public required decimal WorstTrade { get; set; }
    public required decimal AverageProfit { get; set; }
    public required decimal AverageLoss { get; set; }
    public required decimal LargestProfitTrade { get; set; }
    public required decimal LargestLossTrade { get; set; }
    public required int MaxConsecutiveWins { get; set; }
    public required int MaxConsecutiveLosses { get; set; }
    public required string AverageTradeDuration { get; set; }
    public required decimal TradesPerDay { get; set; }
    public required decimal TradesPerWeek { get; set; }
    public required decimal TradesPerMonth { get; set; }
}

public class TradeCountDto
{
    public required int Count { get; set; }
    public required decimal Percentage { get; set; }
}

public class FinancialStatsDto
{
    public required decimal GrossProfit { get; set; }
    public required decimal GrossLoss { get; set; }
    public required decimal TotalNetProfit { get; set; }
    public required decimal ProfitFactor { get; set; }
    public required decimal ExpectedPayoff { get; set; }
    public required decimal AverageTradeNetProfit { get; set; }
    public required decimal ReturnOnInvestment { get; set; }
    public required decimal AnnualizedReturn { get; set; }
    public required decimal TotalCommission { get; set; }
    public required decimal TotalSwap { get; set; }
    public required decimal NetProfitAfterCosts { get; set; }
}

public class RiskMetricsDto
{
    public required MaxDrawdownInfoDto MaxDrawdown { get; set; }
    public required decimal AverageDrawdown { get; set; }
    public required decimal CalmarRatio { get; set; }
    public required decimal MaxDailyLoss { get; set; }
    public required decimal MaxDailyProfit { get; set; }
    public required decimal AverageDailyPL { get; set; }
    public required decimal Volatility { get; set; }
    public required decimal StandardDeviation { get; set; }
    public required decimal DownsideDeviation { get; set; }
    public required decimal RiskOfRuin { get; set; }
    public required decimal WinLossRatio { get; set; }
    public required decimal PayoffRatio { get; set; }
}

public class MaxDrawdownInfoDto
{
    public required decimal Amount { get; set; }
    public required decimal Percentage { get; set; }
    public required DateTime Date { get; set; }
    public required string Duration { get; set; }
    public required string RecoveryTime { get; set; }
}

public class AdvancedMetricsDto
{
    public required decimal SharpeRatio { get; set; }
    public required decimal SortinoRatio { get; set; }
    public required decimal InformationRatio { get; set; }
    public required decimal TreynorRatio { get; set; }
    public required decimal Alpha { get; set; }
    public required decimal Beta { get; set; }
    public required decimal RSquared { get; set; }
    public required decimal TrackingError { get; set; }
    public required decimal ValueAtRisk95 { get; set; }
    public required decimal ValueAtRisk99 { get; set; }
    public required decimal ConditionalVaR { get; set; }
    public required decimal MaxLeverageUsed { get; set; }
    public required decimal AverageLeverage { get; set; }
}

public class SymbolBreakdownDto
{
    public required string Symbol { get; set; }
    public required int Trades { get; set; }
    public required decimal NetProfit { get; set; }
    public required decimal WinRate { get; set; }
    public required decimal ProfitFactor { get; set; }
    public required string AverageHoldTime { get; set; }
}

public class MonthlyPerformanceDto
{
    public required int Year { get; set; }
    public required int Month { get; set; }
    public required int Trades { get; set; }
    public required decimal NetProfit { get; set; }
    public required decimal WinRate { get; set; }
    public required decimal MaxDrawdown { get; set; }
}