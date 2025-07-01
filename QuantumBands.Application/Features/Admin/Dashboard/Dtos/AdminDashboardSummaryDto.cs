// QuantumBands.Application/Features/Admin/Dashboard/Dtos/AdminDashboardSummaryDto.cs
using QuantumBands.Application.Common.Models;

namespace QuantumBands.Application.Features.Admin.Dashboard.Dtos;

public class AdminDashboardSummaryDto
{
    public long TotalUsers { get; set; }
    public int TotalActiveFunds { get; set; }
    public decimal TotalPlatformNAV { get; set; }
    public int PendingDepositsCount { get; set; }
    public int PendingWithdrawalsCount { get; set; }
    public List<SimpleTradeInfoDto> RecentTrades { get; set; } = new();
    public List<ChartDataPoint<long>> UserGrowthData { get; set; } = new();
    public List<ChartDataPoint<decimal>> PlatformNavHistory { get; set; } = new();
    public decimal? LastMatchedPrice { get; set; }
    public long? LastMatchedVolume { get; set; }
}