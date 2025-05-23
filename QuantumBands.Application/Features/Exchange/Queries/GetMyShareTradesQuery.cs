// QuantumBands.Application/Features/Exchange/Queries/GetMyTrades/GetMyShareTradesQuery.cs
namespace QuantumBands.Application.Features.Exchange.Queries;

public class GetMyShareTradesQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? TradingAccountId { get; set; }
    public string? OrderSide { get; set; } // "Buy" or "Sell"
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string SortBy { get; set; } = "TradeDate";
    public string SortOrder { get; set; } = "desc";

    private const int MaxPageSize = 100;
    public int ValidatedPageSize
    {
        get => (PageSize > MaxPageSize || PageSize <= 0) ? MaxPageSize : PageSize;
        set => PageSize = value;
    }
    public int ValidatedPageNumber
    {
        get => PageNumber <= 0 ? 1 : PageNumber;
        set => PageNumber = value;
    }
}