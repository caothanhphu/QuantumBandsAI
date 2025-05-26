// QuantumBands.Application/Features/Exchange/Dtos/TradingAccountMarketDataDto.cs
using System.Collections.Generic;

namespace QuantumBands.Application.Features.Exchange.Dtos;

public class TradingAccountMarketDataDto
{
    public int TradingAccountId { get; set; }
    public required string TradingAccountName { get; set; }
    public decimal? LastTradePrice { get; set; }
    public List<OrderBookEntryDto> BestBids { get; set; } = new List<OrderBookEntryDto>();
    public List<OrderBookEntryDto> BestAsks { get; set; } = new List<OrderBookEntryDto>();
    public List<ActiveOfferingDto> ActiveOfferings { get; set; } = new List<ActiveOfferingDto>(); // <<< THÊM MỚI
    public List<SimpleTradeDto> RecentTrades { get; set; } = new List<SimpleTradeDto>();
}