// QuantumBands.API/Controllers/WalletsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Wallets.Commands.BankDeposit;
using QuantumBands.Application.Features.Wallets.Commands.CreateWithdrawal;
using QuantumBands.Application.Features.Wallets.Commands.InternalTransfer;
using QuantumBands.Application.Features.Wallets.Dtos;
using QuantumBands.Application.Features.Wallets.Queries.GetTransactions;
using QuantumBands.Application.Interfaces; // For IWalletService
using System.Security.Claims; // For User property
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.API.Controllers;

[Authorize] // Yêu cầu xác thực cho tất cả actions trong controller này
[ApiController]
[Route("api/v1/[controller]")]
public class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly ILogger<WalletsController> _logger;

    public WalletsController(IWalletService walletService, ILogger<WalletsController> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current authenticated user's wallet details.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>The user's wallet details.</returns>
    [HttpGet()]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyWallet(CancellationToken cancellationToken)
    {
        // User property được cung cấp bởi ASP.NET Core sau khi xác thực JWT thành công
        if (User?.Identity?.IsAuthenticated != true)
        {
            // Dòng này thường không cần thiết vì [Authorize] đã kiểm tra
            _logger.LogWarning("GetMyWallet endpoint accessed without proper authentication although [Authorize] is present.");
            return Unauthorized(new { Message = "User is not authenticated." });
        }

        _logger.LogInformation("Attempting to retrieve wallet for current authenticated user: {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));

        var (walletDto, errorMessage) = await _walletService.GetUserWalletAsync(User, cancellationToken);

        if (walletDto == null)
        {
            _logger.LogWarning("Failed to retrieve wallet for current user. Error: {ErrorMessage}", errorMessage);
            if (errorMessage != null && (errorMessage.Contains("not found") || errorMessage.Contains("not authenticated")))
            {
                // Nếu service trả về "not authenticated", có thể trả về 401 thay vì 404
                return NotFound(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? "An unexpected error occurred while retrieving the wallet." });
        }

        return Ok(walletDto);
    }

    [HttpGet("transactions")]
    [ProducesResponseType(typeof(PaginatedList<WalletTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMyWalletTransactions([FromQuery] GetWalletTransactionsQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to retrieve transactions for current authenticated user with query: {@Query}", query);
        var result = await _walletService.GetUserWalletTransactionsAsync(User, query, cancellationToken);
        return Ok(result);
    }
    
    [HttpPost("deposits/bank/initiate")] // Endpoint đã đổi
    [ProducesResponseType(typeof(BankDepositInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> InitiateBankDeposit([FromBody] InitiateBankDepositRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} initiating bank deposit for AmountUSD: {AmountUSD}", User.FindFirstValue(ClaimTypes.NameIdentifier), request.AmountUSD);
        var (response, errorMessage) = await _walletService.InitiateBankDepositAsync(User, request, cancellationToken);

        if (response == null)
        {
            _logger.LogWarning("Bank deposit initiation failed for User {UserId}. Error: {ErrorMessage}", User.FindFirstValue(ClaimTypes.NameIdentifier), errorMessage);
            if (errorMessage != null && (errorMessage.Contains("configuration") || errorMessage.Contains("Exchange rate")))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage });
            }
            return BadRequest(new { Message = errorMessage ?? "Failed to initiate bank deposit." });
        }
        return Ok(response);
    }
    [HttpPost("withdrawals")] // Route: /api/v1/wallets/me/withdrawals
    [ProducesResponseType(typeof(WithdrawalRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateWithdrawalRequest([FromBody] CreateWithdrawalRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("User {UserId} initiating withdrawal request for Amount: {Amount} {Currency}", userId, request.Amount, request.CurrencyCode);

        var (responseDto, errorMessage) = await _walletService.CreateWithdrawalRequestAsync(User, request, cancellationToken);

        if (responseDto == null)
        {
            _logger.LogWarning("Withdrawal request creation failed for User {UserId}. Error: {ErrorMessage}", userId, errorMessage);
            if (errorMessage != null)
            {
                if (errorMessage.Contains("not found")) return NotFound(new { Message = errorMessage });
                if (errorMessage.Contains("Insufficient") || errorMessage.Contains("must be greater than 0") || errorMessage.Contains("required"))
                {
                    return BadRequest(new { Message = errorMessage });
                }
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? "Failed to create withdrawal request." });
        }

        // Trả về 201 Created với thông tin yêu cầu rút tiền
        // Location header có thể trỏ đến một endpoint để xem chi tiết yêu cầu rút tiền (nếu có)
        return CreatedAtAction(nameof(GetMyWalletTransactions), new { /* tham số cho GetMyWalletTransactions nếu có */ }, responseDto);
        // Hoặc đơn giản là:
        // return StatusCode(StatusCodes.Status201Created, responseDto);
    }
    [HttpPost("internal-transfer/verify-recipient")]
    [ProducesResponseType(typeof(RecipientInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyRecipient([FromBody] VerifyRecipientRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to verify recipient email: {RecipientEmail}", request.RecipientEmail);
        var (recipientInfo, errorMessage) = await _walletService.VerifyRecipientForTransferAsync(request, cancellationToken);

        if (recipientInfo == null)
        {
            _logger.LogWarning("Recipient verification failed for email {RecipientEmail}. Error: {ErrorMessage}", request.RecipientEmail, errorMessage);
            if (errorMessage != null && errorMessage.Contains("not found"))
            {
                return NotFound(new { Message = errorMessage });
            }
            return BadRequest(new { Message = errorMessage ?? "Failed to verify recipient." });
        }
        return Ok(recipientInfo);
    }

    [HttpPost("internal-transfer/execute")]
    [ProducesResponseType(typeof(WalletTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)] // Nếu ví người gửi/nhận không tìm thấy
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExecuteTransfer([FromBody] ExecuteInternalTransferRequest request, CancellationToken cancellationToken)
    {
        var senderUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("User {SenderUserId} attempting to execute internal transfer to UserID {RecipientUserId} for Amount {Amount}",
                               senderUserId, request.RecipientUserId, request.Amount);

        var (senderTransactionDto, errorMessage) = await _walletService.ExecuteInternalTransferAsync(User, request, cancellationToken);

        if (senderTransactionDto == null)
        {
            _logger.LogWarning("Internal transfer execution failed for sender {SenderUserId} to recipient {RecipientUserId}. Error: {ErrorMessage}",
                               senderUserId, request.RecipientUserId, errorMessage);
            if (errorMessage != null)
            {
                if (errorMessage.Contains("not found")) return NotFound(new { Message = errorMessage });
                if (errorMessage.Contains("Insufficient") || errorMessage.Contains("yourself") || errorMessage.Contains("invalid"))
                {
                    return BadRequest(new { Message = errorMessage });
                }
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? "Failed to execute internal transfer." });
        }
        return Ok(senderTransactionDto);
    }

}