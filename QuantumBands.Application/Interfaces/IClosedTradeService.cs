// QuantumBands.Application/Interfaces/IClosedTradeService.cs
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.Application.Interfaces;

public interface IClosedTradeService
{
    Task<(int TotalTrades, decimal WinRate, decimal ProfitFactor, decimal TotalProfit)> GetPerformanceKPIsAsync(int accountId, CancellationToken cancellationToken = default);
}
