// QuantumBands.API/Controllers/UsersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuantumBands.Application.Features.Authentication; // For UserProfileDto
using QuantumBands.Application.Features.Users.Commands.UpdateProfile; // For UpdateUserProfileRequest
using QuantumBands.Application.Interfaces; // For IUserService
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;

namespace QuantumBands.API.Controllers;

[Authorize] // Tất cả các action trong controller này đều yêu cầu xác thực
[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to retrieve profile for current authenticated user via UsersController.");

        var (profile, errorMessage) = await _userService.GetUserProfileAsync(User, cancellationToken);

        if (profile == null)
        {
            _logger.LogWarning("Failed to retrieve profile for current user. Error: {ErrorMessage}", errorMessage);
            if (errorMessage != null && (errorMessage.Contains("not found") || errorMessage.Contains("not authenticated")))
            {
                return NotFound(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? "An unexpected error occurred." });
        }
        return Ok(profile);
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] // Cho validation lỗi
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        if (request == null) // FluentValidation đã kiểm tra, nhưng kiểm tra null vẫn tốt
        {
            return BadRequest(new { Message = "Update request cannot be null." });
        }

        _logger.LogInformation("Attempting to update profile for current authenticated user.");

        var (updatedProfile, errorMessage) = await _userService.UpdateUserProfileAsync(User, request, cancellationToken);

        if (updatedProfile == null)
        {
            _logger.LogWarning("Failed to update profile for current user. Error: {ErrorMessage}", errorMessage);
            if (errorMessage != null && (errorMessage.Contains("not found") || errorMessage.Contains("not authenticated")))
            {
                return NotFound(new { Message = errorMessage });
            }
            if (errorMessage != null && errorMessage.Contains("concurrency conflict"))
            {
                return Conflict(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? "An unexpected error occurred while updating profile." });
        }

        _logger.LogInformation("Profile updated successfully for UserID: {UserId}", updatedProfile.UserId);
        return Ok(updatedProfile);
    }
}