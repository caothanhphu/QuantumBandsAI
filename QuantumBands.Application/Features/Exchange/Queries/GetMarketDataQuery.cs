// QuantumBands.Application/Features/Exchange/Queries/GetMarketData/GetMarketDataQuery.cs
namespace QuantumBands.Application.Features.Exchange.Queries;

public class GetMarketDataQuery
{
    public string? TradingAccountIds { get; set; } // Comma-separated string of IDs
    public int RecentTradesLimit { get; set; } = 5; // Default limit

    private const int MaxRecentTradesLimit = 20;
    private const int MinRecentTradesLimit = 1;
    public int ValidatedRecentTradesLimit
    {
        get => (RecentTradesLimit > MaxRecentTradesLimit || RecentTradesLimit < MinRecentTradesLimit) ? 5 : RecentTradesLimit; // Default to 5 if out of range
        set => RecentTradesLimit = value;
    }
}