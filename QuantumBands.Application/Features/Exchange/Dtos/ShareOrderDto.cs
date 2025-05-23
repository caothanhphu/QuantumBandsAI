// QuantumBands.Application/Features/Exchange/Dtos/ShareOrderDto.cs
namespace QuantumBands.Application.Features.Exchange.Dtos;

public class ShareOrderDto
{
    public long OrderId { get; set; }
    public int UserId { get; set; }
    public int TradingAccountId { get; set; }
    public required string TradingAccountName { get; set; }
    public required string OrderSide { get; set; } // "Buy" hoặc "Sell"
    public required string OrderType { get; set; } // Tên của OrderType, ví dụ "Market", "Limit"
    public long QuantityOrdered { get; set; }
    public long QuantityFilled { get; set; }
    public decimal? LimitPrice { get; set; }
    public decimal? AverageFillPrice { get; set; }
    public required string OrderStatus { get; set; } // Tên của OrderStatus
    public DateTime OrderDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public decimal? TransactionFee { get; set; }
}