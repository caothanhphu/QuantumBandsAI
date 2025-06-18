// QuantumBands.Application/Services/ChartDataService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuantumBands.Application.Features.TradingAccounts.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Enums;
using QuantumBands.Application.Features.TradingAccounts.Queries;
using QuantumBands.Application.Interfaces;

namespace QuantumBands.Application.Services;

/// <summary>
/// Service for generating chart data for trading account performance visualization
/// </summary>
public class ChartDataService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChartDataService> _logger;
    private readonly IWalletService _walletService;

    public ChartDataService(
        IUnitOfWork unitOfWork,
        ILogger<ChartDataService> logger,
        IWalletService walletService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _walletService = walletService;
    }

    /// <summary>
    /// Generates chart data based on the specified parameters
    /// </summary>
    public async Task<ChartDataDto> GenerateChartDataAsync(
        int accountId,
        GetChartDataQuery query,
        CancellationToken cancellationToken = default)
    {
        var (startDate, endDate) = CalculateDateRange(query.Period, accountId);
        var rawData = await GetRawDataAsync(accountId, startDate, endDate, cancellationToken);
        var dataPoints = AggregateData(rawData, query.Interval);
        var chartData = await CalculateChartValuesAsync(accountId, dataPoints, query.Type, cancellationToken);
        
        var summary = CalculateSummary(chartData);
        
        return new ChartDataDto
        {
            ChartType = query.Type,
            Period = query.Period,
            Interval = query.Interval,
            DataPoints = chartData,
            Summary = summary
        };
    }

    /// <summary>
    /// Calculates the date range based on the time period
    /// </summary>
    private (DateTime startDate, DateTime endDate) CalculateDateRange(TimePeriod period, int accountId)
    {
        var endDate = DateTime.UtcNow;
        DateTime startDate;

        switch (period)
        {
            case TimePeriod.OneMonth:
                startDate = endDate.AddDays(-30);
                break;
            case TimePeriod.ThreeMonths:
                startDate = endDate.AddDays(-90);
                break;
            case TimePeriod.SixMonths:
                startDate = endDate.AddDays(-180);
                break;
            case TimePeriod.OneYear:
                startDate = endDate.AddDays(-365);
                break;
            case TimePeriod.All:
            default:
                // Get account creation date
                var account = _unitOfWork.TradingAccounts.Query()
                    .Where(ta => ta.TradingAccountId == accountId)
                    .Select(ta => ta.CreatedAt)
                    .FirstOrDefault();
                startDate = account != default ? account : endDate.AddYears(-1);
                break;
        }

        return (startDate, endDate);
    }

    /// <summary>
    /// Gets raw trading data for the specified date range
    /// </summary>
    private async Task<List<DailyDataPoint>> GetRawDataAsync(
        int accountId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        // Get closed trades
        var closedTrades = await _unitOfWork.EAClosedTrades.Query()
            .Where(ct => ct.TradingAccountId == accountId && 
                        ct.CloseTime >= startDate && 
                        ct.CloseTime <= endDate)
            .OrderBy(ct => ct.CloseTime)
            .Select(ct => new { ct.CloseTime, Profit = ct.RealizedPandL })
            .ToListAsync(cancellationToken);

        // Get deposits/withdrawals through wallet and user relationship
        var transactions = await _unitOfWork.WalletTransactions.Query()
            .Include(wt => wt.Wallet)
            .Include(wt => wt.Wallet.User)
            .Where(wt => wt.Wallet.User.TradingAccounts.Any(ta => ta.TradingAccountId == accountId) && 
                        wt.TransactionDate >= startDate && 
                        wt.TransactionDate <= endDate)
            .OrderBy(wt => wt.TransactionDate)
            .Select(wt => new { CreatedAt = wt.TransactionDate, wt.Amount })
            .ToListAsync(cancellationToken);

        // Get initial deposit
        var (_, _, initialDeposit) = await _walletService.GetFinancialSummaryAsync(accountId, cancellationToken);

        // Combine and aggregate by day
        var dailyData = new Dictionary<DateTime, DailyDataPoint>();
        var runningBalance = initialDeposit;

        // Process transactions
        foreach (var transaction in transactions)
        {
            var date = transaction.CreatedAt.Date;
            if (!dailyData.ContainsKey(date))
            {
                dailyData[date] = new DailyDataPoint
                {
                    Date = date,
                    Balance = runningBalance,
                    DailyProfit = 0,
                    Deposits = 0,
                    Withdrawals = 0
                };
            }

            if (transaction.Amount > 0)
                dailyData[date].Deposits += transaction.Amount;
            else
                dailyData[date].Withdrawals += Math.Abs(transaction.Amount);

            runningBalance += transaction.Amount;
            dailyData[date].Balance = runningBalance;
        }

        // Process trades
        foreach (var trade in closedTrades)
        {
            var date = trade.CloseTime.Date;
            if (!dailyData.ContainsKey(date))
            {
                dailyData[date] = new DailyDataPoint
                {
                    Date = date,
                    Balance = runningBalance,
                    DailyProfit = 0,
                    Deposits = 0,
                    Withdrawals = 0
                };
            }

            dailyData[date].DailyProfit += trade.Profit;
            runningBalance += trade.Profit;
            dailyData[date].Balance = runningBalance;
        }

        return dailyData.Values.OrderBy(d => d.Date).ToList();
    }

    /// <summary>
    /// Aggregates data according to the specified interval
    /// </summary>
    private List<DailyDataPoint> AggregateData(List<DailyDataPoint> rawData, DataInterval interval)
    {
        switch (interval)
        {
            case DataInterval.Weekly:
                return AggregateWeekly(rawData);
            case DataInterval.Monthly:
                return AggregateMonthly(rawData);
            case DataInterval.Daily:
            default:
                return rawData;
        }
    }

    /// <summary>
    /// Aggregates data by week (Sunday start)
    /// </summary>
    private List<DailyDataPoint> AggregateWeekly(List<DailyDataPoint> dailyData)
    {
        return dailyData
            .GroupBy(d => GetWeekStart(d.Date))
            .Select(g => new DailyDataPoint
            {
                Date = g.Key,
                Balance = g.Last().Balance, // End of week balance
                DailyProfit = g.Sum(d => d.DailyProfit),
                Deposits = g.Sum(d => d.Deposits),
                Withdrawals = g.Sum(d => d.Withdrawals)
            })
            .OrderBy(d => d.Date)
            .ToList();
    }

    /// <summary>
    /// Aggregates data by month (1st day of month)
    /// </summary>
    private List<DailyDataPoint> AggregateMonthly(List<DailyDataPoint> dailyData)
    {
        return dailyData
            .GroupBy(d => new DateTime(d.Date.Year, d.Date.Month, 1))
            .Select(g => new DailyDataPoint
            {
                Date = g.Key,
                Balance = g.Last().Balance, // End of month balance
                DailyProfit = g.Sum(d => d.DailyProfit),
                Deposits = g.Sum(d => d.Deposits),
                Withdrawals = g.Sum(d => d.Withdrawals)
            })
            .OrderBy(d => d.Date)
            .ToList();
    }

    /// <summary>
    /// Gets the start of the week (Sunday) for a given date
    /// </summary>
    private DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    /// <summary>
    /// Calculates chart values based on chart type
    /// </summary>
    private async Task<List<ChartDataPointDto>> CalculateChartValuesAsync(
        int accountId,
        List<DailyDataPoint> dataPoints,
        ChartType chartType,
        CancellationToken cancellationToken)
    {
        var result = new List<ChartDataPointDto>();
        var (_, _, initialDeposit) = await _walletService.GetFinancialSummaryAsync(accountId, cancellationToken);
        var peakEquity = initialDeposit;

        foreach (var point in dataPoints)
        {
            decimal value = chartType switch
            {
                ChartType.Balance => point.Balance,
                ChartType.Equity => await CalculateEquityAsync(accountId, point.Date, point.Balance, cancellationToken),
                ChartType.Growth => initialDeposit > 0 ? ((point.Balance - initialDeposit) / initialDeposit) * 100 : 0,
                ChartType.Drawdown => CalculateDrawdown(point.Balance, ref peakEquity),
                _ => point.Balance
            };

            var equity = chartType == ChartType.Equity ? value : 
                await CalculateEquityAsync(accountId, point.Date, point.Balance, cancellationToken);

            result.Add(new ChartDataPointDto
            {
                Timestamp = point.Date,
                Value = Math.Round(value, 2),
                Metadata = new ChartDataMetadataDto
                {
                    Balance = Math.Round(point.Balance, 2),
                    Equity = Math.Round(equity, 2),
                    OpenPositions = await GetOpenPositionsCountAsync(accountId, point.Date, cancellationToken),
                    DailyProfit = Math.Round(point.DailyProfit, 2)
                }
            });
        }

        return result;
    }

    /// <summary>
    /// Calculates equity (balance + floating P/L) for a specific date
    /// </summary>
    private async Task<decimal> CalculateEquityAsync(
        int accountId,
        DateTime date,
        decimal balance,
        CancellationToken cancellationToken)
    {
        // For historical dates, we would need floating P/L at that time
        // For simplicity, we'll use current floating P/L only for recent dates
        if (date.Date == DateTime.UtcNow.Date)
        {
            var floatingPnL = await _unitOfWork.EAOpenPositions.Query()
                .Where(op => op.TradingAccountId == accountId)
                .SumAsync(op => op.FloatingPandL, cancellationToken);
            
            return balance + floatingPnL;
        }

        return balance; // Historical equity would equal balance for closed periods
    }

    /// <summary>
    /// Calculates drawdown percentage from peak equity
    /// </summary>
    private decimal CalculateDrawdown(decimal currentEquity, ref decimal peakEquity)
    {
        if (currentEquity > peakEquity)
        {
            peakEquity = currentEquity;
        }

        if (peakEquity <= 0) return 0;

        return ((currentEquity - peakEquity) / peakEquity) * 100;
    }

    /// <summary>
    /// Gets the number of open positions at a specific date
    /// </summary>
    private async Task<int> GetOpenPositionsCountAsync(
        int accountId,
        DateTime date,
        CancellationToken cancellationToken)
    {
        // For current date, get actual open positions
        if (date.Date == DateTime.UtcNow.Date)
        {
            return await _unitOfWork.EAOpenPositions.Query()
                .CountAsync(op => op.TradingAccountId == accountId, cancellationToken);
        }

        // For historical dates, we would need position history
        // For now, return 0 for historical dates
        return 0;
    }

    /// <summary>
    /// Calculates summary statistics for the chart data
    /// </summary>
    private ChartSummaryDto CalculateSummary(List<ChartDataPointDto> dataPoints)
    {
        if (!dataPoints.Any())
        {
            return new ChartSummaryDto
            {
                StartValue = 0,
                EndValue = 0,
                ChangeAbsolute = 0,
                ChangePercent = 0,
                MaxValue = 0,
                MinValue = 0,
                TotalDataPoints = 0
            };
        }

        var startValue = dataPoints.First().Value;
        var endValue = dataPoints.Last().Value;
        var maxValue = dataPoints.Max(dp => dp.Value);
        var minValue = dataPoints.Min(dp => dp.Value);
        var changeAbsolute = endValue - startValue;
        var changePercent = startValue != 0 ? (changeAbsolute / startValue) * 100 : 0;

        return new ChartSummaryDto
        {
            StartValue = Math.Round(startValue, 2),
            EndValue = Math.Round(endValue, 2),
            ChangeAbsolute = Math.Round(changeAbsolute, 2),
            ChangePercent = Math.Round(changePercent, 2),
            MaxValue = Math.Round(maxValue, 2),
            MinValue = Math.Round(minValue, 2),
            TotalDataPoints = dataPoints.Count
        };
    }

    /// <summary>
    /// Internal class for daily data aggregation
    /// </summary>
    private class DailyDataPoint
    {
        public DateTime Date { get; set; }
        public decimal Balance { get; set; }
        public decimal DailyProfit { get; set; }
        public decimal Deposits { get; set; }
        public decimal Withdrawals { get; set; }
    }
}