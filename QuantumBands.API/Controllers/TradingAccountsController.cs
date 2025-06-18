// QuantumBands.API/Controllers/TradingAccountsController.cs
using Microsoft.AspNetCore.Mvc;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos; // Using TradingAccountDto
using QuantumBands.Application.Features.TradingAccounts.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Queries; // Using GetPublicTradingAccountsQuery
using QuantumBands.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.API.Controllers;

[ApiController]
//[Route("api/v1/[controller]")] 
[Route("api/v1/trading-accounts")]// Route: /api/v1/trading-accounts
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

    /// <summary>
    /// Gets account overview with balance info and performance KPIs for a specific trading account.
    /// Users can only access their own accounts, admins can access any account.
    /// </summary>
    [HttpGet("{accountId}/overview")] // Endpoint: /api/v1/trading-accounts/{accountId}/overview
    [ProducesResponseType(typeof(AccountOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountOverview(int accountId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request received for account overview for TradingAccountID: {AccountId}", accountId);

        // Get current user info
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "uid" || c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Invalid user authentication" });
        }

        var isAdmin = User.IsInRole("Admin");

        var (overview, errorMessage) = await _tradingAccountService.GetAccountOverviewAsync(accountId, userId, isAdmin, cancellationToken);

        if (overview == null)
        {
            _logger.LogWarning("Failed to retrieve account overview for TradingAccountID {AccountId}. Error: {ErrorMessage}", accountId, errorMessage);
            
            if (errorMessage != null && errorMessage.Contains("Unauthorized"))
            {
                return Forbid();
            }
            if (errorMessage != null && errorMessage.Contains("not found"))
            {
                return NotFound(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? $"An unexpected error occurred while fetching overview for trading account ID {accountId}." });
        }

        return Ok(overview);
    }

    /// <summary>
    /// Gets chart data for trading account performance visualization.
    /// Supports multiple chart types (balance, equity, growth, drawdown) and time periods.
    /// Users can only access their own accounts, admins can access any account.
    /// </summary>
    /// <param name="accountId">Trading account identifier</param>
    /// <param name="query">Chart data query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chart data with data points and summary statistics</returns>
    [HttpGet("{accountId}/charts")] // Endpoint: /api/v1/trading-accounts/{accountId}/charts
    [ProducesResponseType(typeof(ChartDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChartsData(
        int accountId, 
        [FromQuery] GetChartDataQuery query, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request received for chart data for TradingAccountID: {AccountId} with query: {@Query}", accountId, query);

        // Get current user info
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "uid" || c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Invalid user authentication" });
        }

        var isAdmin = User.IsInRole("Admin");

        var (chartData, errorMessage) = await _tradingAccountService.GetChartDataAsync(accountId, query, userId, isAdmin, cancellationToken);

        if (chartData == null)
        {
            _logger.LogWarning("Failed to retrieve chart data for TradingAccountID {AccountId}. Error: {ErrorMessage}", accountId, errorMessage);
            
            if (errorMessage != null && errorMessage.Contains("Unauthorized"))
            {
                return Forbid();
            }
            if (errorMessage != null && errorMessage.Contains("not found"))
            {
                return NotFound(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? $"An unexpected error occurred while fetching chart data for trading account ID {accountId}." });
        }

        return Ok(chartData);
    }

    /// <summary>
    /// Gets paginated trading history for a specific trading account with advanced filtering and sorting.
    /// Supports filtering by symbol, trade type, date ranges, profit ranges, and volume ranges.
    /// Users can only access their own accounts, admins can access any account.
    /// </summary>
    /// <param name="accountId">Trading account identifier</param>
    /// <param name="query">Trading history query parameters with filters and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated trading history with summary statistics and applied filters</returns>
    [HttpGet("{accountId}/trading-history")] // Endpoint: /api/v1/trading-accounts/{accountId}/trading-history
    [ProducesResponseType(typeof(PaginatedTradingHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTradingHistory(
        int accountId, 
        [FromQuery] GetTradingHistoryQuery query, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request received for trading history for TradingAccountID: {AccountId} with query: {@Query}", accountId, query);

        // Get current user info
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "uid" || c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Invalid user authentication" });
        }

        var isAdmin = User.IsInRole("Admin");

        var (tradingHistory, errorMessage) = await _tradingAccountService.GetTradingHistoryAsync(accountId, query, userId, isAdmin, cancellationToken);

        if (tradingHistory == null)
        {
            _logger.LogWarning("Failed to retrieve trading history for TradingAccountID {AccountId}. Error: {ErrorMessage}", accountId, errorMessage);
            
            if (errorMessage != null && errorMessage.Contains("Unauthorized"))
            {
                return Forbid();
            }
            if (errorMessage != null && errorMessage.Contains("not found"))
            {
                return NotFound(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? $"An unexpected error occurred while fetching trading history for trading account ID {accountId}." });
        }

        return Ok(tradingHistory);
    }

    /// <summary>
    /// Gets real-time open positions for a specific trading account with comprehensive metrics and market data.
    /// Includes unrealized P&L calculations, margin information, and position summary statistics.
    /// Supports optional symbol filtering and real-time refresh capabilities.
    /// Users can only access their own accounts, admins can access any account.
    /// </summary>
    /// <param name="accountId">Trading account identifier</param>
    /// <param name="includeMetrics">Include advanced performance metrics in response</param>
    /// <param name="symbols">Comma-separated list of symbols to filter positions (optional)</param>
    /// <param name="refresh">Force refresh of real-time data before response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Real-time open positions with summary metrics and market data</returns>
    [HttpGet("{accountId}/open-positions")] // Endpoint: /api/v1/trading-accounts/{accountId}/open-positions
    [ProducesResponseType(typeof(OpenPositionsRealtimeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOpenPositions(
        int accountId,
        [FromQuery] bool includeMetrics = false,
        [FromQuery] string? symbols = null,
        [FromQuery] bool refresh = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request received for real-time open positions for TradingAccountID: {AccountId}, includeMetrics: {IncludeMetrics}, symbols: {Symbols}, refresh: {Refresh}", 
            accountId, includeMetrics, symbols, refresh);

        // Get current user info
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "uid" || c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Invalid user authentication" });
        }

        var isAdmin = User.IsInRole("Admin");

        var (openPositions, errorMessage) = await _tradingAccountService.GetOpenPositionsRealtimeAsync(
            accountId, includeMetrics, symbols, refresh, userId, isAdmin, cancellationToken);

        if (openPositions == null)
        {
            _logger.LogWarning("Failed to retrieve real-time open positions for TradingAccountID {AccountId}. Error: {ErrorMessage}", accountId, errorMessage);
            
            if (errorMessage != null && errorMessage.Contains("Unauthorized"))
            {
                return Forbid();
            }
            if (errorMessage != null && errorMessage.Contains("not found"))
            {
                return NotFound(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? $"An unexpected error occurred while fetching open positions for trading account ID {accountId}." });
        }

        return Ok(openPositions);
    }

    /// <summary>
    /// Gets comprehensive trading statistics and risk metrics for a specific trading account.
    /// Provides detailed analysis including trading performance, financial metrics, risk analysis,
    /// symbol breakdown, and monthly performance data with optional advanced metrics.
    /// Users can only access their own accounts, admins can access any account.
    /// </summary>
    /// <param name="accountId">Trading account identifier</param>
    /// <param name="query">Statistics query parameters with period, symbols filter, and advanced metrics options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive trading statistics with performance analysis and risk metrics</returns>
    [HttpGet("{accountId}/statistics")] // Endpoint: /api/v1/trading-accounts/{accountId}/statistics
    [ProducesResponseType(typeof(TradingStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatistics(
        int accountId,
        [FromQuery] GetStatisticsQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request received for trading statistics for TradingAccountID: {AccountId} with query: {@Query}", accountId, query);

        // Get current user info
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "uid" || c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Invalid user authentication" });
        }

        var isAdmin = User.IsInRole("Admin");

        var (statistics, errorMessage) = await _tradingAccountService.GetStatisticsAsync(accountId, query, userId, isAdmin, cancellationToken);

        if (statistics == null)
        {
            _logger.LogWarning("Failed to retrieve trading statistics for TradingAccountID {AccountId}. Error: {ErrorMessage}", accountId, errorMessage);

            if (errorMessage != null && errorMessage.Contains("Unauthorized"))
            {
                return Forbid();
            }
            if (errorMessage != null && errorMessage.Contains("not found"))
            {
                return NotFound(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? $"An unexpected error occurred while calculating statistics for trading account ID {accountId}." });
        }

        return Ok(statistics);
    }

    /// <summary>
    /// Gets comprehensive activity & audit trail for a specific trading account.
    /// Includes deposits, withdrawals, logins, configuration changes, trading activities, and system events.
    /// Supports filtering by activity type, date range, and pagination.
    /// </summary>
    /// <param name="accountId">The unique identifier of the trading account</param>
    /// <param name="query">Query parameters for filtering and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of account activities with comprehensive details and summary</returns>
    [HttpGet("{accountId}/activity")]
    [ProducesResponseType(typeof(AccountActivityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetActivity(
        int accountId,
        [FromQuery] GetActivityQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request received for account activity for TradingAccountID: {AccountId} with query: {@Query}", accountId, query);

        // Get current user info
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "uid" || c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Invalid user authentication" });
        }

        var isAdmin = User.IsInRole("Admin");

        var (activity, errorMessage) = await _tradingAccountService.GetActivityAsync(accountId, query, userId, isAdmin, cancellationToken);

        if (activity == null)
        {
            _logger.LogWarning("Failed to retrieve account activity for TradingAccountID {AccountId}. Error: {ErrorMessage}", accountId, errorMessage);

            if (errorMessage != null && errorMessage.Contains("permission"))
            {
                return Forbid();
            }
            if (errorMessage != null && errorMessage.Contains("not found"))
            {
                return NotFound(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? $"An unexpected error occurred while retrieving activity for trading account ID {accountId}." });
        }

        return Ok(activity);
    }

    /// <summary>
    /// Exports trading account data in various formats (CSV, Excel, PDF).
    /// Supports exporting trading history, statistics, performance reports, and risk reports.
    /// Includes filtering by date range and symbols, with authorization controls.
    /// </summary>
    /// <param name="accountId">The unique identifier of the trading account</param>
    /// <param name="query">Export parameters including type, format, and filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File download with appropriate content type and headers</returns>
    [HttpGet("{accountId}/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportData(
        int accountId,
        [FromQuery] ExportDataQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request received for data export for TradingAccountID: {AccountId} with query: {@Query}", accountId, query);

        // Get current user info
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "uid" || c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { Message = "Invalid user authentication" });
        }

        var isAdmin = User.IsInRole("Admin");

        var (exportResult, errorMessage) = await _tradingAccountService.ExportDataAsync(accountId, query, userId, isAdmin, cancellationToken);

        if (exportResult == null)
        {
            _logger.LogWarning("Failed to export data for TradingAccountID {AccountId}. Error: {ErrorMessage}", accountId, errorMessage);

            if (errorMessage != null && errorMessage.Contains("permission"))
            {
                return Forbid();
            }
            if (errorMessage != null && errorMessage.Contains("not found"))
            {
                return NotFound(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? $"An unexpected error occurred while exporting data for trading account ID {accountId}." });
        }

        // Return file download
        return File(
            exportResult.Data,
            exportResult.ContentType,
            exportResult.FileName);
    }
}