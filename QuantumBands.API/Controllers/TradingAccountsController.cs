// QuantumBands.API/Controllers/TradingAccountsController.cs
using Microsoft.AspNetCore.Mvc;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos; // Using TradingAccountDto
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
    /// Gets a specific trading account by its ID.
    /// (This endpoint is primarily for CreatedAtAction in AdminController, but can be public)
    /// </summary>
    [HttpGet("{accountId}")]
    [ProducesResponseType(typeof(TradingAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTradingAccountById(int accountId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request received for trading account ID: {AccountId}", accountId);
        var account = await _tradingAccountService.GetTradingAccountByIdAsync(accountId, cancellationToken);
        if (account == null)
        {
            return NotFound(new { Message = $"Trading account with ID {accountId} not found." });
        }
        return Ok(account);
    }
}