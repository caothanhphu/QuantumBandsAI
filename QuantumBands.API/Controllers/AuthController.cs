// QuantumBands.API/Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using QuantumBands.Application.Features.Authentication.Commands.RegisterUser;
using QuantumBands.Application.Features.Authentication.Commands.VerifyEmail; // Thêm using
using QuantumBands.Application.Features.Authentication.Commands.ResendVerificationEmail; // Thêm using
using QuantumBands.Application.Features.Authentication.Commands.Login; // Thêm using
using QuantumBands.Application.Features.Authentication.Commands.RefreshToken; // Thêm using
using QuantumBands.Application.Features.Authentication.Commands.ForgotPassword; // Thêm using
using QuantumBands.Application.Features.Authentication.Commands.ResetPassword; // Thêm using
using QuantumBands.Application.Interfaces; // For IAuthService
using System.Threading.Tasks;
using System.Threading;
using QuantumBands.Application.Features.Authentication;
using Microsoft.AspNetCore.Authorization; // Thêm using cho [Authorize]
using System.Security.Claims; // Thêm using cho User.FindFirstValue

namespace QuantumBands.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")] // Route chuẩn hơn cho versioning
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(QuantumBands.Application.Features.Authentication.UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)] // Cho trường hợp username/email đã tồn tại
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        if (command == null) // FluentValidation sẽ bắt hầu hết các trường hợp, nhưng kiểm tra null vẫn tốt
        {
            return BadRequest("Registration request cannot be null.");
        }

        _logger.LogInformation("Received registration request for username: {Username}", command.Username);

        var (userDto, errorMessage) = await _authService.RegisterUserAsync(command, cancellationToken);

        if (userDto == null)
        {
            _logger.LogWarning("Registration failed for {Username}. Error: {ErrorMessage}", command.Username, errorMessage);
            // Phân biệt lỗi do người dùng (409) hay lỗi hệ thống (500)
            if (errorMessage != null && (errorMessage.Contains("exists") || errorMessage.Contains("already taken")))
            {
                return Conflict(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? "An unexpected error occurred during registration." });
        }

        _logger.LogInformation("User {Username} registered successfully with ID {UserId}.", userDto.Username, userDto.UserId);
        // Trả về 201 Created với UserDto và Location header
        // Giả sử bạn sẽ có một endpoint để lấy thông tin user theo ID, ví dụ /api/v1/users/{id}
        // return CreatedAtAction("GetUserById", "Users", new { id = userDto.UserId }, userDto);
        // Nếu chưa có UsersController.GetUserById, có thể trả về Created không có location:
        return StatusCode(StatusCodes.Status201Created, userDto);
    }
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)] // Hoặc trả về thông báo chung trong 400
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        if (request == null) // FluentValidation đã kiểm tra các trường, nhưng kiểm tra null vẫn tốt
        {
            return BadRequest("Verification request cannot be null.");
        }
        _logger.LogInformation("Received email verification request for UserID: {UserId}", request.UserId);

        var (success, message) = await _authService.VerifyEmailAsync(request, cancellationToken);

        if (!success)
        {
            _logger.LogWarning("Email verification failed for UserID {UserId}. Reason: {Reason}", request.UserId, message);
            // Trả về BadRequest hoặc NotFound tùy thuộc vào thông điệp lỗi
            if (message.Contains("expired") || message.Contains("Invalid"))
            {
                return BadRequest(new { Message = message });
            }
            // Có thể thêm các trường hợp lỗi khác
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = message });
        }

        _logger.LogInformation("Email verification successful for UserID {UserId}.", request.UserId);
        return Ok(new { Message = message });
    }

    [HttpPost("resend-verification-email")]
    [ProducesResponseType(StatusCodes.Status200OK)] // Luôn trả về OK để không tiết lộ email tồn tại
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResendVerificationEmail([FromBody] ResendVerificationEmailRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest("Request cannot be null.");
        }
        _logger.LogInformation("Received request to resend verification email for Email: {Email}", request.Email);

        var (success, message) = await _authService.ResendVerificationEmailAsync(request, cancellationToken);

        if (!success)
        {
            // Lỗi này thường là lỗi hệ thống (không gửi được mail, không lưu được token mới)
            _logger.LogError("Failed to process resend verification email for {Email}. Reason: {Reason}", request.Email, message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = message });
        }

        // Luôn trả về OK để bảo mật
        _logger.LogInformation("Resend verification email process completed for Email: {Email}. Result message: {Message}", request.Email, message);
        return Ok(new { Message = message });
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] // Cho validation lỗi
    [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Cho sai credentials
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest("Login request cannot be null.");
        }
        _logger.LogInformation("Received login request for user: {UsernameOrEmail}", request.UsernameOrEmail);

        var (loginResponse, errorMessage) = await _authService.LoginAsync(request, cancellationToken);

        if (loginResponse == null)
        {
            _logger.LogWarning("Login failed for {UsernameOrEmail}. Reason: {Reason}", request.UsernameOrEmail, errorMessage);
            // Phân biệt lỗi do sai thông tin (401) hay lỗi hệ thống (500)
            if (errorMessage != null && (errorMessage.Contains("Invalid") || errorMessage.Contains("inactive") || errorMessage.Contains("verify")))
            {
                // Trả về 401 cho các lỗi liên quan đến thông tin đăng nhập hoặc trạng thái tài khoản
                return Unauthorized(new { Message = errorMessage });
            }
            // Các lỗi khác coi là lỗi server
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? "An unexpected error occurred during login." });
        }

        _logger.LogInformation("User {Username} logged in successfully.", loginResponse.Username);
        return Ok(loginResponse);
    }
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] // Cho validation lỗi (nếu có validator)
    [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Cho token không hợp lệ/hết hạn
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new { Message = "Refresh token request cannot be null." });
        }
        _logger.LogInformation("Received request to refresh token.");

        var (loginResponse, errorMessage) = await _authService.RefreshTokenAsync(request, cancellationToken);

        if (loginResponse == null)
        {
            _logger.LogWarning("Token refresh failed. Reason: {Reason}", errorMessage);
            // Trả về 401 cho các lỗi liên quan đến token
            return Unauthorized(new { Message = errorMessage ?? "Invalid token or refresh token." });
        }

        _logger.LogInformation("Token refreshed successfully for UserID {UserId}.", loginResponse.UserId);
        return Ok(loginResponse);
    }
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)] // Luôn trả về 200 để tránh user enumeration
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Cho các lỗi không mong muốn khi xử lý
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new { Message = "Request cannot be null." });
        }
        _logger.LogInformation("Received forgot password request for email: {Email}", request.Email);

        var (success, message) = await _authService.ForgotPasswordAsync(request, cancellationToken);

        // Ngay cả khi có lỗi server (success=false), chúng ta vẫn trả về message chung từ service
        // để không tiết lộ thông tin. Lỗi server sẽ được log.
        _logger.LogInformation("Forgot password process completed for email {Email}. Result message: {Message}", request.Email, message);
        return Ok(new { Message = message }); // Luôn trả về thông báo chung từ service
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] // Cho token không hợp lệ/hết hạn hoặc validation lỗi
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new { Message = "Request cannot be null." });
        }
        _logger.LogInformation("Received reset password request for email: {Email}", request.Email);

        var (success, message) = await _authService.ResetPasswordAsync(request, cancellationToken);

        if (!success)
        {
            _logger.LogWarning("Password reset failed for email {Email}. Reason: {Reason}", request.Email, message);
            // Phân biệt lỗi do người dùng (token sai/hết hạn) hay lỗi hệ thống
            if (message.Contains("Invalid") || message.Contains("expired"))
            {
                return BadRequest(new { Message = message });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = message ?? "An unexpected error occurred while resetting password." });
        }

        _logger.LogInformation("Password reset successful for email {Email}.", request.Email);
        return Ok(new { Message = message });
    }
}
