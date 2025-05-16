// QuantumBands.Application/Services/WalletService.cs
using QuantumBands.Application.Features.Wallets.Dtos;
using QuantumBands.Application.Interfaces;
using QuantumBands.Domain.Entities; // For Wallet entity
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration; // For FirstOrDefaultAsync
using System.Linq.Expressions;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Wallets.Queries.GetTransactions; // For Expression
using QuantumBands.Application.Features.Wallets.Commands.AdminDeposit;
using QuantumBands.Application.Features.Wallets.Commands.BankDeposit;
using System.Globalization;
using QuantumBands.Application.Interfaces.Repositories;

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
}