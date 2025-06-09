// QuantumBands.Application/Features/Admin/ExchangeMonitor/Dtos/AdminShareOrderViewDto.cs
namespace QuantumBands.Application.Features.Admin.ExchangeMonitor.Dtos;

public class AdminShareOrderViewDto
{
    public long OrderId { get; set; }
    public int UserId { get; set; }
    public required string Username { get; set; }
    public required string UserEmail { get; set; }
    public int TradingAccountId { get; set; }
    public required string TradingAccountName { get; set; }
    public required string OrderSide { get; set; }
    public required string OrderType { get; set; }
    public long QuantityOrdered { get; set; }
    public long QuantityFilled { get; set; }
    public decimal? LimitPrice { get; set; }
    public decimal? AverageFillPrice { get; set; }
    public required string OrderStatus { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public decimal? TransactionFee { get; set; }
}