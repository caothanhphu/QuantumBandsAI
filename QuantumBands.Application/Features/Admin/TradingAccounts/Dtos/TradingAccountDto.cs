// QuantumBands.Application/Features/Admin/TradingAccounts/Dtos/TradingAccountDto.cs
namespace QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;
public class TradingAccountDto
{
    public int TradingAccountId { get; set; }
    public required string AccountName { get; set; }
    public string? Description { get; set; }
    public string? EaName { get; set; }
    public string? BrokerPlatformIdentifier { get; set; }
    public decimal InitialCapital { get; set; }
    public long TotalSharesIssued { get; set; }
    public decimal CurrentNetAssetValue { get; set; }
    public decimal CurrentSharePrice { get; set; }
    public decimal ManagementFeeRate { get; set; }
    public bool IsActive { get; set; }
    public int CreatedByUserId { get; set; }
    public required string CreatorUsername { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}