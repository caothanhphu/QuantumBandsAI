// QuantumBands.Application/Features/Exchange/Queries/GetMyOrders/GetMyShareOrdersQuery.cs
using System;

namespace QuantumBands.Application.Features.Exchange.Queries;

public class GetMyShareOrdersQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? TradingAccountId { get; set; }
    public string? Status { get; set; } // Có thể là một chuỗi các status cách nhau bởi dấu phẩy
    public string? OrderSide { get; set; } // "Buy" or "Sell"
    public string? OrderType { get; set; } // "Market" or "Limit"
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