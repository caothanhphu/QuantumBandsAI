// QuantumBands.Application/Features/Admin/TradingAccounts/Commands/UpdateInitialShareOfferingRequest.cs
using System;

namespace QuantumBands.Application.Features.Admin.TradingAccounts.Commands;

public class UpdateInitialShareOfferingRequest
{
    public long? SharesOffered { get; set; }
    public decimal? OfferingPricePerShare { get; set; }
    public decimal? FloorPricePerShare { get; set; }
    public decimal? CeilingPricePerShare { get; set; }
    public DateTime? OfferingEndDate { get; set; }
    public string? Status { get; set; } // "Active", "Cancelled", "Completed", "Expired"
}