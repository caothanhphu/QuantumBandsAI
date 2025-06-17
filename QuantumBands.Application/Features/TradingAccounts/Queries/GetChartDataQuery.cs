// QuantumBands.Application/Features/TradingAccounts/Queries/GetChartDataQuery.cs
using QuantumBands.Application.Features.TradingAccounts.Enums;

namespace QuantumBands.Application.Features.TradingAccounts.Queries;

/// <summary>
/// Query parameters for requesting chart data
/// </summary>
public class GetChartDataQuery
{
    /// <summary>
    /// Type of chart to generate (required)
    /// </summary>
    public required ChartType Type { get; set; }
    
    /// <summary>
    /// Time period for the chart data (default: ALL)
    /// </summary>
    public TimePeriod Period { get; set; } = TimePeriod.All;
    
    /// <summary>
    /// Data aggregation interval (default: daily)
    /// </summary>
    public DataInterval Interval { get; set; } = DataInterval.Daily;
}