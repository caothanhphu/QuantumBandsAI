using System.ComponentModel.DataAnnotations;

namespace QuantumBands.Application.Features.TradingAccounts.Queries;

public class GetTradingHistoryQuery
{
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 20;

    [StringLength(10, ErrorMessage = "Symbol must not exceed 10 characters")]
    public string? Symbol { get; set; }

    [RegularExpression(@"^(BUY|SELL)$", ErrorMessage = "Type must be either 'BUY' or 'SELL'")]
    public string? Type { get; set; }

    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    public decimal? MinProfit { get; set; }

    public decimal? MaxProfit { get; set; }

    public decimal? MinVolume { get; set; }

    public decimal? MaxVolume { get; set; }

    [RegularExpression(@"^(closeTime|openTime|profit|symbol|volume)$", 
        ErrorMessage = "SortBy must be one of: closeTime, openTime, profit, symbol, volume")]
    public string SortBy { get; set; } = "closeTime";

    [RegularExpression(@"^(asc|desc)$", ErrorMessage = "SortOrder must be either 'asc' or 'desc'")]
    public string SortOrder { get; set; } = "desc";

    public int ValidatedPage => Math.Max(1, Page);
    public int ValidatedPageSize => Math.Min(Math.Max(1, PageSize), 100);
    public string ValidatedSortBy => string.IsNullOrWhiteSpace(SortBy) ? "closeTime" : SortBy.ToLowerInvariant();
    public string ValidatedSortOrder => string.IsNullOrWhiteSpace(SortOrder) ? "desc" : SortOrder.ToLowerInvariant();
}