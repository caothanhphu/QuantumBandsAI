// QuantumBands.Application/Interfaces/IEAIntegrationService.cs
using QuantumBands.Application.Features.EAIntegration.Commands.PushClosedTrades;
using QuantumBands.Application.Features.EAIntegration.Commands.PushLiveData;
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.Application.Interfaces;

public interface IEAIntegrationService
{
    Task<(LiveDataResponse? Response, string? ErrorMessage, int StatusCode)> ProcessLiveDataPushAsync(
        int tradingAccountId,
        PushLiveDataRequest request,
        CancellationToken cancellationToken = default);
    Task<(PushClosedTradesResponse? Response, string? ErrorMessage, int StatusCode)> ProcessClosedTradesPushAsync(
        int tradingAccountId,
        PushClosedTradesRequest request,
        CancellationToken cancellationToken = default);

}