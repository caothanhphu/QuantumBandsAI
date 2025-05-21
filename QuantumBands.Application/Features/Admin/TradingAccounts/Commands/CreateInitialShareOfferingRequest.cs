// QuantumBands.Application/Features/Admin/TradingAccounts/Commands/CreateInitialShareOfferingRequest.cs
namespace QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
public class CreateInitialShareOfferingRequest
{
    public long SharesOffered { get; set; }
    public decimal OfferingPricePerShare { get; set; }
    public decimal? FloorPricePerShare { get; set; }
    public decimal? CeilingPricePerShare { get; set; }
    public DateTime? OfferingEndDate { get; set; }
}