// QuantumBands.Application/Features/TradingAccounts/Dtos/TradingAccountSnapshotDto.cs
namespace QuantumBands.Application.Features.TradingAccounts.Dtos;
public class TradingAccountSnapshotDto
{
    public long SnapshotId { get; set; }
    public DateOnly SnapshotDate { get; set; } // Chỉ ngày
    public decimal OpeningNAV { get; set; }
    public decimal RealizedPAndLForTheDay { get; set; }
    public decimal UnrealizedPAndLForTheDay { get; set; }
    public decimal ManagementFeeDeducted { get; set; }
    public decimal ProfitDistributed { get; set; }
    public decimal ClosingNAV { get; set; }
    public decimal ClosingSharePrice { get; set; }
    public DateTime CreatedAt { get; set; }
}