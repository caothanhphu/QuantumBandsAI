// QuantumBands.Application/Features/Wallets/Queries/GetTransactions/GetWalletTransactionsQuery.cs
namespace QuantumBands.Application.Features.Wallets.Queries.GetTransactions;

public class GetWalletTransactionsQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? TransactionType { get; set; }
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string SortBy { get; set; } = "TransactionDate";
    public string SortOrder { get; set; } = "desc";

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