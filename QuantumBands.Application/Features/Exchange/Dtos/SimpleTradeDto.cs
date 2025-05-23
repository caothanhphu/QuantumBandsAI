// QuantumBands.Application/Features/Exchange/Dtos/SimpleTradeDto.cs
namespace QuantumBands.Application.Features.Exchange.Dtos;

public class SimpleTradeDto
{
    public decimal Price { get; set; }
    public long Quantity { get; set; }
    public DateTime TradeTime { get; set; }
}