// QuantumBands.Application/Features/Exchange/Dtos/MyShareTradeDto.cs
namespace QuantumBands.Application.Features.Exchange.Dtos;

public class MyShareTradeDto
{
    public long TradeId { get; set; }
    public int TradingAccountId { get; set; }
    public required string TradingAccountName { get; set; }
    public required string OrderSide { get; set; } // "Buy" or "Sell" from the user's perspective
    public long QuantityTraded { get; set; }
    public decimal TradePrice { get; set; }
    public decimal TotalValue { get; set; }
    public decimal? FeeAmount { get; set; }
    public DateTime TradeDate { get; set; }
}