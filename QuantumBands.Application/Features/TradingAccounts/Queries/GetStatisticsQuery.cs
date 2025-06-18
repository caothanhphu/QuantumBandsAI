// QuantumBands.Application/Features/TradingAccounts/Queries/GetStatisticsQuery.cs
using QuantumBands.Application.Features.TradingAccounts.Enums;

namespace QuantumBands.Application.Features.TradingAccounts.Queries;

public class GetStatisticsQuery
{
    public TimePeriod Period { get; set; } = TimePeriod.All;
    public string? Symbols { get; set; }
    public bool IncludeAdvanced { get; set; } = false;
    public string? Benchmark { get; set; }
}