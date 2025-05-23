// QuantumBands.Application/Features/Exchange/Commands/CreateOrder/CreateShareOrderRequest.cs
namespace QuantumBands.Application.Features.Exchange.Commands.CreateOrder;

public class CreateShareOrderRequest
{
    public int TradingAccountId { get; set; }
    public int OrderTypeId { get; set; } // Tham chiếu đến ID trong bảng ShareOrderTypes
    public required string OrderSide { get; set; } // "Buy" hoặc "Sell"
    public long QuantityOrdered { get; set; }
    public decimal? LimitPrice { get; set; } // Nullable, chỉ cần thiết cho lệnh Limit
}