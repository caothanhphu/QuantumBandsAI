// QuantumBands.Application/Features/TradingAccounts/Enums/ChartEnums.cs
using System.ComponentModel;

namespace QuantumBands.Application.Features.TradingAccounts.Enums;

/// <summary>
/// Types of charts available for trading account performance
/// </summary>
public enum ChartType
{
    /// <summary>
    /// Account balance progression over time
    /// </summary>
    [Description("balance")]
    Balance,
    
    /// <summary>
    /// Account equity (balance + floating P/L) over time
    /// </summary>
    [Description("equity")]
    Equity,
    
    /// <summary>
    /// Growth percentage from initial deposit
    /// </summary>
    [Description("growth")]
    Growth,
    
    /// <summary>
    /// Drawdown percentage from peak equity
    /// </summary>
    [Description("drawdown")]
    Drawdown
}

/// <summary>
/// Time periods for chart data
/// </summary>
public enum TimePeriod
{
    /// <summary>
    /// Last 30 days
    /// </summary>
    [Description("1M")]
    OneMonth,
    
    /// <summary>
    /// Last 90 days
    /// </summary>
    [Description("3M")]
    ThreeMonths,
    
    /// <summary>
    /// Last 180 days
    /// </summary>
    [Description("6M")]
    SixMonths,
    
    /// <summary>
    /// Last 365 days
    /// </summary>
    [Description("1Y")]
    OneYear,
    
    /// <summary>
    /// All available data from account creation
    /// </summary>
    [Description("ALL")]
    All
}

/// <summary>
/// Data aggregation intervals for chart display
/// </summary>
public enum DataInterval
{
    /// <summary>
    /// One data point per day
    /// </summary>
    [Description("daily")]
    Daily,
    
    /// <summary>
    /// Aggregate by week (Sunday start)
    /// </summary>
    [Description("weekly")]
    Weekly,
    
    /// <summary>
    /// Aggregate by month (1st day of month)
    /// </summary>
    [Description("monthly")]
    Monthly
}