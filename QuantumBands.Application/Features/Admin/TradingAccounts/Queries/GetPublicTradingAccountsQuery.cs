// QuantumBands.Application/Features/TradingAccounts/Queries/GetPublicTradingAccountsQuery.cs
namespace QuantumBands.Application.Features.TradingAccounts.Queries;

public class GetPublicTradingAccountsQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "AccountName"; // Default sort field
    public string SortOrder { get; set; } = "asc";    // Default sort order
    public bool? IsActive { get; set; } // Lọc theo trạng thái hoạt động
    public string? SearchTerm { get; set; } // Tìm kiếm theo tên hoặc mô tả

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