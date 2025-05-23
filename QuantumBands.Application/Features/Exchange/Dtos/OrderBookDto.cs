// QuantumBands.Application/Features/Exchange/Dtos/OrderBookDto.cs
using System;
using System.Collections.Generic;

namespace QuantumBands.Application.Features.Exchange.Dtos;

public class OrderBookDto
{
    public int TradingAccountId { get; set; }
    public required string TradingAccountName { get; set; }
    public decimal? LastTradePrice { get; set; }
    public DateTime Timestamp { get; set; }
    public List<OrderBookEntryDto> Bids { get; set; } = new List<OrderBookEntryDto>();
    public List<OrderBookEntryDto> Asks { get; set; } = new List<OrderBookEntryDto>();
}