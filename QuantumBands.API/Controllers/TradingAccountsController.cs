﻿// QuantumBands.API/Controllers/TradingAccountsController.cs
using Microsoft.AspNetCore.Mvc;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos; // Using TradingAccountDto
using QuantumBands.Application.Features.TradingAccounts.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Queries; // Using GetPublicTradingAccountsQuery
using QuantumBands.Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")] // Route: /api/v1/trading-accounts
public class TradingAccountsController : ControllerBase
{
    private readonly ITradingAccountService _tradingAccountService;
    private readonly ILogger<TradingAccountsController> _logger;

    public TradingAccountsController(ITradingAccountService tradingAccountService, ILogger<TradingAccountsController> logger)
    {
        _tradingAccountService = tradingAccountService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a list of publicly available trading accounts (funds).
    /// Supports pagination, sorting, and filtering by active status and search term.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<TradingAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] // For invalid query parameters
    public async Task<IActionResult> GetPublicTradingAccounts([FromQuery] GetPublicTradingAccountsQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request received for public trading accounts with query: {@Query}", query);
        var result = await _tradingAccountService.GetPublicTradingAccountsAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets comprehensive details of a specific trading account,
    /// including open positions, closed trades history, and daily performance snapshots.
    /// </summary>
    [HttpGet("{accountId}")] // Endpoint: /api/v1/trading-accounts/{accountId}
    [ProducesResponseType(typeof(TradingAccountDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] // For invalid query parameters
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTradingAccountDetails(int accountId, [FromQuery] GetTradingAccountDetailsQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request received for trading account details. ID: {AccountId}, Query: {@Query}", accountId, query);

        // (Tùy chọn) Validate query DTO ở đây nếu không dùng auto-validation của FluentValidation cho [FromQuery]
        // Hoặc tạo một validator riêng cho GetTradingAccountDetailsQuery và đăng ký nó

        var (detailDto, errorMessage) = await _tradingAccountService.GetTradingAccountDetailsAsync(accountId, query, cancellationToken);

        if (detailDto == null)
        {
            _logger.LogWarning("Trading account details not found for ID {AccountId}. Error: {ErrorMessage}", accountId, errorMessage);
            if (errorMessage != null && errorMessage.Contains("not found"))
            {
                return NotFound(new { Message = errorMessage });
            }
            // Các lỗi khác có thể là lỗi server hoặc lỗi logic không mong muốn
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? $"An unexpected error occurred while fetching details for trading account ID {accountId}." });
        }
        return Ok(detailDto);
    }
    /// <summary>
    /// Gets a list of initial share offerings for a specific trading account.
    /// Supports pagination, sorting, and filtering by status.
    /// </summary>
    [HttpGet("{accountId}/initial-offerings")] // Endpoint: /api/v1/trading-accounts/{accountId}/initial-offerings
    [ProducesResponseType(typeof(PaginatedList<InitialShareOfferingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] // For invalid query parameters
    [ProducesResponseType(StatusCodes.Status404NotFound)] // If accountId is not found
    public async Task<IActionResult> GetInitialShareOfferings(int accountId, [FromQuery] GetInitialOfferingsQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request received for initial share offerings for TradingAccountID: {AccountId} with query: {@Query}", accountId, query);

        var (offerings, errorMessage) = await _tradingAccountService.GetInitialShareOfferingsAsync(accountId, query, cancellationToken);

        if (offerings == null)
        {
            _logger.LogWarning("Failed to retrieve initial share offerings for TradingAccountID {AccountId}. Error: {ErrorMessage}", accountId, errorMessage);
            if (errorMessage != null && errorMessage.Contains("not found"))
            {
                return NotFound(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? $"An unexpected error occurred while fetching offerings for trading account ID {accountId}." });
        }
        return Ok(offerings);
    }
}