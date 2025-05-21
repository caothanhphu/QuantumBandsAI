// QuantumBands.Application/Features/Admin/TradingAccounts/Dtos/InitialShareOfferingDto.cs
namespace QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;
public class InitialShareOfferingDto
{
    public int OfferingId { get; set; }
    public int TradingAccountId { get; set; }
    public int AdminUserId { get; set; }
    public required string AdminUsername { get; set; }
    public long SharesOffered { get; set; }
    public long SharesSold { get; set; }
    public decimal OfferingPricePerShare { get; set; }
    public decimal? FloorPricePerShare { get; set; }
    public decimal? CeilingPricePerShare { get; set; }
    public DateTime OfferingStartDate { get; set; }
    public DateTime? OfferingEndDate { get; set; }
    public required string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}