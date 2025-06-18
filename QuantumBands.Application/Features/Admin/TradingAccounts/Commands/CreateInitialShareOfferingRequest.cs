// QuantumBands.Application/Features/Admin/TradingAccounts/Commands/CreateInitialShareOfferingRequest.cs
namespace QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
public record CreateInitialShareOfferingRequest(
    long SharesOffered,
    decimal OfferingPricePerShare,
    decimal? FloorPricePerShare,
    decimal? CeilingPricePerShare,
    DateTime? OfferingEndDate
);