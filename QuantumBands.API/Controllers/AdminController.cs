// QuantumBands.API/Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuantumBands.Application.Features.Wallets.Commands.AdminDeposit;
using QuantumBands.Application.Features.Wallets.Commands.BankDeposit;
using QuantumBands.Application.Features.Wallets.Dtos;
using QuantumBands.Application.Interfaces;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.API.Controllers;

[Authorize(Roles = "Admin")] // Yêu cầu vai trò Admin cho tất cả actions
[ApiController]
[Route("api/v1/admin")] // Route cơ sở cho các API của Admin
public class AdminController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly ILogger<AdminController> _logger;
    // Inject các services khác nếu AdminController quản lý nhiều hơn Wallet

    public AdminController(IWalletService walletService, ILogger<AdminController> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    // Endpoint cho Admin nạp tiền trực tiếp (đã có từ trước, đảm bảo logic đúng)
    [HttpPost("wallets/deposit")]
    [ProducesResponseType(typeof(WalletTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AdminDirectDeposit([FromBody] AdminDirectDepositRequest request, CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Admin {AdminId} attempting direct deposit to UserID: {TargetUserId}, Amount: {Amount}", adminUserId, request.UserId, request.Amount);
        var (transactionDto, errorMessage) = await _walletService.AdminDirectDepositAsync(User, request, cancellationToken);

        if (transactionDto == null)
        {
            _logger.LogWarning("Admin direct deposit failed. Admin: {AdminId}, Target User: {TargetUserId}, Error: {ErrorMessage}", adminUserId, request.UserId, errorMessage);
            if (errorMessage != null)
            {
                if (errorMessage.Contains("not found")) return NotFound(new { Message = errorMessage });
                if (errorMessage.Contains("Unauthorized") || errorMessage.Contains("Admin role required")) return Forbid(); // Hoặc Unauthorized tùy logic
                if (errorMessage.Contains("must be positive") || errorMessage.Contains("Invalid or unsupported currency")) return BadRequest(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? "Failed to process admin direct deposit." });
        }
        return Ok(transactionDto);
    }

    // Endpoint cho Admin xác nhận nạp tiền qua ngân hàng
    [HttpPost("wallets/deposits/bank/confirm")]
    [ProducesResponseType(typeof(WalletTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmBankDeposit([FromBody] ConfirmBankDepositRequest request, CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Admin {AdminId} attempting to confirm bank deposit TransactionID: {TransactionId}", adminUserId, request.TransactionId);
        var (transactionDto, errorMessage) = await _walletService.ConfirmBankDepositAsync(User, request, cancellationToken);

        if (transactionDto == null)
        {
            _logger.LogWarning("Admin confirm bank deposit failed for TransactionID {TransactionId}. Admin: {AdminId}, Error: {ErrorMessage}", request.TransactionId, adminUserId, errorMessage);
            if (errorMessage != null)
            {
                if (errorMessage.Contains("not found")) return NotFound(new { Message = errorMessage });
                if (errorMessage.Contains("not pending") || errorMessage.Contains("Invalid")) return BadRequest(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? "Failed to confirm bank deposit." });
        }
        return Ok(transactionDto);
    }

    // Endpoint cho Admin hủy yêu cầu nạp tiền qua ngân hàng
    [HttpPost("wallets/deposits/bank/cancel")]
    [ProducesResponseType(typeof(WalletTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBankDeposit([FromBody] CancelBankDepositRequest request, CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Admin {AdminId} attempting to cancel bank deposit TransactionID: {TransactionId}", adminUserId, request.TransactionId);
        var (transactionDto, errorMessage) = await _walletService.CancelBankDepositAsync(User, request, cancellationToken);

        if (transactionDto == null)
        {
            _logger.LogWarning("Admin cancel bank deposit failed for TransactionID {TransactionId}. Admin: {AdminId}, Error: {ErrorMessage}", request.TransactionId, adminUserId, errorMessage);
            if (errorMessage != null)
            {
                if (errorMessage.Contains("not found")) return NotFound(new { Message = errorMessage });
                if (errorMessage.Contains("cannot be cancelled") || errorMessage.Contains("Invalid")) return BadRequest(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? "Failed to cancel bank deposit." });
        }
        return Ok(transactionDto);
    }
}