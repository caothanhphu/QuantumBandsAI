// QuantumBands.API/Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;
using QuantumBands.Application.Features.Admin.Users.Commands.UpdateUserRole;
using QuantumBands.Application.Features.Admin.Users.Commands.UpdateUserStatus;
using QuantumBands.Application.Features.Admin.Users.Dtos;
using QuantumBands.Application.Features.Admin.Users.Queries;
using QuantumBands.Application.Features.Wallets.Commands.AdminActions;
using QuantumBands.Application.Features.Wallets.Commands.AdminDeposit;
using QuantumBands.Application.Features.Wallets.Commands.BankDeposit;
using QuantumBands.Application.Features.Wallets.Dtos;
using QuantumBands.Application.Features.Wallets.Queries.GetTransactions;
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Services;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.API.Controllers;

[Authorize(Roles = "Admin")] // Yêu cầu vai trò Admin cho tất cả actions
[ApiController]
[Route("api/v1/admin")] // Route cơ sở cho các API của Admin
public class AdminController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IWalletService _walletService;
    private readonly ITradingAccountService _tradingAccountService; // Inject service mới
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IUserService userService,
        IWalletService walletService,
        ITradingAccountService tradingAccountService, // Thêm vào constructor
        ILogger<AdminController> logger)
    {
        _userService = userService;
        _walletService = walletService;
        _tradingAccountService = tradingAccountService; // Gán
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
    [HttpPost("wallets/withdrawals/approve")] // Route: /api/v1/admin/wallets/withdrawals/approve
    [ProducesResponseType(typeof(WalletTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveWithdrawal([FromBody] ApproveWithdrawalRequest request, CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Admin {AdminId} attempting to approve withdrawal TransactionID: {TransactionId}", adminUserId, request.TransactionId);
        var (transactionDto, errorMessage) = await _walletService.ApproveWithdrawalAsync(User, request, cancellationToken);

        if (transactionDto == null)
        {
            _logger.LogWarning("Admin approve withdrawal failed for TransactionID {TransactionId}. Admin: {AdminId}, Error: {ErrorMessage}", request.TransactionId, adminUserId, errorMessage);
            if (errorMessage != null)
            {
                if (errorMessage.Contains("not found")) return NotFound(new { Message = errorMessage });
                if (errorMessage.Contains("not pending") || errorMessage.Contains("Insufficient") || errorMessage.Contains("Invalid")) return BadRequest(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? "Failed to approve withdrawal request." });
        }
        return Ok(transactionDto);
    }

    [HttpPost("wallets/withdrawals/reject")] // Route: /api/v1/admin/wallets/withdrawals/reject
    [ProducesResponseType(typeof(WalletTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectWithdrawal([FromBody] RejectWithdrawalRequest request, CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Admin {AdminId} attempting to reject withdrawal TransactionID: {TransactionId}", adminUserId, request.TransactionId);
        var (transactionDto, errorMessage) = await _walletService.RejectWithdrawalAsync(User, request, cancellationToken);

        if (transactionDto == null)
        {
            _logger.LogWarning("Admin reject withdrawal failed for TransactionID {TransactionId}. Admin: {AdminId}, Error: {ErrorMessage}", request.TransactionId, adminUserId, errorMessage);
            if (errorMessage != null)
            {
                if (errorMessage.Contains("not found")) return NotFound(new { Message = errorMessage });
                if (errorMessage.Contains("cannot be rejected") || errorMessage.Contains("Invalid")) return BadRequest(new { Message = errorMessage });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = errorMessage ?? "Failed to reject withdrawal request." });
        }
        return Ok(transactionDto);
    }

    [HttpGet("wallets/deposits/bank/pending-confirmation")]
    [ProducesResponseType(typeof(PaginatedList<AdminPendingBankDepositDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] // Nếu query params không hợp lệ
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingBankDeposits([FromQuery] GetAdminPendingBankDepositsQuery query, CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Admin {AdminId} requesting list of pending bank deposits with query: {@Query}", adminUserId, query);

        // (Tùy chọn) Validate query DTO ở đây nếu không dùng auto-validation của FluentValidation cho [FromQuery]
        // Hoặc tạo một validator riêng cho GetAdminPendingBankDepositsQuery và đăng ký nó

        var result = await _walletService.GetAdminPendingBankDepositsAsync(User, query, cancellationToken);
        return Ok(result);
    }
    [HttpGet("wallets/withdrawals/pending-approval")]
    [ProducesResponseType(typeof(PaginatedList<WithdrawalRequestAdminViewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingWithdrawals([FromQuery] GetAdminPendingWithdrawalsQuery query, CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Admin {AdminId} requesting list of pending withdrawal requests with query: {@Query}", adminUserId, query);

        var result = await _walletService.GetAdminPendingWithdrawalsAsync(User, query, cancellationToken);
        return Ok(result);
    }
    // --- USER MANAGEMENT ENDPOINTS ---
    [HttpGet("users")] // Route: /api/v1/admin/users
    [ProducesResponseType(typeof(PaginatedList<AdminUserViewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsers([FromQuery] GetAdminUsersQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Admin requesting list of all users with query: {@Query}", query);
        var result = await _userService.GetAdminAllUsersAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpPut("users/{userId}/status")] // Route: /api/v1/admin/users/{userId}/status
    [ProducesResponseType(typeof(AdminUserViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserStatus(int userId, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Admin attempting to update status for UserID: {UserId} to IsActive: {IsActive}", userId, request.IsActive);
        var (updatedUser, errorMessage) = await _userService.UpdateUserStatusByAdminAsync(userId, request, cancellationToken);

        if (updatedUser == null)
        {
            if (errorMessage != null && errorMessage.Contains("not found")) return NotFound(new { Message = errorMessage });
            return BadRequest(new { Message = errorMessage ?? "Failed to update user status." });
        }
        return Ok(updatedUser);
    }

    [HttpPut("users/{userId}/role")] // Route: /api/v1/admin/users/{userId}/role
    [ProducesResponseType(typeof(AdminUserViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Admin attempting to update role for UserID: {UserId} to RoleID: {RoleId}", userId, request.RoleId);
        var (updatedUser, errorMessage) = await _userService.UpdateUserRoleByAdminAsync(userId, request, cancellationToken);

        if (updatedUser == null)
        {
            if (errorMessage != null && errorMessage.Contains("not found")) return NotFound(new { Message = errorMessage });
            if (errorMessage != null && errorMessage.Contains("Invalid Role ID")) return BadRequest(new { Message = errorMessage });
            return BadRequest(new { Message = errorMessage ?? "Failed to update user role." });
        }
        return Ok(updatedUser);
    }
    [HttpPost("trading-accounts")] // Route: /api/v1/admin/trading-accounts
    [ProducesResponseType(typeof(TradingAccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTradingAccount([FromBody] CreateTradingAccountRequest request, CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Admin {AdminId} attempting to create trading account: {AccountName}", adminUserId, request.AccountName);

        var (accountDto, errorMessage) = await _tradingAccountService.CreateTradingAccountAsync(request, User, cancellationToken);

        if (accountDto == null)
        {
            if (errorMessage != null && errorMessage.Contains("already exists"))
            {
                return Conflict(new { Message = errorMessage });
            }
            return BadRequest(new { Message = errorMessage ?? "Failed to create trading account." });
        }
        // Trả về 201 Created với DTO và Location header (nếu có endpoint GetById)
        // Giả sử sẽ có endpoint GET /api/v1/trading-accounts/{id} (chưa tạo trong ticket này)
        //return CreatedAtAction("GetTradingAccountById", "TradingAccounts", new { accountId = accountDto.TradingAccountId }, accountDto);
        // Nếu chưa có GetTradingAccountById, dùng:
        return StatusCode(StatusCodes.Status201Created, accountDto);
    }

    [HttpPost("trading-accounts/{accountId}/initial-offerings")] // Route: /api/v1/admin/trading-accounts/{accountId}/initial-offerings
    [ProducesResponseType(typeof(InitialShareOfferingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateInitialShareOffering(int accountId, [FromBody] CreateInitialShareOfferingRequest request, CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Admin {AdminId} attempting to create initial share offering for TradingAccountID: {TradingAccountId}", adminUserId, accountId);

        var (offeringDto, errorMessage) = await _tradingAccountService.CreateInitialShareOfferingAsync(accountId, request, User, cancellationToken);

        if (offeringDto == null)
        {
            if (errorMessage != null && errorMessage.Contains("not found")) return NotFound(new { Message = errorMessage });
            if (errorMessage != null && (errorMessage.Contains("exceeds available shares") || errorMessage.Contains("Invalid"))) return BadRequest(new { Message = errorMessage });
            return BadRequest(new { Message = errorMessage ?? "Failed to create initial share offering." });
        }
        // Trả về 201 Created với DTO và Location header (nếu có endpoint GetOfferingById)
        // return CreatedAtAction("GetOfferingById", new { accountId = accountId, offeringId = offeringDto.OfferingId }, offeringDto);
        return StatusCode(StatusCodes.Status201Created, offeringDto);
    }
}
