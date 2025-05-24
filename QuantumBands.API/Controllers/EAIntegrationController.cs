// QuantumBands.API/Controllers/EAIntegrationController.cs
using Microsoft.AspNetCore.Mvc;
using QuantumBands.API.Attributes;
using QuantumBands.Application.Features.EAIntegration.Commands.PushClosedTrades;
using QuantumBands.Application.Features.EAIntegration.Commands.PushLiveData;
using QuantumBands.Application.Interfaces;
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
            return BadRequest(new ProblemDetails { Title = "Invalid Trading Account ID", Status = StatusCodes.Status400BadRequest });
        }

        var (response, errorMessage, statusCode) = await _eaIntegrationService.ProcessLiveDataPushAsync(accountId, request, cancellationToken);

        if (response == null)
        {
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
            return BadRequest(new ProblemDetails { Title = "Invalid Trading Account ID", Status = StatusCodes.Status400BadRequest });
        }
        // FluentValidation sẽ xử lý validation của request body

        var (response, errorMessage, statusCode) = await _eaIntegrationService.ProcessClosedTradesPushAsync(accountId, request, cancellationToken);

        if (response == null)
        {
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