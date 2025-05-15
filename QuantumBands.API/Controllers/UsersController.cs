// QuantumBands.API/Controllers/UsersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuantumBands.Application.Features.Authentication; // For UserProfileDto
using QuantumBands.Application.Features.Users.Commands.UpdateProfile; // For UpdateUserProfileRequest
using QuantumBands.Application.Interfaces; // For IUserService
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;
using QuantumBands.Application.Features.Users.Commands.ChangePassword;
using QuantumBands.Application.Features.Users.Commands.Setup2FA;
using QuantumBands.Application.Features.Users.Commands.Enable2FA;
using QuantumBands.Application.Features.Users.Commands.Verify2FA;
using QuantumBands.Application.Features.Users.Commands.Disable2FA;

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
    // [Authorize(Roles = "Admin,Investor")] // Có thể đặt ở đây hoặc để [Authorize] ở cấp controller là đủ nếu không có phân quyền cụ thể hơn
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

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] // Cho validation lỗi hoặc current password sai
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)] // Nếu user không tìm thấy (hiếm khi xảy ra với [Authorize])
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (request == null) // FluentValidation đã kiểm tra, nhưng kiểm tra null vẫn tốt
        {
            return BadRequest(new { Message = "Change password request cannot be null." });
        }

        _logger.LogInformation("Attempting to change password for current authenticated user.");

        var (success, message) = await _userService.ChangePasswordAsync(User, request, cancellationToken);

        if (!success)
        {
            _logger.LogWarning("Password change failed for current user. Reason: {Reason}", message);
            // Phân biệt lỗi do người dùng (ví dụ: sai mật khẩu hiện tại) hay lỗi hệ thống
            if (message.Contains("Incorrect current password") || message.Contains("not found")) // "not found" cho user
            {
                return BadRequest(new { Message = message }); // 400 cho thông tin không hợp lệ
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = message ?? "An unexpected error occurred while changing password." });
        }

        _logger.LogInformation("Password changed successfully for current user.");
        return Ok(new { Message = message });
    }

    [HttpPost("2fa/setup")]
    [ProducesResponseType(typeof(Setup2FAResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Setup2FA(CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} attempting to setup 2FA.", User.FindFirstValue(ClaimTypes.NameIdentifier));
        var (response, errorMessage) = await _userService.Setup2FAAsync(User, cancellationToken);
        if (response == null)
        {
            return BadRequest(new { Message = errorMessage ?? "Failed to initiate 2FA setup." });
        }
        return Ok(response);
    }

    [HttpPost("2fa/enable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Enable2FA([FromBody] Enable2FARequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} attempting to enable 2FA.", User.FindFirstValue(ClaimTypes.NameIdentifier));
        var (success, message, recoveryCodes) = await _userService.Enable2FAAsync(User, request, cancellationToken);
        if (!success)
        {
            return BadRequest(new { Message = message });
        }
        // Trả về recovery codes nếu có
        return Ok(new { Message = message, RecoveryCodes = recoveryCodes });
    }

    [HttpPost("2fa/verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Verify2FACode([FromBody] Verify2FARequest request, CancellationToken cancellationToken)
    {
        // Endpoint này dùng để user xác minh mã 2FA cho một hành động nhạy cảm
        // hoặc là bước thứ hai của login (nếu luồng login được thiết kế như vậy)
        _logger.LogInformation("User {UserId} attempting to verify 2FA code.", User.FindFirstValue(ClaimTypes.NameIdentifier));
        var (success, message) = await _userService.Verify2FACodeAsync(User, request, cancellationToken);
        if (!success)
        {
            return BadRequest(new { Message = message });
        }
        return Ok(new { Success = true, Message = message });
    }

    [HttpPost("2fa/disable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Disable2FA([FromBody] Disable2FARequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} attempting to disable 2FA.", User.FindFirstValue(ClaimTypes.NameIdentifier));
        var (success, message) = await _userService.Disable2FAAsync(User, request, cancellationToken);
        if (!success)
        {
            return BadRequest(new { Message = message });
        }
        return Ok(new { Message = message });
    }
}