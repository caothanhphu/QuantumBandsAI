// QuantumBands.Application/Features/Admin/ExchangeMonitor/Dtos/AdminShareTradeViewDto.cs
namespace QuantumBands.Application.Features.Admin.ExchangeMonitor.Dtos;

public class AdminShareTradeViewDto
{
    public long TradeId { get; set; }
    public int TradingAccountId { get; set; }
    public required string TradingAccountName { get; set; }
    public int BuyerUserId { get; set; }
    public required string BuyerUsername { get; set; }
    public int SellerUserId { get; set; }
    public required string SellerUsername { get; set; }
    public long QuantityTraded { get; set; }
    public decimal TradePrice { get; set; }
    public decimal TotalValue { get; set; }
    public decimal BuyerFeeAmount { get; set; }
    public decimal SellerFeeAmount { get; set; }
    public DateTime TradeDate { get; set; }
}