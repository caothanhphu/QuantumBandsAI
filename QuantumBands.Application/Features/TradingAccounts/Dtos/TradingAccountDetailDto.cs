// QuantumBands.Application/Features/TradingAccounts/Dtos/TradingAccountDetailDto.cs
using QuantumBands.Application.Common.Models; // For PaginatedList
using System.Collections.Generic;

namespace QuantumBands.Application.Features.TradingAccounts.Dtos;

public class TradingAccountDetailDto // Có thể kế thừa từ TradingAccountDto nếu các trường cơ bản giống hệt
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

    public List<EAOpenPositionDto> OpenPositions { get; set; } = new List<EAOpenPositionDto>();
    public PaginatedList<EAClosedTradeDto> ClosedTradesHistory { get; set; } = null!;
    public PaginatedList<TradingAccountSnapshotDto> DailySnapshotsInfo { get; set; } = null!;
    // Có thể thêm List<InitialShareOfferingDto> nếu cần hiển thị ở đây
}