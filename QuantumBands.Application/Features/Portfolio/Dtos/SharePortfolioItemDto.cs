// QuantumBands.Application/Features/Portfolio/Dtos/SharePortfolioItemDto.cs
namespace QuantumBands.Application.Features.Portfolio.Dtos;

public class SharePortfolioItemDto
{
    public int PortfolioId { get; set; }
    public int TradingAccountId { get; set; }
    public required string TradingAccountName { get; set; }
    public long Quantity { get; set; }
    public decimal AverageBuyPrice { get; set; }
    public decimal CurrentSharePrice { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal UnrealizedPAndL { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}