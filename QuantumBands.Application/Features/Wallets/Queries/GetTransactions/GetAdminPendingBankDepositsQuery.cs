
using System;

namespace QuantumBands.Application.Features.Wallets.Queries.GetTransactions;

public class GetAdminPendingBankDepositsQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "TransactionDate"; // Default sort field
    public string SortOrder { get; set; } = "desc"; // Default sort order
    public int? UserId { get; set; }
    public string? UsernameOrEmail { get; set; }
    public string? ReferenceCode { get; set; }
    public decimal? MinAmountUSD { get; set; }
    public decimal? MaxAmountUSD { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

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