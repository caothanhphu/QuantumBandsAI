// QuantumBands.Application/Features/Exchange/Queries/GetMarketData/GetMarketDataQuery.cs
namespace QuantumBands.Application.Features.Exchange.Queries;

public class GetMarketDataQuery
{
    public string? TradingAccountIds { get; set; }
    public int RecentTradesLimit { get; set; } = 5;
    public int ActiveOfferingsLimit { get; set; } = 3; // <<< THÊM MỚI

    private const int MaxRecentTradesLimit = 20;
    private const int MinRecentTradesLimit = 1;
    private const int MaxActiveOfferingsLimit = 10;
    private const int MinActiveOfferingsLimit = 1;

    public int ValidatedRecentTradesLimit
    {
        get => (RecentTradesLimit > MaxRecentTradesLimit || RecentTradesLimit < MinRecentTradesLimit) ? 5 : RecentTradesLimit;
        set => RecentTradesLimit = value;
    }

    public int ValidatedActiveOfferingsLimit // <<< THÊM MỚI
    {
        get => (ActiveOfferingsLimit > MaxActiveOfferingsLimit || ActiveOfferingsLimit < MinActiveOfferingsLimit) ? 3 : ActiveOfferingsLimit;
        set => ActiveOfferingsLimit = value;
    }
}