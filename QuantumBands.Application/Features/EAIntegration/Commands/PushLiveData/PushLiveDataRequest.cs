// QuantumBands.Application/Features/EAIntegration/Commands/PushLiveData/PushLiveDataRequest.cs
using QuantumBands.Application.Features.EAIntegration.Dtos;
using System.Collections.Generic;

namespace QuantumBands.Application.Features.EAIntegration.Commands.PushLiveData;

public class PushLiveDataRequest
{
    public decimal AccountEquity { get; set; }
    public decimal AccountBalance { get; set; }
    public List<EAOpenPositionDtoFromEA> OpenPositions { get; set; } = new List<EAOpenPositionDtoFromEA>();
}