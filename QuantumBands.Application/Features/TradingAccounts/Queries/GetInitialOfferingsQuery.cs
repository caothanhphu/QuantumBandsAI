// QuantumBands.Application/Features/TradingAccounts/Queries/GetInitialOfferingsQuery.cs
namespace QuantumBands.Application.Features.TradingAccounts.Queries;

public class GetInitialOfferingsQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10; // Mặc định trả về 10, có thể cho phép nhiều hơn nếu không phân trang
    public string SortBy { get; set; } = "OfferingStartDate";
    public string SortOrder { get; set; } = "desc";
    public string? Status { get; set; } // "Active", "Completed", "Cancelled", "Expired"

    private const int MaxPageSize = 50;
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