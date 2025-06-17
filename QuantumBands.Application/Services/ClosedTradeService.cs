// QuantumBands.Application/Services/ClosedTradeService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Interfaces.Repositories;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.Application.Services;

public class ClosedTradeService : IClosedTradeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClosedTradeService> _logger;

    public ClosedTradeService(IUnitOfWork unitOfWork, ILogger<ClosedTradeService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<(int TotalTrades, decimal WinRate, decimal ProfitFactor, decimal TotalProfit)> GetPerformanceKPIsAsync(int accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Calculating performance KPIs for trading account {AccountId}", accountId);

            var trades = await _unitOfWork.EAClosedTrades.Query()
                .Where(ct => ct.TradingAccountId == accountId)
                .Select(ct => new { ct.RealizedPandL })
                .ToListAsync(cancellationToken);

            if (!trades.Any())
            {
                return (0, 0m, 0m, 0m);
            }

            var totalTrades = trades.Count;
            var winningTrades = trades.Where(t => t.RealizedPandL > 0).ToList();
            var losingTrades = trades.Where(t => t.RealizedPandL < 0).ToList();

            var winRate = totalTrades > 0 ? (decimal)winningTrades.Count / totalTrades * 100 : 0m;
            var totalProfit = trades.Sum(t => t.RealizedPandL);

            var grossProfit = winningTrades.Sum(t => t.RealizedPandL);
            var grossLoss = Math.Abs(losingTrades.Sum(t => t.RealizedPandL));
            var profitFactor = grossLoss > 0 ? grossProfit / grossLoss : (grossProfit > 0 ? decimal.MaxValue : 0);

            _logger.LogInformation("Performance KPIs calculated for account {AccountId}: TotalTrades={TotalTrades}, WinRate={WinRate}%, ProfitFactor={ProfitFactor}", 
                accountId, totalTrades, winRate, profitFactor);

            return (totalTrades, winRate, profitFactor, totalProfit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating performance KPIs for trading account {AccountId}", accountId);
            throw;
        }
    }
}
