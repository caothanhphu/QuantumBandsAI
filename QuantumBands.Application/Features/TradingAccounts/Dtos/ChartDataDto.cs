// QuantumBands.Application/Features/TradingAccounts/Dtos/ChartDataDto.cs
using QuantumBands.Application.Features.TradingAccounts.Enums;

namespace QuantumBands.Application.Features.TradingAccounts.Dtos;

/// <summary>
/// Represents chart data response for trading account performance visualization
/// </summary>
public class ChartDataDto
{
    /// <summary>
    /// Type of chart data (balance, equity, growth, drawdown)
    /// </summary>
    public required ChartType ChartType { get; set; }
    
    /// <summary>
    /// Time period covered by the data
    /// </summary>
    public required TimePeriod Period { get; set; }
    
    /// <summary>
    /// Data aggregation interval
    /// </summary>
    public required DataInterval Interval { get; set; }
    
    /// <summary>
    /// Array of data points for the chart
    /// </summary>
    public required List<ChartDataPointDto> DataPoints { get; set; } = new();
    
    /// <summary>
    /// Summary statistics for the chart data
    /// </summary>
    public required ChartSummaryDto Summary { get; set; }
}

/// <summary>
/// Individual data point in a chart
/// </summary>
public class ChartDataPointDto
{
    /// <summary>
    /// Timestamp for this data point (ISO 8601 format)
    /// </summary>
    public required DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Primary value for the chart type
    /// </summary>
    public required decimal Value { get; set; }
    
    /// <summary>
    /// Additional metadata for this data point
    /// </summary>
    public ChartDataMetadataDto? Metadata { get; set; }
}

/// <summary>
/// Metadata accompanying each chart data point
/// </summary>
public class ChartDataMetadataDto
{
    /// <summary>
    /// Account balance at this point in time
    /// </summary>
    public decimal? Balance { get; set; }
    
    /// <summary>
    /// Account equity (balance + floating P/L) at this point
    /// </summary>
    public decimal? Equity { get; set; }
    
    /// <summary>
    /// Number of open positions at this point
    /// </summary>
    public int? OpenPositions { get; set; }
    
    /// <summary>
    /// Daily profit/loss for this point
    /// </summary>
    public decimal? DailyProfit { get; set; }
}

/// <summary>
/// Summary statistics for chart data
/// </summary>
public class ChartSummaryDto
{
    /// <summary>
    /// Starting value for the time period
    /// </summary>
    public required decimal StartValue { get; set; }
    
    /// <summary>
    /// Ending value for the time period
    /// </summary>
    public required decimal EndValue { get; set; }
    
    /// <summary>
    /// Absolute change from start to end
    /// </summary>
    public required decimal ChangeAbsolute { get; set; }
    
    /// <summary>
    /// Percentage change from start to end
    /// </summary>
    public required decimal ChangePercent { get; set; }
    
    /// <summary>
    /// Maximum value during the period
    /// </summary>
    public required decimal MaxValue { get; set; }
    
    /// <summary>
    /// Minimum value during the period
    /// </summary>
    public required decimal MinValue { get; set; }
    
    /// <summary>
    /// Total number of data points
    /// </summary>
    public required int TotalDataPoints { get; set; }
}