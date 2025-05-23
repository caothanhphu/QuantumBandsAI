// QuantumBands.Application/Services/WalletService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // For FirstOrDefaultAsync
using Microsoft.Extensions.Logging;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Wallets.Commands.AdminActions;
using QuantumBands.Application.Features.Wallets.Commands.AdminDeposit;
using QuantumBands.Application.Features.Wallets.Commands.BankDeposit;
using QuantumBands.Application.Features.Wallets.Commands.CreateWithdrawal;
using QuantumBands.Application.Features.Wallets.Commands.InternalTransfer;
using QuantumBands.Application.Features.Wallets.Dtos;
using QuantumBands.Application.Features.Wallets.Queries.GetTransactions; // For Expression
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Interfaces.Repositories;
using QuantumBands.Domain.Entities; // For Wallet entity
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.Application.Services;

public class WalletService : IWalletService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WalletService> _logger;
    private readonly IConfiguration _configuration; // Inject IConfiguration
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ITransactionTypeRepository _transactionTypeRepository;

    public WalletService(
    IUnitOfWork unitOfWork,
    ILogger<WalletService> logger,
    IConfiguration configuration,
    ISystemSettingRepository systemSettingRepository,
    ITransactionTypeRepository transactionTypeRepository)
    // IPayPalService payPalService) // Comment out or remove
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _configuration = configuration;
        _systemSettingRepository = systemSettingRepository;
        _transactionTypeRepository = transactionTypeRepository;
        // _payPalService = payPalService; // Comment out or remove
    }



    private int? GetUserIdFromPrincipal(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }
        var userIdString = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "uid" || c.Type == JwtRegisteredClaimNames.Sub)?.Value;

        if (int.TryParse(userIdString, out var userId))
        {
            return userId;
        }
        return null;
    }

    public async Task<(WalletDto? WalletProfile, string? ErrorMessage)> GetUserWalletAsync(ClaimsPrincipal currentUser, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromPrincipal(currentUser);
        if (!userId.HasValue)
        {
            _logger.LogWarning("GetUserWalletAsync: User is not authenticated or UserId claim is missing.");
            return (null, "User not authenticated or identity is invalid.");
        }

        _logger.LogInformation("Fetching wallet and user email for authenticated UserID: {UserId}", userId.Value);

        var user = await _unitOfWork.Users.Query()
                            .Include(u => u.Wallet) // Nạp thông tin Wallet liên quan
                            .FirstOrDefaultAsync(u => u.UserId == userId.Value, cancellationToken);

        if (user == null || user.Wallet == null)
        {
            _logger.LogWarning("GetUserWalletAsync: User or Wallet not found for UserID {UserId}.", userId.Value);
            return (null, "User or Wallet not found.");
        }

        // Lấy prefix cho QR code từ config, nếu có
        string qrCodePrefix = _configuration["AppSettings:QrCodeEmailPrefix"] ?? ""; // Ví dụ: "mailto:"

        var walletProfileDto = new WalletDto
        {
            WalletId = user.Wallet.WalletId,
            UserId = user.UserId,
            Balance = user.Wallet.Balance,
            CurrencyCode = user.Wallet.CurrencyCode,
            EmailForQrCode = $"{qrCodePrefix}{user.Email}", // Thêm prefix nếu có
            UpdatedAt = user.Wallet.UpdatedAt
        };

        _logger.LogInformation("Wallet profile found for UserID {UserId}. WalletID: {WalletId}", userId.Value, walletProfileDto.WalletId);
        return (walletProfileDto, null);
    }
    public async Task<PaginatedList<WalletTransactionDto>> GetUserWalletTransactionsAsync(
    ClaimsPrincipal currentUser,
    GetWalletTransactionsQuery query,
    CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromPrincipal(currentUser);
        if (!userId.HasValue)
        {
            // Hoặc throw một exception cụ thể
            _logger.LogWarning("GetUserWalletTransactionsAsync: User is not authenticated.");
            return new PaginatedList<WalletTransactionDto>(new List<WalletTransactionDto>(), 0, query.ValidatedPageNumber, query.ValidatedPageSize);
        }

        _logger.LogInformation("Fetching transactions for UserID: {UserId} with query: {@Query}", userId.Value, query);

        var wallet = await _unitOfWork.Wallets.Query()
                             .FirstOrDefaultAsync(w => w.UserId == userId.Value, cancellationToken);

        if (wallet == null)
        {
            _logger.LogWarning("GetUserWalletTransactionsAsync: Wallet not found for UserID {UserId}.", userId.Value);
            return new PaginatedList<WalletTransactionDto>(new List<WalletTransactionDto>(), 0, query.ValidatedPageNumber, query.ValidatedPageSize);
        }

        var transactionsQuery = _unitOfWork.WalletTransactions.Query() // Giả sử có IGenericRepository<WalletTransaction>
                                    .Include(t => t.TransactionType) // Để lấy TransactionTypeName
                                    .Where(t => t.WalletId == wallet.WalletId);

        // Áp dụng Filter
        if (!string.IsNullOrWhiteSpace(query.TransactionType))
        {
            transactionsQuery = transactionsQuery.Where(t => t.TransactionType.TypeName == query.TransactionType);
        }
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            transactionsQuery = transactionsQuery.Where(t => t.Status == query.Status);
        }
        if (query.StartDate.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.TransactionDate >= query.StartDate.Value);
        }
        if (query.EndDate.HasValue)
        {
            // Cộng thêm 1 ngày để bao gồm cả ngày kết thúc
            transactionsQuery = transactionsQuery.Where(t => t.TransactionDate < query.EndDate.Value.AddDays(1));
        }

        // Áp dụng Sắp xếp (ví dụ đơn giản)
        // Cần một giải pháp mạnh mẽ hơn cho việc sắp xếp động dựa trên chuỗi
        if (query.SortOrder?.ToLower() == "desc")
        {
            transactionsQuery = transactionsQuery.OrderByDescending(t => t.TransactionDate); // Mặc định sắp xếp theo ngày giảm dần
        }
        else
        {
            transactionsQuery = transactionsQuery.OrderBy(t => t.TransactionDate);
        }
        // Nếu muốn sắp xếp theo các trường khác, bạn cần thêm logic ở đây

        var paginatedTransactions = await PaginatedList<WalletTransaction>.CreateAsync(
            transactionsQuery,
            query.ValidatedPageNumber,
            query.ValidatedPageSize,
            cancellationToken);

        var transactionDtos = paginatedTransactions.Items.Select(t => new WalletTransactionDto
        {
            TransactionId = t.TransactionId,
            TransactionTypeName = t.TransactionType.TypeName, // Lấy từ TransactionType đã Include
            Amount = t.Amount,
            CurrencyCode = wallet.CurrencyCode, // Lấy từ wallet vì transaction không lưu currency
            BalanceAfter = t.BalanceAfter,
            ReferenceId = t.ReferenceId,
            PaymentMethod = t.PaymentMethod, // Cần thêm cột này vào WalletTransaction entity
            ExternalTransactionId = t.ExternalTransactionId, // Cần thêm cột này
            Description = t.Description,
            Status = t.Status,
            TransactionDate = t.TransactionDate
        }).ToList();

        return new PaginatedList<WalletTransactionDto>(
            transactionDtos,
            paginatedTransactions.TotalCount,
            paginatedTransactions.PageNumber,
            paginatedTransactions.PageSize);
    }
    private async Task<string> GenerateUniqueReferenceCodeAsync(string prefix, CancellationToken cancellationToken)
    {
        string uniquePart;
        string fullReferenceCode;
        int attempts = 0;
        const int maxAttempts = 20; // Increased attempts

        do
        {
            // Tạo phần duy nhất mạnh mẽ hơn, ví dụ: 10-12 ký tự chữ và số ngẫu nhiên
            uniquePart = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper(); // 12 chars
            fullReferenceCode = prefix + uniquePart;
            if (fullReferenceCode.Length > 20)
            {
                fullReferenceCode = fullReferenceCode.Substring(0, 20);
            }
            attempts++;
            if (attempts > maxAttempts)
            {
                _logger.LogError("Failed to generate a unique reference code after {MaxAttempts} attempts with prefix {Prefix}.", maxAttempts, prefix);
                throw new InvalidOperationException("Could not generate a unique reference code. Please try again.");
            }
        }
        // Kiểm tra xem reference code đã tồn tại trong các giao dịch gần đây chưa
        while (await _unitOfWork.WalletTransactions.Query().AnyAsync(wt => wt.ReferenceId == fullReferenceCode, cancellationToken));

        return fullReferenceCode;
    }

    public async Task<(BankDepositInfoResponse? Response, string? ErrorMessage)> InitiateBankDepositAsync(ClaimsPrincipal currentUser, InitiateBankDepositRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromPrincipal(currentUser);
        if (!userId.HasValue) return (null, "User not authenticated.");

        var userWallet = await _unitOfWork.Wallets.Query().FirstOrDefaultAsync(w => w.UserId == userId.Value, cancellationToken);
        if (userWallet == null) return (null, "User wallet not found.");

        if (request.AmountUSD <= 0) return (null, "Deposit amount (USD) must be greater than zero.");

        string? exchangeRateStr = await _systemSettingRepository.GetSettingValueAsync("DepositExchangeRateUSDtoVND", cancellationToken);
        if (!decimal.TryParse(exchangeRateStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal exchangeRate) || exchangeRate <= 0)
        {
            _logger.LogError("Invalid or missing exchange rate 'DepositExchangeRateUSDtoVND' in SystemSettings.");
            return (null, "System error: Exchange rate configuration is invalid or not found.");
        }

        decimal amountVND = Math.Round(request.AmountUSD * exchangeRate, 0);
        string prefix = (await _systemSettingRepository.GetSettingValueAsync("DepositReferenceCodePrefix", cancellationToken)) ?? "FINIXDEP";
        string referenceCode = await GenerateUniqueReferenceCodeAsync(prefix, cancellationToken);

        var depositInitiatedType = await _transactionTypeRepository.GetByNameAsync("BankDepositInitiated", cancellationToken);
        if (depositInitiatedType == null)
        {
            _logger.LogError("TransactionType 'BankDepositInitiated' not found.");
            return (null, "System error: Deposit type configuration missing.");
        }

        var transaction = new WalletTransaction
        {
            WalletId = userWallet.WalletId,
            TransactionTypeId = depositInitiatedType.TransactionTypeId,
            Amount = request.AmountUSD,
            BalanceBefore = userWallet.Balance,
            BalanceAfter = userWallet.Balance,
            CurrencyCode = "USD",
            ReferenceId = referenceCode,
            Description = $"Bank deposit request for {request.AmountUSD:F2} USD. VND equivalent: {amountVND:N0} VND (Rate: {exchangeRate}). Ref: {referenceCode}",
            Status = "PendingBankTransfer",
            PaymentMethod = "BankTransfer",
            TransactionDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.WalletTransactions.AddAsync(transaction);
        await _unitOfWork.CompleteAsync(cancellationToken);
        _logger.LogInformation("Bank deposit initiated for UserID {UserId}, AmountUSD {AmountUSD}, RefCode {ReferenceCode}, TransactionID {TransactionId}",
                               userId.Value, request.AmountUSD, referenceCode, transaction.TransactionId);

        var response = new BankDepositInfoResponse
        {
            TransactionId = transaction.TransactionId,
            RequestedAmountUSD = request.AmountUSD,
            AmountVND = amountVND,
            ExchangeRate = exchangeRate,
            BankName = (await _systemSettingRepository.GetSettingValueAsync("DepositBankAccountName", cancellationToken)) ?? "N/A",
            AccountHolder = (await _systemSettingRepository.GetSettingValueAsync("DepositBankAccountHolder", cancellationToken)) ?? "N/A",
            AccountNumber = (await _systemSettingRepository.GetSettingValueAsync("DepositBankAccountNumber", cancellationToken)) ?? "N/A",
            ReferenceCode = referenceCode
        };
        return (response, null);
    }

    public async Task<(WalletTransactionDto? Transaction, string? ErrorMessage)> ConfirmBankDepositAsync(ClaimsPrincipal adminUser, ConfirmBankDepositRequest request, CancellationToken cancellationToken = default)
    {
        var adminUserId = GetUserIdFromPrincipal(adminUser);
        if (!adminUserId.HasValue) return (null, "Admin user not authenticated.");
        // Thêm kiểm tra vai trò Admin ở đây hoặc trong controller

        var transaction = await _unitOfWork.WalletTransactions.Query()
                                .Include(t => t.Wallet) // Cần Wallet để cập nhật số dư
                                .Include(t => t.TransactionType)
                                .FirstOrDefaultAsync(t => t.TransactionId == request.TransactionId, cancellationToken);

        if (transaction == null) return (null, "Deposit transaction not found.");
        if (transaction.Status != "PendingBankTransfer") // Chỉ xác nhận các giao dịch đang chờ
        {
            return (null, $"Transaction is not pending bank transfer confirmation. Current status: {transaction.Status}");
        }
        if (transaction.Wallet == null) // Kiểm tra null cho wallet
        {
            _logger.LogError("Wallet not found for TransactionID {TransactionId} during confirmation.", request.TransactionId);
            return (null, "Associated wallet not found for the transaction.");
        }


        var depositCompletedType = await _transactionTypeRepository.GetByNameAsync("BankDepositCompleted", cancellationToken);
        if (depositCompletedType == null)
        {
            _logger.LogError("TransactionType 'BankDepositCompleted' not found.");
            return (null, "System error: Deposit type configuration missing.");
        }

        transaction.Status = "Completed";
        transaction.TransactionTypeId = depositCompletedType.TransactionTypeId;
        transaction.BalanceAfter = transaction.Wallet.Balance + transaction.Amount; // Amount là USD
        transaction.Description = $"{transaction.Description} | Confirmed by Admin {adminUserId.Value}. Notes: {request.AdminNotes ?? "N/A"}";
        if (request.ActualAmountVNDReceived.HasValue)
        {
            transaction.Description += $" | Actual VND received: {request.ActualAmountVNDReceived.Value:N0} VND.";
        }
        transaction.UpdatedAt = DateTime.UtcNow;

        transaction.Wallet.Balance += transaction.Amount;
        transaction.Wallet.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.WalletTransactions.Update(transaction);
        _unitOfWork.Wallets.Update(transaction.Wallet);
        await _unitOfWork.CompleteAsync(cancellationToken);

        _logger.LogInformation("Bank deposit TransactionID {TransactionId} confirmed by Admin {AdminUserId}. UserID {UserId} wallet updated.",
                               request.TransactionId, adminUserId.Value, transaction.Wallet.UserId);

        return (new WalletTransactionDto
        {
            TransactionId = transaction.TransactionId,
            TransactionTypeName = depositCompletedType.TypeName,
            Amount = transaction.Amount,
            CurrencyCode = transaction.CurrencyCode,
            BalanceAfter = transaction.BalanceAfter,
            ReferenceId = transaction.ReferenceId,
            PaymentMethod = transaction.PaymentMethod,
            Description = transaction.Description,
            Status = transaction.Status,
            TransactionDate = transaction.TransactionDate
        }, null);
    }

    public async Task<(WalletTransactionDto? Transaction, string? ErrorMessage)> CancelBankDepositAsync(ClaimsPrincipal adminUser, CancelBankDepositRequest request, CancellationToken cancellationToken = default)
    {
        var adminUserId = GetUserIdFromPrincipal(adminUser);
        if (!adminUserId.HasValue) return (null, "Admin user not authenticated.");

        var transaction = await _unitOfWork.WalletTransactions.Query()
                                .Include(t => t.TransactionType)
                                .FirstOrDefaultAsync(t => t.TransactionId == request.TransactionId, cancellationToken);

        if (transaction == null) return (null, "Deposit transaction not found.");
        if (transaction.Status != "PendingBankTransfer")
        {
            return (null, $"Transaction cannot be cancelled. Current status: {transaction.Status}");
        }

        var depositCancelledType = await _transactionTypeRepository.GetByNameAsync("BankDepositCancelled", cancellationToken);
        if (depositCancelledType == null)
        {
            _logger.LogError("TransactionType 'BankDepositCancelled' not found.");
            return (null, "System error: Deposit type configuration missing.");
        }

        transaction.Status = "Cancelled";
        transaction.TransactionTypeId = depositCancelledType.TransactionTypeId;
        transaction.Description = $"{transaction.Description?.TrimEnd()} | Cancelled by Admin {adminUserId.Value}. Reason: {request.AdminNotes}";
        // BalanceAfter giữ nguyên như BalanceBefore vì không có tiền vào
        transaction.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.WalletTransactions.Update(transaction);
        await _unitOfWork.CompleteAsync(cancellationToken);

        _logger.LogInformation("Bank deposit TransactionID {TransactionId} cancelled by Admin {AdminUserId}.", request.TransactionId, adminUserId.Value);

        return (new WalletTransactionDto
        {
            TransactionId = transaction.TransactionId,
            TransactionTypeName = depositCancelledType.TypeName,
            Amount = transaction.Amount,
            CurrencyCode = transaction.CurrencyCode,
            BalanceAfter = transaction.BalanceAfter,
            ReferenceId = transaction.ReferenceId,
            PaymentMethod = transaction.PaymentMethod,
            Description = transaction.Description,
            Status = transaction.Status,
            TransactionDate = transaction.TransactionDate
        }, null);
    }

    public async Task<(WalletTransactionDto? Transaction, string? ErrorMessage)> AdminDirectDepositAsync(ClaimsPrincipal adminUser, AdminDirectDepositRequest request, CancellationToken cancellationToken = default)
    {
        var adminUserId = GetUserIdFromPrincipal(adminUser);
        if (!adminUserId.HasValue) return (null, "Admin user not authenticated.");

        var admin = await _unitOfWork.Users.Query().Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == adminUserId.Value, cancellationToken);
        if (admin == null || admin.Role?.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) != true)
        {
            return (null, "Unauthorized: Admin role required.");
        }

        var targetUser = await _unitOfWork.Users.GetByIdAsync(request.UserId);
        if (targetUser == null) return (null, "Target user not found.");

        var targetWallet = await _unitOfWork.Wallets.Query().FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);
        if (targetWallet == null) return (null, "Target user's wallet not found.");

        if (request.Amount <= 0) return (null, "Deposit amount must be positive.");
        if (string.IsNullOrWhiteSpace(request.CurrencyCode) || request.CurrencyCode.ToUpper() != "USD")
        {
            return (null, "Invalid or unsupported currency code. Only USD is supported for direct deposits.");
        }


        var adminDepositType = await _transactionTypeRepository.GetByNameAsync("AdminManualDeposit", cancellationToken);
        if (adminDepositType == null)
        {
            _logger.LogError("TransactionType 'AdminManualDeposit' not found.");
            return (null, "System error: Deposit type configuration missing.");
        }

        var transaction = new WalletTransaction
        {
            WalletId = targetWallet.WalletId,
            TransactionTypeId = adminDepositType.TransactionTypeId,
            Amount = request.Amount,
            BalanceBefore = targetWallet.Balance,
            BalanceAfter = targetWallet.Balance + request.Amount,
            CurrencyCode = request.CurrencyCode.ToUpper(),
            Description = request.Description,
            ReferenceId = request.ReferenceId,
            Status = "Completed",
            PaymentMethod = "AdminCredit",
            TransactionDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        targetWallet.Balance += request.Amount;
        targetWallet.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.WalletTransactions.AddAsync(transaction);
        _unitOfWork.Wallets.Update(targetWallet);

        await _unitOfWork.CompleteAsync(cancellationToken);
        _logger.LogInformation("Admin {AdminUserId} directly deposited {Amount} {Currency} to UserID {TargetUserId}. TransactionID: {TransactionId}", adminUserId.Value, request.Amount, request.CurrencyCode, request.UserId, transaction.TransactionId);

        return (new WalletTransactionDto
        {
            TransactionId = transaction.TransactionId,
            TransactionTypeName = adminDepositType.TypeName,
            Amount = transaction.Amount,
            CurrencyCode = transaction.CurrencyCode,
            BalanceAfter = transaction.BalanceAfter,
            ReferenceId = transaction.ReferenceId,
            PaymentMethod = transaction.PaymentMethod,
            Description = transaction.Description,
            Status = transaction.Status,
            TransactionDate = transaction.TransactionDate
        }, null);
    }
    public async Task<(WithdrawalRequestDto? Response, string? ErrorMessage)> CreateWithdrawalRequestAsync(ClaimsPrincipal currentUser, CreateWithdrawalRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromPrincipal(currentUser);
        if (!userId.HasValue)
        {
            _logger.LogWarning("CreateWithdrawalRequestAsync: User is not authenticated or UserId claim is missing.");
            return (null, "User not authenticated or identity is invalid.");
        }

        _logger.LogInformation("User {UserId} initiating withdrawal request for Amount: {Amount} {Currency}", userId.Value, request.Amount, request.CurrencyCode);

        var userWallet = await _unitOfWork.Wallets.Query().FirstOrDefaultAsync(w => w.UserId == userId.Value, cancellationToken);
        if (userWallet == null)
        {
            _logger.LogWarning("CreateWithdrawalRequestAsync: Wallet not found for UserID {UserId}.", userId.Value);
            return (null, "User wallet not found.");
        }

        // 1. Kiểm tra số dư ví
        if (userWallet.Balance < request.Amount)
        {
            _logger.LogWarning("CreateWithdrawalRequestAsync: Insufficient balance for UserID {UserId}. Requested: {RequestedAmount}, Available: {AvailableBalance}",
                               userId.Value, request.Amount, userWallet.Balance);
            return (null, "Insufficient wallet balance to perform this withdrawal.");
        }

        // 2. Lấy TransactionType cho "WithdrawalPending" (hoặc tên tương tự)
        // Đảm bảo TransactionType này có IsCredit = false (vì là trừ tiền khỏi ví)
        var withdrawalPendingType = await _transactionTypeRepository.GetByNameAsync("WithdrawalPending", cancellationToken); // Hoặc "WithdrawalRequested"
        if (withdrawalPendingType == null)
        {
            _logger.LogError("TransactionType 'WithdrawalPending' not found in the database.");
            return (null, "System error: Withdrawal type configuration missing.");
        }
        if (withdrawalPendingType.IsCredit) // Double check
        {
            _logger.LogError("TransactionType 'WithdrawalPending' is incorrectly configured as a credit type.");
            return (null, "System error: Withdrawal type configuration error.");
        }


        // 3. Tạo bản ghi WalletTransaction
        var transaction = new WalletTransaction
        {
            WalletId = userWallet.WalletId,
            TransactionTypeId = withdrawalPendingType.TransactionTypeId,
            Amount = request.Amount, // Số tiền yêu cầu rút
            CurrencyCode = request.CurrencyCode.ToUpper(),
            BalanceBefore = userWallet.Balance,
            // BalanceAfter sẽ được cập nhật khi Admin duyệt. Hiện tại, nó vẫn là balance cũ
            // vì tiền chưa thực sự bị trừ khỏi ví cho đến khi admin xác nhận.
            // Tuy nhiên, một số hệ thống có thể chọn "tạm giữ" số tiền này bằng cách trừ nó khỏi "available_balance"
            // nhưng không thay đổi "actual_balance". Để đơn giản, chúng ta giữ "actual_balance".
            BalanceAfter = userWallet.Balance,
            WithdrawalMethodDetails = request.WithdrawalMethodDetails, // Lưu trực tiếp
            UserProvidedNotes = request.Notes, // Lưu trực tiếp
            Description = $"Withdrawal request. Details: {request.WithdrawalMethodDetails}. Notes: {request.Notes ?? "N/A"}",
            Status = "PendingAdminApproval",
            PaymentMethod = "Withdrawal", // Hoặc cụ thể hơn như "BankWithdrawal"
            TransactionDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
            // ReferenceID có thể được tạo ở đây hoặc để trống cho Admin điền khi xử lý
        };

        await _unitOfWork.WalletTransactions.AddAsync(transaction);
        await _unitOfWork.CompleteAsync(cancellationToken);

        _logger.LogInformation("Withdrawal request created successfully for UserID {UserId}. TransactionID: {TransactionId}, Amount: {Amount}",
                               userId.Value, transaction.TransactionId, request.Amount);

        var responseDto = new WithdrawalRequestDto
        {
            WithdrawalRequestId = transaction.TransactionId,
            UserId = userId.Value,
            Amount = transaction.Amount,
            CurrencyCode = transaction.CurrencyCode,
            Status = transaction.Status,
            WithdrawalMethodDetails = request.WithdrawalMethodDetails,
            Notes = request.Notes,
            RequestedAt = transaction.TransactionDate
        };

        return (responseDto, null);
    }
    public async Task<(WalletTransactionDto? Transaction, string? ErrorMessage)> ApproveWithdrawalAsync(ClaimsPrincipal adminUser, ApproveWithdrawalRequest request, CancellationToken cancellationToken = default)
    {
        var adminUserId = GetUserIdFromPrincipal(adminUser);
        if (!adminUserId.HasValue) return (null, "Admin user not authenticated.");
        // Thêm kiểm tra vai trò Admin ở đây hoặc để controller xử lý

        _logger.LogInformation("Admin {AdminUserId} attempting to approve withdrawal TransactionID: {TransactionId}", adminUserId.Value, request.TransactionId);

        var transaction = await _unitOfWork.WalletTransactions.Query()
                                .Include(t => t.Wallet) // Cần Wallet để cập nhật số dư
                                .Include(t => t.TransactionType) // Để lấy tên cho DTO
                                .FirstOrDefaultAsync(t => t.TransactionId == request.TransactionId, cancellationToken);

        if (transaction == null) return (null, "Withdrawal transaction not found.");
        if (transaction.Status != "PendingAdminApproval")
        {
            return (null, $"Transaction is not pending approval. Current status: {transaction.Status}");
        }
        if (transaction.Wallet == null)
        {
            _logger.LogError("Wallet not found for TransactionID {TransactionId} during approval.", request.TransactionId);
            return (null, "Associated wallet not found for the transaction.");
        }
        // Đảm bảo TransactionType "WithdrawalPending" đã được lấy đúng khi tạo request
        if (transaction.TransactionType?.IsCredit == true) // IsCredit=0 (false) cho withdrawal
        {
            _logger.LogError("TransactionType for withdrawal (ID: {TransactionTypeId}) is incorrectly configured as a credit type.", transaction.TransactionTypeId);
            return (null, "System error: Withdrawal type configuration error.");
        }


        // Kiểm tra lại số dư trước khi trừ (mặc dù đã kiểm tra khi user tạo request)
        if (transaction.Wallet.Balance < transaction.Amount)
        {
            _logger.LogWarning("Insufficient balance for UserID {UserId} during withdrawal approval. Requested: {RequestedAmount}, Available: {AvailableBalance}. TransactionID: {TransactionId}",
                               transaction.Wallet.UserId, transaction.Amount, transaction.Wallet.Balance, transaction.TransactionId);
            // Cân nhắc: Có nên hủy giao dịch ở đây không nếu số dư không đủ?
            // Hoặc chỉ báo lỗi cho Admin. Hiện tại báo lỗi.
            return (null, "Insufficient wallet balance at the time of approval. User's balance might have changed.");
        }

        var withdrawalCompletedType = await _transactionTypeRepository.GetByNameAsync("WithdrawalCompleted", cancellationToken);
        if (withdrawalCompletedType == null)
        {
            _logger.LogError("TransactionType 'WithdrawalCompleted' not found.");
            return (null, "System error: Withdrawal type configuration missing.");
        }

        // Trừ tiền khỏi ví
        transaction.Wallet.Balance -= transaction.Amount;
        transaction.Wallet.UpdatedAt = DateTime.UtcNow;

        // Cập nhật giao dịch
        transaction.Status = "Completed";
        transaction.TransactionTypeId = withdrawalCompletedType.TransactionTypeId; // Cập nhật loại giao dịch
        transaction.BalanceAfter = transaction.Wallet.Balance; // Số dư mới sau khi trừ
        transaction.Description = $"{transaction.Description?.TrimEnd()} | Approved by Admin {adminUserId.Value}. Notes: {request.AdminNotes ?? "N/A"}";
        if (!string.IsNullOrEmpty(request.ExternalTransactionReference))
        {
            transaction.ExternalTransactionId = request.ExternalTransactionReference;
            transaction.Description += $" | Ext. Ref: {request.ExternalTransactionReference}";
        }
        transaction.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Wallets.Update(transaction.Wallet);
        _unitOfWork.WalletTransactions.Update(transaction);
        await _unitOfWork.CompleteAsync(cancellationToken);

        _logger.LogInformation("Withdrawal TransactionID {TransactionId} approved by Admin {AdminUserId}. UserID {UserId} wallet updated. New balance: {NewBalance}",
                               request.TransactionId, adminUserId.Value, transaction.Wallet.UserId, transaction.Wallet.Balance);

        return (new WalletTransactionDto
        {
            TransactionId = transaction.TransactionId,
            TransactionTypeName = withdrawalCompletedType.TypeName,
            Amount = transaction.Amount,
            CurrencyCode = transaction.CurrencyCode,
            BalanceAfter = transaction.BalanceAfter,
            ReferenceId = transaction.ReferenceId,
            PaymentMethod = transaction.PaymentMethod,
            ExternalTransactionId = transaction.ExternalTransactionId,
            Description = transaction.Description,
            Status = transaction.Status,
            TransactionDate = transaction.TransactionDate, // Ngày tạo yêu cầu ban đầu
            UpdatedAt = transaction.UpdatedAt
        }, null);
    }

    public async Task<(WalletTransactionDto? Transaction, string? ErrorMessage)> RejectWithdrawalAsync(ClaimsPrincipal adminUser, RejectWithdrawalRequest request, CancellationToken cancellationToken = default)
    {
        var adminUserId = GetUserIdFromPrincipal(adminUser);
        if (!adminUserId.HasValue) return (null, "Admin user not authenticated.");

        _logger.LogInformation("Admin {AdminUserId} attempting to reject withdrawal TransactionID: {TransactionId}", adminUserId.Value, request.TransactionId);

        var transaction = await _unitOfWork.WalletTransactions.Query()
                                .Include(t => t.Wallet) // Wallet không thay đổi balance nhưng có thể cần cho DTO
                                .Include(t => t.TransactionType)
                                .FirstOrDefaultAsync(t => t.TransactionId == request.TransactionId, cancellationToken);

        if (transaction == null) return (null, "Withdrawal transaction not found.");
        if (transaction.Status != "PendingAdminApproval")
        {
            return (null, $"Transaction cannot be rejected. Current status: {transaction.Status}");
        }

        var withdrawalRejectedType = await _transactionTypeRepository.GetByNameAsync("WithdrawalRejected", cancellationToken);
        if (withdrawalRejectedType == null)
        {
            _logger.LogError("TransactionType 'WithdrawalRejected' not found.");
            return (null, "System error: Withdrawal type configuration missing.");
        }

        transaction.Status = "Rejected";
        transaction.TransactionTypeId = withdrawalRejectedType.TransactionTypeId;
        transaction.Description = $"{transaction.Description?.TrimEnd()} | Rejected by Admin {adminUserId.Value}. Reason: {request.AdminNotes}";
        // BalanceAfter và Wallet.Balance không thay đổi
        transaction.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.WalletTransactions.Update(transaction);
        await _unitOfWork.CompleteAsync(cancellationToken);

        _logger.LogInformation("Withdrawal TransactionID {TransactionId} rejected by Admin {AdminUserId}.", request.TransactionId, adminUserId.Value);

        return (new WalletTransactionDto
        {
            TransactionId = transaction.TransactionId,
            TransactionTypeName = withdrawalRejectedType.TypeName,
            Amount = transaction.Amount,
            CurrencyCode = transaction.CurrencyCode,
            BalanceAfter = transaction.BalanceAfter, // Sẽ giống BalanceBefore của giao dịch này
            ReferenceId = transaction.ReferenceId,
            PaymentMethod = transaction.PaymentMethod,
            ExternalTransactionId = transaction.ExternalTransactionId,
            Description = transaction.Description,
            Status = transaction.Status,
            TransactionDate = transaction.TransactionDate,
            UpdatedAt = transaction.UpdatedAt
        }, null);
    }
    public async Task<(RecipientInfoResponse? RecipientInfo, string? ErrorMessage)> VerifyRecipientForTransferAsync(VerifyRecipientRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Verifying recipient with email: {RecipientEmail}", request.RecipientEmail);

        var recipientUser = await _unitOfWork.Users.Query()
                                    .AsNoTracking() // Không cần theo dõi thay đổi cho truy vấn này
                                    .FirstOrDefaultAsync(u => u.Email == request.RecipientEmail && u.IsActive, cancellationToken);

        if (recipientUser == null)
        {
            _logger.LogWarning("Recipient email {RecipientEmail} not found or user is inactive.", request.RecipientEmail);
            return (null, "Recipient email not found or user is inactive.");
        }

        // (Tùy chọn) Kiểm tra xem email người nhận đã được xác thực chưa nếu đó là yêu cầu
        // if (!recipientUser.IsEmailVerified)
        // {
        //     _logger.LogWarning("Recipient email {RecipientEmail} (UserID: {UserId}) is not verified.", request.RecipientEmail, recipientUser.UserId);
        //     return (null, "Recipient email is not verified.");
        // }

        var response = new RecipientInfoResponse
        {
            RecipientUserId = recipientUser.UserId,
            RecipientUsername = recipientUser.Username,
            RecipientFullName = recipientUser.FullName
        };

        _logger.LogInformation("Recipient verified: UserID {UserId}, Username {Username}", response.RecipientUserId, response.RecipientUsername);
        return (response, null);
    }

    public async Task<(WalletTransactionDto? SenderTransaction, string? ErrorMessage)> ExecuteInternalTransferAsync(ClaimsPrincipal senderUserPrincipal, ExecuteInternalTransferRequest request, CancellationToken cancellationToken = default)
    {
        var senderUserId = GetUserIdFromPrincipal(senderUserPrincipal);
        if (!senderUserId.HasValue)
        {
            return (null, "Sender not authenticated or identity is invalid.");
        }

        _logger.LogInformation("UserID {SenderUserId} attempting internal transfer to UserID {RecipientUserId} for Amount {Amount} {Currency}",
                               senderUserId.Value, request.RecipientUserId, request.Amount, request.CurrencyCode);

        if (senderUserId.Value == request.RecipientUserId)
        {
            _logger.LogWarning("UserID {SenderUserId} attempted to transfer funds to self.", senderUserId.Value);
            return (null, "Cannot transfer funds to yourself.");
        }

        // Lấy thông tin ví và user của người gửi và người nhận trong một transaction
        // Điều này quan trọng để đảm bảo tính nhất quán và tránh race condition.
        // Tuy nhiên, UnitOfWork sẽ quản lý transaction khi gọi CompleteAsync.
        // Chúng ta cần nạp cả User và Wallet để có đủ thông tin.

        var sender = await _unitOfWork.Users.Query()
                            .Include(u => u.Wallet)
                            .FirstOrDefaultAsync(u => u.UserId == senderUserId.Value, cancellationToken);

        var recipient = await _unitOfWork.Users.Query()
                                .Include(u => u.Wallet)
                                .FirstOrDefaultAsync(u => u.UserId == request.RecipientUserId && u.IsActive, cancellationToken); // Chỉ chuyển cho user active

        if (sender == null || sender.Wallet == null)
        {
            _logger.LogError("Sender (UserID: {SenderUserId}) or their wallet not found.", senderUserId.Value);
            return (null, "Sender's wallet information is missing or invalid.");
        }
        if (recipient == null || recipient.Wallet == null)
        {
            _logger.LogWarning("Recipient (UserID: {RecipientUserId}) or their wallet not found, or recipient is inactive.", request.RecipientUserId);
            return (null, "Recipient user or their wallet not found, or recipient is inactive.");
        }

        // Kiểm tra số dư người gửi
        if (sender.Wallet.Balance < request.Amount)
        {
            _logger.LogWarning("Insufficient balance for UserID {SenderUserId}. Requested: {RequestedAmount}, Available: {AvailableBalance}",
                               senderUserId.Value, request.Amount, sender.Wallet.Balance);
            return (null, "Insufficient wallet balance.");
        }

        // Lấy TransactionTypes
        var transferSentType = await _transactionTypeRepository.GetByNameAsync("InternalTransferSent", cancellationToken);
        var transferReceivedType = await _transactionTypeRepository.GetByNameAsync("InternalTransferReceived", cancellationToken);

        if (transferSentType == null || transferReceivedType == null)
        {
            _logger.LogError("Required TransactionTypes 'InternalTransferSent' or 'InternalTransferReceived' not found.");
            return (null, "System error: Internal transfer type configuration missing.");
        }
        if (transferSentType.IsCredit || !transferReceivedType.IsCredit)
        {
            _logger.LogError("TransactionTypes 'InternalTransferSent' (IsCredit=false expected) or 'InternalTransferReceived' (IsCredit=true expected) are misconfigured.");
            return (null, "System error: Internal transfer type configuration error.");
        }


        var now = DateTime.UtcNow;
        string commonReferenceId = $"INT_TR_{Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper()}"; // Tạo một ID chung cho cặp giao dịch

        // Tạo giao dịch ghi nợ cho người gửi
        var senderTransaction = new WalletTransaction
        {
            WalletId = sender.Wallet.WalletId,
            TransactionTypeId = transferSentType.TransactionTypeId,
            Amount = request.Amount,
            CurrencyCode = request.CurrencyCode.ToUpper(),
            BalanceBefore = sender.Wallet.Balance,
            BalanceAfter = sender.Wallet.Balance - request.Amount,
            Description = $"Sent to {recipient.Email} (User ID: {recipient.UserId}). Notes: {request.Description ?? "N/A"}",
            ReferenceId = commonReferenceId, // Hoặc ID của recipient transaction
            Status = "Completed",
            PaymentMethod = "InternalTransfer",
            TransactionDate = now,
            UpdatedAt = now
        };

        // Tạo giao dịch ghi có cho người nhận
        var recipientTransaction = new WalletTransaction
        {
            WalletId = recipient.Wallet.WalletId,
            TransactionTypeId = transferReceivedType.TransactionTypeId,
            Amount = request.Amount,
            CurrencyCode = request.CurrencyCode.ToUpper(),
            BalanceBefore = recipient.Wallet.Balance,
            BalanceAfter = recipient.Wallet.Balance + request.Amount,
            Description = $"Received from {sender.Email} (User ID: {sender.UserId}). Notes: {request.Description ?? "N/A"}",
            ReferenceId = commonReferenceId, // Hoặc ID của sender transaction
            Status = "Completed",
            PaymentMethod = "InternalTransfer",
            TransactionDate = now,
            UpdatedAt = now
        };

        // Cập nhật số dư ví
        sender.Wallet.Balance -= request.Amount;
        sender.Wallet.UpdatedAt = now;

        recipient.Wallet.Balance += request.Amount;
        recipient.Wallet.UpdatedAt = now;

        // Thêm vào Unit of Work
        await _unitOfWork.WalletTransactions.AddAsync(senderTransaction);
        await _unitOfWork.WalletTransactions.AddAsync(recipientTransaction);
        _unitOfWork.Wallets.Update(sender.Wallet);
        _unitOfWork.Wallets.Update(recipient.Wallet);

        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken); // Lưu tất cả trong một transaction
            _logger.LogInformation("Internal transfer successful: {Amount} {Currency} from UserID {SenderId} to UserID {RecipientId}. Sender TxID: {SenderTxId}, Recipient TxID: {RecipientTxId}",
                                   request.Amount, request.CurrencyCode, senderUserId.Value, request.RecipientUserId, senderTransaction.TransactionId, recipientTransaction.TransactionId);

            // Liên kết hai giao dịch (tùy chọn)
            senderTransaction.RelatedTransactionId = recipientTransaction.TransactionId;
            recipientTransaction.RelatedTransactionId = senderTransaction.TransactionId;
            _unitOfWork.WalletTransactions.Update(senderTransaction);
            _unitOfWork.WalletTransactions.Update(recipientTransaction);
            await _unitOfWork.CompleteAsync(cancellationToken);


            var senderTransactionDto = new WalletTransactionDto
            {
                TransactionId = senderTransaction.TransactionId,
                TransactionTypeName = transferSentType.TypeName,
                Amount = senderTransaction.Amount,
                CurrencyCode = senderTransaction.CurrencyCode,
                BalanceAfter = senderTransaction.BalanceAfter,
                ReferenceId = $"TRANSFER_TO_USER_{recipient.UserId}", // Điều chỉnh ReferenceID cho response
                PaymentMethod = senderTransaction.PaymentMethod,
                Description = senderTransaction.Description,
                Status = senderTransaction.Status,
                TransactionDate = senderTransaction.TransactionDate,
                UpdatedAt = senderTransaction.UpdatedAt
            };
            return (senderTransactionDto, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during internal transfer execution between UserID {SenderId} and UserID {RecipientId}.", senderUserId.Value, request.RecipientUserId);
            return (null, "An error occurred while executing the transfer.");
        }
    }
    public async Task<PaginatedList<AdminPendingBankDepositDto>> GetAdminPendingBankDepositsAsync(
        ClaimsPrincipal adminUser,
        GetAdminPendingBankDepositsQuery query,
        CancellationToken cancellationToken = default)
    {
        var adminUserId = GetUserIdFromPrincipal(adminUser);
        if (!adminUserId.HasValue)
        {
            _logger.LogWarning("GetAdminPendingBankDepositsAsync: Admin user not authenticated.");
            // Trả về danh sách rỗng hoặc throw exception tùy theo chính sách của bạn
            return new PaginatedList<AdminPendingBankDepositDto>(new List<AdminPendingBankDepositDto>(), 0, query.ValidatedPageNumber, query.ValidatedPageSize);
        }
        // Thêm kiểm tra vai trò Admin ở đây nếu controller chưa đủ chặt chẽ

        _logger.LogInformation("Admin {AdminUserId} fetching pending bank deposits with query: {@Query}", adminUserId.Value, query);

        var transactionsQuery = _unitOfWork.WalletTransactions.Query()
                                    .Include(t => t.Wallet) // Cần Wallet để lấy UserId
                                        .ThenInclude(w => w.User) // Từ Wallet lấy User để có Username, Email
                                    .Include(t => t.TransactionType) // Để lấy TransactionTypeName (dù có thể không cần nếu chỉ lọc theo status code)
                                    .Where(t => t.PaymentMethod == "BankTransfer" &&
                                                (t.Status == "PendingBankTransfer" || t.Status == "PendingAdminConfirmation")); // Các trạng thái chờ

        // Áp dụng Filter
        if (query.UserId.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.Wallet.UserId == query.UserId.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.UsernameOrEmail))
        {
            string searchTerm = query.UsernameOrEmail.ToLower();
            transactionsQuery = transactionsQuery.Where(t => t.Wallet.User.Username.ToLower().Contains(searchTerm) ||
                                                           t.Wallet.User.Email.ToLower().Contains(searchTerm));
        }
        if (!string.IsNullOrWhiteSpace(query.ReferenceCode))
        {
            transactionsQuery = transactionsQuery.Where(t => t.ReferenceId == query.ReferenceCode);
        }
        if (query.MinAmountUSD.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.Amount >= query.MinAmountUSD.Value);
        }
        if (query.MaxAmountUSD.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.Amount <= query.MaxAmountUSD.Value);
        }
        if (query.DateFrom.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.TransactionDate >= query.DateFrom.Value.Date); // Bắt đầu từ 00:00:00 của DateFrom
        }
        if (query.DateTo.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.TransactionDate < query.DateTo.Value.Date.AddDays(1)); // Đến cuối ngày DateTo
        }

        // Áp dụng Sắp xếp
        bool isDescending = query.SortOrder?.ToLower() == "desc";
        Expression<Func<WalletTransaction, object>> orderByExpression;

        switch (query.SortBy?.ToLowerInvariant())
        {
            case "amount":
                orderByExpression = t => t.Amount;
                break;
            case "userid":
                orderByExpression = t => t.Wallet.UserId;
                break;
            case "referenceid":
                orderByExpression = t => t.ReferenceId!; // Thêm ! nếu bạn chắc chắn nó không null khi sort
                break;
            case "transactiondate":
            default:
                orderByExpression = t => t.TransactionDate;
                break;
        }
        transactionsQuery = isDescending
            ? transactionsQuery.OrderByDescending(orderByExpression)
            : transactionsQuery.OrderBy(orderByExpression);

        var paginatedTransactions = await PaginatedList<WalletTransaction>.CreateAsync(
            transactionsQuery,
            query.ValidatedPageNumber,
            query.ValidatedPageSize,
            cancellationToken);

        var dtos = paginatedTransactions.Items.Select(t =>
        {
            // Trích xuất AmountVND và ExchangeRate từ Description
            decimal? amountVND = null;
            decimal? exchangeRate = null;
            if (!string.IsNullOrEmpty(t.Description))
            {
                var vndMatch = Regex.Match(t.Description, @"VND equivalent: ([\d,]+) VND");
                if (vndMatch.Success && decimal.TryParse(vndMatch.Groups[1].Value.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal vndVal))
                {
                    amountVND = vndVal;
                }
                var rateMatch = Regex.Match(t.Description, @"Rate: ([\d.,]+)");
                if (rateMatch.Success && decimal.TryParse(rateMatch.Groups[1].Value.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal rateVal))
                {
                    exchangeRate = rateVal;
                }
            }

            return new AdminPendingBankDepositDto
            {
                TransactionId = t.TransactionId,
                UserId = t.Wallet.UserId,
                Username = t.Wallet.User.Username,
                UserEmail = t.Wallet.User.Email,
                AmountUSD = t.Amount,
                CurrencyCode = t.CurrencyCode,
                AmountVND = amountVND,
                ExchangeRate = exchangeRate,
                ReferenceCode = t.ReferenceId,
                PaymentMethod = t.PaymentMethod ?? "N/A",
                Status = t.Status,
                TransactionDate = t.TransactionDate,
                UpdatedAt = t.UpdatedAt,
                Description = t.Description
            };
        }).ToList();

        return new PaginatedList<AdminPendingBankDepositDto>(
            dtos,
            paginatedTransactions.TotalCount,
            paginatedTransactions.PageNumber,
            paginatedTransactions.PageSize);
    }

    public async Task<PaginatedList<WithdrawalRequestAdminViewDto>> GetAdminPendingWithdrawalsAsync(
    ClaimsPrincipal adminUser,
    GetAdminPendingWithdrawalsQuery query,
    CancellationToken cancellationToken = default)
    {
        var adminUserId = GetUserIdFromPrincipal(adminUser);
        if (!adminUserId.HasValue)
        {
            _logger.LogWarning("GetAdminPendingWithdrawalsAsync: Admin user not authenticated.");
            return new PaginatedList<WithdrawalRequestAdminViewDto>(new List<WithdrawalRequestAdminViewDto>(), 0, query.ValidatedPageNumber, query.ValidatedPageSize);
        }
        // Thêm kiểm tra vai trò Admin ở đây nếu controller chưa đủ chặt chẽ

        _logger.LogInformation("Admin {AdminUserId} fetching pending withdrawal requests with query: {@Query}", adminUserId.Value, query);

        // Lấy TransactionType "WithdrawalPending" để lọc chính xác
        var withdrawalPendingType = await _transactionTypeRepository.GetByNameAsync("WithdrawalPending", cancellationToken);
        if (withdrawalPendingType == null)
        {
            _logger.LogError("TransactionType 'WithdrawalPending' not found. Cannot fetch pending withdrawals.");
            return new PaginatedList<WithdrawalRequestAdminViewDto>(new List<WithdrawalRequestAdminViewDto>(), 0, query.ValidatedPageNumber, query.ValidatedPageSize);
        }

        var transactionsQuery = _unitOfWork.WalletTransactions.Query()
                                    .Include(t => t.Wallet)
                                        .ThenInclude(w => w.User)
                                    .Where(t => t.TransactionTypeId == withdrawalPendingType.TransactionTypeId &&
                                                t.Status == "PendingAdminApproval" &&
                                                t.PaymentMethod == "Withdrawal"); // Hoặc tên PaymentMethod bạn dùng

        // Áp dụng Filter
        if (query.UserId.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.Wallet.UserId == query.UserId.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.UsernameOrEmail))
        {
            string searchTerm = query.UsernameOrEmail.ToLower();
            transactionsQuery = transactionsQuery.Where(t => t.Wallet.User.Username.ToLower().Contains(searchTerm) ||
                                                           t.Wallet.User.Email.ToLower().Contains(searchTerm));
        }
        if (query.MinAmount.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.Amount >= query.MinAmount.Value);
        }
        if (query.MaxAmount.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.Amount <= query.MaxAmount.Value);
        }
        if (query.DateFrom.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.TransactionDate >= query.DateFrom.Value.Date);
        }
        if (query.DateTo.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.TransactionDate < query.DateTo.Value.Date.AddDays(1));
        }

        // Áp dụng Sắp xếp
        bool isDescending = query.SortOrder?.ToLower() == "desc";
        Expression<Func<WalletTransaction, object>> orderByExpression;

        switch (query.SortBy?.ToLowerInvariant())
        {
            case "amount":
                orderByExpression = t => t.Amount;
                break;
            case "userid":
                orderByExpression = t => t.Wallet.UserId;
                break;
            case "username":
                orderByExpression = t => t.Wallet.User.Username;
                break;
            case "useremail":
                orderByExpression = t => t.Wallet.User.Email;
                break;
            case "requestedat": // Mặc định
            default:
                orderByExpression = t => t.TransactionDate;
                break;
        }
        transactionsQuery = isDescending
            ? transactionsQuery.OrderByDescending(orderByExpression)
            : transactionsQuery.OrderBy(orderByExpression);

        var paginatedTransactions = await PaginatedList<WalletTransaction>.CreateAsync(
            transactionsQuery,
            query.ValidatedPageNumber,
            query.ValidatedPageSize,
            cancellationToken);

        var dtos = paginatedTransactions.Items.Select(t => new WithdrawalRequestAdminViewDto
        {
            TransactionId = t.TransactionId,
            UserId = t.Wallet.UserId,
            Username = t.Wallet.User.Username,
            UserEmail = t.Wallet.User.Email,
            Amount = t.Amount,
            CurrencyCode = t.CurrencyCode,
            Status = t.Status,
            WithdrawalMethodDetails = t.WithdrawalMethodDetails, // Từ cột mới trong DB
            UserNotes = t.UserProvidedNotes, // Từ cột mới trong DB
            RequestedAt = t.TransactionDate,
            AdminNotes = null // Sẽ được điền khi admin xử lý
        }).ToList();

        return new PaginatedList<WithdrawalRequestAdminViewDto>(
            dtos,
            paginatedTransactions.TotalCount,
            paginatedTransactions.PageNumber,
            paginatedTransactions.PageSize);
    }
    public async Task<(bool Success, string? ErrorMessage, WalletTransactionDto? Transaction)> ReleaseHeldFundsForOrderAsync(
    int userId,
    long cancelledOrderId,
    decimal amountToRelease,
    string currencyCode,
    string reason,
    CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to release held funds for UserID {UserId}, Cancelled OrderID {CancelledOrderId}, Amount: {Amount}",
                               userId, cancelledOrderId, amountToRelease);

        if (amountToRelease <= 0)
        {
            _logger.LogInformation("Amount to release is zero or negative for OrderID {CancelledOrderId}, no funds released.", cancelledOrderId);
            return (true, "No funds needed to be released.", null); // Coi như thành công vì không có gì để làm
        }

        var userWallet = await _unitOfWork.Wallets.Query()
                                 .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
        if (userWallet == null)
        {
            _logger.LogError("ReleaseHeldFunds: Wallet not found for UserID {UserId}.", userId);
            return (false, "User wallet not found.", null);
        }

        var refundTransactionType = await _transactionTypeRepository.GetByNameAsync("OrderCancellationFundRelease", cancellationToken);
        if (refundTransactionType == null)
        {
            _logger.LogError("ReleaseHeldFunds: TransactionType 'OrderCancellationFundRelease' not found.");
            return (false, "System error: Refund transaction type configuration missing.", null);
        }
        if (!refundTransactionType.IsCredit)
        {
            _logger.LogError("ReleaseHeldFunds: TransactionType 'OrderCancellationFundRelease' is incorrectly configured (should be IsCredit=true).");
            return (false, "System error: Refund transaction type misconfigured.", null);
        }

        var now = DateTime.UtcNow;
        var transaction = new WalletTransaction
        {
            WalletId = userWallet.WalletId,
            TransactionTypeId = refundTransactionType.TransactionTypeId,
            Amount = amountToRelease,
            CurrencyCode = currencyCode.ToUpper(),
            BalanceBefore = userWallet.Balance,
            BalanceAfter = userWallet.Balance + amountToRelease, // Cộng tiền lại
            Description = $"{reason}. Ref OrderID: {cancelledOrderId}",
            ReferenceId = $"REFUND_ORD_{cancelledOrderId}",
            Status = "Completed", // Giao dịch hoàn tiền này là completed
            PaymentMethod = "SystemRefund", // Hoặc một payment method phù hợp
            TransactionDate = now,
            UpdatedAt = now
        };

        userWallet.Balance += amountToRelease;
        userWallet.UpdatedAt = now;

        await _unitOfWork.WalletTransactions.AddAsync(transaction);
        _unitOfWork.Wallets.Update(userWallet);
        // Việc lưu (CompleteAsync) sẽ được thực hiện bởi ExchangeService sau khi tất cả các bước hủy lệnh thành công

        _logger.LogInformation("Held funds released for UserID {UserId}, Cancelled OrderID {CancelledOrderId}. Amount: {Amount}. New Wallet Balance (pending save): {NewBalance}",
                               userId, cancelledOrderId, amountToRelease, userWallet.Balance);

        var dto = new WalletTransactionDto // Tạo DTO để trả về nếu cần
        {
            TransactionId = 0, // ID sẽ được gán sau khi CompleteAsync
            TransactionTypeName = refundTransactionType.TypeName,
            Amount = transaction.Amount,
            CurrencyCode = transaction.CurrencyCode,
            BalanceAfter = transaction.BalanceAfter,
            ReferenceId = transaction.ReferenceId,
            PaymentMethod = transaction.PaymentMethod,
            Description = transaction.Description,
            Status = transaction.Status,
            TransactionDate = transaction.TransactionDate,
            UpdatedAt = transaction.UpdatedAt
        };

        return (true, "Funds successfully marked for release.", dto);
    }
}