// QuantumBands.Application/Features/Exchange/Dtos/ActiveOfferingDto.cs
namespace QuantumBands.Application.Features.Exchange.Dtos;

public class ActiveOfferingDto
{
    public int OfferingId { get; set; }
    public decimal Price { get; set; }
    public long AvailableQuantity { get; set; }
}