// QuantumBands.Application/Features/Admin/Dashboard/Dtos/SimpleTradeInfoDto.cs
namespace QuantumBands.Application.Features.Admin.Dashboard.Dtos;

public class SimpleTradeInfoDto
{
    public long TradeId { get; set; }
    public required string TradingAccountName { get; set; }
    public DateTime TradeTime { get; set; }
    public long QuantityTraded { get; set; }
    public decimal TradePrice { get; set; }
}