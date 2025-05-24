// QuantumBands.Application/Services/EAIntegrationService.cs
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuantumBands.Application.Features.EAIntegration.Commands.PushClosedTrades;
using QuantumBands.Application.Features.EAIntegration.Commands.PushLiveData;
using QuantumBands.Application.Interfaces;
using QuantumBands.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.Application.Services;

public class EAIntegrationService : IEAIntegrationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EAIntegrationService> _logger;

    public EAIntegrationService(IUnitOfWork unitOfWork, ILogger<EAIntegrationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<(LiveDataResponse? Response, string? ErrorMessage, int StatusCode)> ProcessLiveDataPushAsync(
        int tradingAccountId,
        PushLiveDataRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing live data push for TradingAccountID: {TradingAccountId}. Equity: {Equity}, Balance: {Balance}, OpenPositionsCount: {Count}",
                               tradingAccountId, request.AccountEquity, request.AccountBalance, request.OpenPositions.Count);

        var tradingAccount = await _unitOfWork.TradingAccounts.GetByIdAsync(tradingAccountId);
        if (tradingAccount == null)
        {
            _logger.LogWarning("TradingAccountID {TradingAccountId} not found.", tradingAccountId);
            return (null, $"Trading account with ID {tradingAccountId} not found.", StatusCodes.Status404NotFound);
        }

        // 1. Cập nhật TradingAccount
        tradingAccount.CurrentNetAssetValue = request.AccountEquity; // NAV chính là Equity từ MT5
        // tradingAccount.AccountBalance = request.AccountBalance; // Nếu bạn có trường riêng cho balance trong TradingAccount
        tradingAccount.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.TradingAccounts.Update(tradingAccount);
        _logger.LogDebug("TradingAccountID {TradingAccountId} NAV updated to {NAV}", tradingAccountId, request.AccountEquity);

        // 2. Xử lý OpenPositions
        var existingDbPositions = await _unitOfWork.EAOpenPositions.Query()
                                        .Where(p => p.TradingAccountId == tradingAccountId)
                                        .ToListAsync(cancellationToken);

        var pushedPositionsMap = request.OpenPositions.ToDictionary(p => p.EaTicketId);
        var dbPositionsMap = existingDbPositions.ToDictionary(p => p.EaTicketId);

        int addedCount = 0;
        int updatedCount = 0;
        int removedCount = 0;

        // Thêm mới hoặc cập nhật
        foreach (var pushedPosDto in request.OpenPositions)
        {
            if (dbPositionsMap.TryGetValue(pushedPosDto.EaTicketId, out var dbPos))
            {
                // Cập nhật lệnh hiện có
                dbPos.Symbol = pushedPosDto.Symbol;
                dbPos.TradeType = pushedPosDto.TradeType;
                dbPos.VolumeLots = pushedPosDto.VolumeLots;
                dbPos.OpenPrice = pushedPosDto.OpenPrice;
                dbPos.OpenTime = pushedPosDto.OpenTime;
                dbPos.CurrentMarketPrice = pushedPosDto.CurrentMarketPrice;
                dbPos.Swap = pushedPosDto.Swap;
                dbPos.Commission = pushedPosDto.Commission;
                dbPos.FloatingPAndL = pushedPosDto.FloatingPAndL;
                dbPos.LastUpdateTime = DateTime.UtcNow;
                _unitOfWork.EAOpenPositions.Update(dbPos);
                updatedCount++;
            }
            else
            {
                // Thêm lệnh mới
                var newDbPos = new EAOpenPosition
                {
                    TradingAccountId = tradingAccountId,
                    EaTicketId = pushedPosDto.EaTicketId,
                    Symbol = pushedPosDto.Symbol,
                    TradeType = pushedPosDto.TradeType,
                    VolumeLots = pushedPosDto.VolumeLots,
                    OpenPrice = pushedPosDto.OpenPrice,
                    OpenTime = pushedPosDto.OpenTime,
                    CurrentMarketPrice = pushedPosDto.CurrentMarketPrice,
                    Swap = pushedPosDto.Swap,
                    Commission = pushedPosDto.Commission,
                    FloatingPAndL = pushedPosDto.FloatingPAndL,
                    LastUpdateTime = DateTime.UtcNow
                };
                await _unitOfWork.EAOpenPositions.AddAsync(newDbPos);
                addedCount++;
            }
        }

        // Xóa các lệnh không còn trong danh sách push (coi như đã đóng từ phía EA)
        var positionsToRemove = new List<EAOpenPosition>();
        foreach (var dbPos in existingDbPositions)
        {
            if (!pushedPositionsMap.ContainsKey(dbPos.EaTicketId))
            {
                positionsToRemove.Add(dbPos);
                removedCount++;
            }
        }
        if (positionsToRemove.Any())
        {
            _unitOfWork.EAOpenPositions.RemoveRange(positionsToRemove);
            _logger.LogInformation("Marked {RemovedCount} positions as closed for TradingAccountID {TradingAccountId} as they were not in the pushed list.", removedCount, tradingAccountId);
        }

        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Live data processed successfully for TradingAccountID {TradingAccountId}. Added: {Added}, Updated: {Updated}, Removed: {Removed}",
                                   tradingAccountId, addedCount, updatedCount, removedCount);

            var response = new LiveDataResponse
            {
                Message = "Live data processed successfully.",
                TradingAccountId = tradingAccountId,
                OpenPositionsProcessed = request.OpenPositions.Count,
                OpenPositionsAdded = addedCount,
                OpenPositionsUpdated = updatedCount,
                OpenPositionsRemoved = removedCount
            };
            return (response, null, StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving live data for TradingAccountID {TradingAccountId}", tradingAccountId);
            return (null, "An error occurred while saving live data.", StatusCodes.Status500InternalServerError);
        }
    }
    public async Task<(PushClosedTradesResponse? Response, string? ErrorMessage, int StatusCode)> ProcessClosedTradesPushAsync(
    int tradingAccountId,
    PushClosedTradesRequest request,
    CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing closed trades push for TradingAccountID: {TradingAccountId}. Trades received: {Count}",
                               tradingAccountId, request.ClosedTrades.Count);

        var tradingAccount = await _unitOfWork.TradingAccounts.GetByIdAsync(tradingAccountId);
        if (tradingAccount == null)
        {
            _logger.LogWarning("TradingAccountID {TradingAccountId} not found for closed trades push.", tradingAccountId);
            return (null, $"Trading account with ID {tradingAccountId} not found.", StatusCodes.Status404NotFound);
        }

        int addedCount = 0;
        int skippedCount = 0;
        var newTradesToInsert = new List<EAClosedTrade>();

        // Lấy danh sách các ticket ID đã tồn tại cho tài khoản này để tránh query nhiều lần trong vòng lặp
        var existingTicketIds = await _unitOfWork.EAClosedTrades.Query()
                                    .Where(ct => ct.TradingAccountId == tradingAccountId)
                                    .Select(ct => ct.EaTicketId)
                                    .ToListAsync(cancellationToken);
        var existingTicketIdSet = new HashSet<string>(existingTicketIds);

        foreach (var closedTradeDto in request.ClosedTrades)
        {
            if (existingTicketIdSet.Contains(closedTradeDto.EaTicketId))
            {
                _logger.LogDebug("Skipping existing closed trade. TradingAccountID: {TradingAccountId}, EaTicketID: {EaTicketId}",
                                 tradingAccountId, closedTradeDto.EaTicketId);
                skippedCount++;
                continue;
            }

            var newClosedTrade = new EAClosedTrade
            {
                TradingAccountId = tradingAccountId,
                EaTicketId = closedTradeDto.EaTicketId,
                Symbol = closedTradeDto.Symbol,
                TradeType = closedTradeDto.TradeType,
                VolumeLots = closedTradeDto.VolumeLots,
                OpenPrice = closedTradeDto.OpenPrice,
                OpenTime = closedTradeDto.OpenTime,
                ClosePrice = closedTradeDto.ClosePrice,
                CloseTime = closedTradeDto.CloseTime,
                Swap = closedTradeDto.Swap,
                Commission = closedTradeDto.Commission,
                RealizedPAndL = closedTradeDto.RealizedPAndL,
                IsProcessedForDailyPandL = false, // Mặc định cho lệnh mới
                RecordedAt = DateTime.UtcNow
            };
            newTradesToInsert.Add(newClosedTrade);
            addedCount++;
            existingTicketIdSet.Add(newClosedTrade.EaTicketId); // Thêm vào set để tránh trường hợp push trùng lặp trong cùng 1 batch
        }

        if (newTradesToInsert.Any())
        {
            await _unitOfWork.EAClosedTrades.AddRangeAsync(newTradesToInsert);
        }

        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Closed trades processed for TradingAccountID {TradingAccountId}. Received: {Received}, Added: {Added}, Skipped: {Skipped}",
                                   tradingAccountId, request.ClosedTrades.Count, addedCount, skippedCount);

            var response = new PushClosedTradesResponse
            {
                Message = "Closed trades processed successfully.",
                TradingAccountId = tradingAccountId,
                TradesReceived = request.ClosedTrades.Count,
                TradesAdded = addedCount,
                TradesSkipped = skippedCount
            };
            return (response, null, StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving closed trades for TradingAccountID {TradingAccountId}", tradingAccountId);
            return (null, "An error occurred while saving closed trades.", StatusCodes.Status500InternalServerError);
        }
    }
}