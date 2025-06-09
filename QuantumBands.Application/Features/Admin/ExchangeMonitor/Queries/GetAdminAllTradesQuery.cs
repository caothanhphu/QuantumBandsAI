// QuantumBands.Application/Features/Admin/ExchangeMonitor/Queries/GetAdminAllTradesQuery.cs
namespace QuantumBands.Application.Features.Admin.ExchangeMonitor.Queries;

public class GetAdminAllTradesQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? TradingAccountId { get; set; }
    public int? BuyerUserId { get; set; }
    public int? SellerUserId { get; set; }
    public string? BuyerSearchTerm { get; set; }
    public string? SellerSearchTerm { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
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