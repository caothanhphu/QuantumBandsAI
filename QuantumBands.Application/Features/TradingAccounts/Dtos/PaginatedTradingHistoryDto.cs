using QuantumBands.Application.Common.Models;

namespace QuantumBands.Application.Features.TradingAccounts.Dtos;

public class PaginatedTradingHistoryDto
{
    public PaginationMetadata Pagination { get; set; } = new();
    public AppliedFilters Filters { get; set; } = new();
    public List<TradingHistoryDto> Trades { get; set; } = new();
    public TradingHistorySummary Summary { get; set; } = new();
}

public class PaginationMetadata
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public int FirstItemIndex { get; set; }
    public int LastItemIndex { get; set; }
}

public class AppliedFilters
{
    public string? Symbol { get; set; }
    public string? Type { get; set; }
    public DateRange? DateRange { get; set; }
    public ProfitRange? ProfitRange { get; set; }
    public VolumeRange? VolumeRange { get; set; }
    public string SortBy { get; set; } = string.Empty;
    public string SortOrder { get; set; } = string.Empty;
}

public class DateRange
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class ProfitRange
{
    public decimal? MinProfit { get; set; }
    public decimal? MaxProfit { get; set; }
}

public class VolumeRange
{
    public decimal? MinVolume { get; set; }
    public decimal? MaxVolume { get; set; }
}

public class TradingHistorySummary
{
    public decimal FilteredTotalProfit { get; set; }
    public int FilteredTotalTrades { get; set; }
    public int FilteredProfitableTrades { get; set; }
    public int FilteredLosingTrades { get; set; }
    public decimal FilteredWinRate { get; set; }
    public decimal FilteredGrossProfit { get; set; }
    public decimal FilteredGrossLoss { get; set; }
    public decimal FilteredTotalCommission { get; set; }
    public decimal FilteredTotalSwap { get; set; }
}