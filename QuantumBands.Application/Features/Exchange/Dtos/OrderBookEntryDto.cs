// QuantumBands.Application/Features/Exchange/Dtos/OrderBookEntryDto.cs
namespace QuantumBands.Application.Features.Exchange.Dtos;

public class OrderBookEntryDto
{
    public decimal Price { get; set; }
    public long TotalQuantity { get; set; }
}