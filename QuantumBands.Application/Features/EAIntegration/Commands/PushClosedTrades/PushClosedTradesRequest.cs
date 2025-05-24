// QuantumBands.Application/Features/EAIntegration/Commands/PushClosedTrades/PushClosedTradesRequest.cs
using System.Collections.Generic;

namespace QuantumBands.Application.Features.EAIntegration.Commands.PushClosedTrades;

public class PushClosedTradesRequest
{
    public List<EAClosedTradeDtoFromEA> ClosedTrades { get; set; } = new List<EAClosedTradeDtoFromEA>();
}