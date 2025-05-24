// QuantumBands.Application/Features/EAIntegration/Commands/PushLiveData/LiveDataResponse.cs
namespace QuantumBands.Application.Features.EAIntegration.Commands.PushLiveData;

public class LiveDataResponse
{
    public required string Message { get; set; }
    public int TradingAccountId { get; set; }
    public int OpenPositionsProcessed { get; set; } // Tổng số lệnh trong request
    public int OpenPositionsAdded { get; set; }
    public int OpenPositionsUpdated { get; set; }
    public int OpenPositionsRemoved { get; set; } // Số lệnh trong DB không có trong request (đã đóng)
}