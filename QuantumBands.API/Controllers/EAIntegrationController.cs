// QuantumBands.API/Controllers/EAIntegrationController.cs
using Microsoft.AspNetCore.Mvc;
using QuantumBands.API.Attributes;
using QuantumBands.Application.Features.EAIntegration.Commands.PushClosedTrades;
using QuantumBands.Application.Features.EAIntegration.Commands.PushLiveData;
using QuantumBands.Application.Interfaces;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
// using QuantumBands.API.Attributes; // Cho ApiKeyAuthorizeAttribute nếu bạn tạo

namespace QuantumBands.API.Controllers;

[ApiController]
[Route("api/v1/ea-integration")]
[ApiKeyAuthorize] // Áp dụng API Key authentication ở đây
public class EAIntegrationController : ControllerBase
{
    private readonly IEAIntegrationService _eaIntegrationService;
    private readonly ILogger<EAIntegrationController> _logger;

    public EAIntegrationController(IEAIntegrationService eaIntegrationService, ILogger<EAIntegrationController> logger)
    {
        _eaIntegrationService = eaIntegrationService;
        _logger = logger;
    }

    [HttpPost("trading-accounts/{accountId}/live-data")]
    [ProducesResponseType(typeof(LiveDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Nếu API Key sai
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PushLiveData(int accountId, [FromBody] PushLiveDataRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received live data push for TradingAccountID: {AccountId}", accountId);

        if (accountId <= 0)
        {
            _logger.LogWarning("Invalid Trading Account ID provided: {AccountId}", accountId);
            return BadRequest(new ProblemDetails { Title = "Invalid Trading Account ID", Status = StatusCodes.Status400BadRequest });
        }

        var (response, errorMessage, statusCode) = await _eaIntegrationService.ProcessLiveDataPushAsync(accountId, request, cancellationToken);

        if (response == null)
        {
            // Log the request body on failure
            try
            {
                var requestBodyForLogging = JsonSerializer.Serialize(request);
                _logger.LogError("EAIntegration: PushLiveData failed for AccountID {AccountId}. StatusCode: {StatusCode}, Error: {ErrorMessage}, RequestBody: {RequestBody}",
                                 accountId, statusCode, errorMessage, requestBodyForLogging);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EAIntegration: PushLiveData failed for AccountID {AccountId} and failed to serialize request body for logging.", accountId);
            }

            return statusCode switch
            {
                StatusCodes.Status404NotFound => NotFound(new ProblemDetails { Title = errorMessage, Status = statusCode }),
                StatusCodes.Status500InternalServerError => StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = errorMessage, Status = statusCode }),
                _ => BadRequest(new ProblemDetails { Title = errorMessage ?? "Unknown error", Status = statusCode }),
            };
        }
        return Ok(response);
    }
    [HttpPost("trading-accounts/{accountId}/closed-trades")]
    [ProducesResponseType(typeof(PushClosedTradesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PushClosedTrades(int accountId, [FromBody] PushClosedTradesRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received closed trades push for TradingAccountID: {AccountId}", accountId);

        if (accountId <= 0)
        {
            _logger.LogWarning("Invalid Trading Account ID provided: {AccountId}", accountId);
            return BadRequest(new ProblemDetails { Title = "Invalid Trading Account ID", Status = StatusCodes.Status400BadRequest });
        }
        // FluentValidation will handle validation of the request body

        var (response, errorMessage, statusCode) = await _eaIntegrationService.ProcessClosedTradesPushAsync(accountId, request, cancellationToken);

        if (response == null)
        {
            // Log the request body on failure
            try
            {
                var requestBodyForLogging = JsonSerializer.Serialize(request);
                _logger.LogError("EAIntegration: PushClosedTrades failed for AccountID {AccountId}. StatusCode: {StatusCode}, Error: {ErrorMessage}, RequestBody: {RequestBody}",
                                 accountId, statusCode, errorMessage, requestBodyForLogging);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EAIntegration: PushClosedTrades failed for AccountID {AccountId} and failed to serialize request body for logging.", accountId);
            }

            return statusCode switch
            {
                StatusCodes.Status404NotFound => NotFound(new ProblemDetails { Title = errorMessage, Status = statusCode }),
                StatusCodes.Status500InternalServerError => StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Title = errorMessage, Status = statusCode }),
                _ => BadRequest(new ProblemDetails { Title = errorMessage ?? "Unknown error processing closed trades.", Status = statusCode }),
            };
        }
        return Ok(response);
    }
}