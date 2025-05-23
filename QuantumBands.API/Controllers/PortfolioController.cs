// QuantumBands.API/Controllers/PortfolioController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuantumBands.Application.Features.Portfolio.Dtos;
using QuantumBands.Application.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic; // For List

namespace QuantumBands.API.Controllers;

[Authorize] // Yêu cầu xác thực cho tất cả actions
[ApiController]
[Route("api/v1/[controller]")] // Route: /api/v1/portfolio
public class PortfolioController : ControllerBase
{
    private readonly ISharePortfolioService _portfolioService;
    private readonly ILogger<PortfolioController> _logger;

    public PortfolioController(ISharePortfolioService portfolioService, ILogger<PortfolioController> logger)
    {
        _portfolioService = portfolioService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current authenticated user's share portfolio.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>A list of share portfolio items.</returns>
    [HttpGet("me")] // Endpoint: /api/v1/portfolio/me
    [ProducesResponseType(typeof(List<SharePortfolioItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)] // Nếu user không có portfolio (hiếm)
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyPortfolio(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Lấy UserId từ token
        _logger.LogInformation("User {UserId} requesting their portfolio.", userId);

        var (portfolioItems, errorMessage) = await _portfolioService.GetMyPortfolioAsync(User, cancellationToken);

        if (portfolioItems == null)
        {
            _logger.LogWarning("Failed to retrieve portfolio for User {UserId}. Error: {ErrorMessage}", userId, errorMessage);
            if (errorMessage != null && (errorMessage.Contains("not found") || errorMessage.Contains("not authenticated")))
            {
                // Trả về mảng rỗng nếu không có item, thay vì 404
                return Ok(new List<SharePortfolioItemDto>());
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? "An unexpected error occurred while retrieving portfolio." });
        }

        return Ok(portfolioItems);
    }
}