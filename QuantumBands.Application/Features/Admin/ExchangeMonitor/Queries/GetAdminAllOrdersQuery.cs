// QuantumBands.Application/Features/Admin/ExchangeMonitor/Queries/GetAdminAllOrdersQuery.cs
namespace QuantumBands.Application.Features.Admin.ExchangeMonitor.Queries;

public class GetAdminAllOrdersQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? TradingAccountId { get; set; }
    public int? UserId { get; set; }
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public string? OrderSide { get; set; }
    public string? OrderType { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string SortBy { get; set; } = "OrderDate";
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