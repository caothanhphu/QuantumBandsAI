// QuantumBands.Application/Features/Exchange/Dtos/MarketDataResponse.cs
using System;
using System.Collections.Generic;

namespace QuantumBands.Application.Features.Exchange.Dtos;

public class MarketDataResponse
{
    public List<TradingAccountMarketDataDto> Items { get; set; } = new List<TradingAccountMarketDataDto>();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}