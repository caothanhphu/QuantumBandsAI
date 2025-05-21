// QuantumBands.Application/Services/TradingAccountService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Queries;
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Interfaces.Repositories; // Assuming specific repositories if needed
using QuantumBands.Domain.Entities;
using System;
using System.IdentityModel.Tokens.Jwt; // For FirstOrDefaultAsync, SumAsync
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.Application.Services;

public class TradingAccountService : ITradingAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TradingAccountService> _logger;
    // private readonly IGenericRepository<TradingAccount> _tradingAccountRepository; // Can get from UnitOfWork
    // private readonly IGenericRepository<InitialShareOffering> _offeringRepository; // Can get from UnitOfWork
    // private readonly IGenericRepository<User> _userRepository; // Can get from UnitOfWork

    public TradingAccountService(IUnitOfWork unitOfWork, ILogger<TradingAccountService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        // _tradingAccountRepository = unitOfWork.GetRepository<TradingAccount>(); // Example if UoW has generic GetRepository
        // _offeringRepository = unitOfWork.GetRepository<InitialShareOffering>();
        // _userRepository = unitOfWork.GetRepository<User>();
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

    public async Task<(TradingAccountDto? Account, string? ErrorMessage)> CreateTradingAccountAsync(CreateTradingAccountRequest request, ClaimsPrincipal adminUserPrincipal, CancellationToken cancellationToken = default)
    {
        var adminUserId = GetUserIdFromPrincipal(adminUserPrincipal);
        if (!adminUserId.HasValue) return (null, "Admin user not authenticated.");

        _logger.LogInformation("Admin {AdminUserId} attempting to create trading account: {AccountName}", adminUserId.Value, request.AccountName);

        var existingAccount = await _unitOfWork.TradingAccounts.Query() // Assuming IUnitOfWork has TradingAccounts
                                    .FirstOrDefaultAsync(ta => ta.AccountName == request.AccountName, cancellationToken);
        if (existingAccount != null)
        {
            _logger.LogWarning("Trading account with name {AccountName} already exists.", request.AccountName);
            return (null, $"A trading account with the name '{request.AccountName}' already exists.");
        }

        var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId.Value); // Assuming IUnitOfWork has Users
        if (admin == null) return (null, "Admin user record not found.");


        var tradingAccount = new TradingAccount
        {
            AccountName = request.AccountName,
            Description = request.Description,
            EaName = request.EaName,
            BrokerPlatformIdentifier = request.BrokerPlatformIdentifier,
            InitialCapital = request.InitialCapital,
            TotalSharesIssued = request.TotalSharesIssued,
            CurrentNetAssetValue = request.InitialCapital, // Initial NAV is the initial capital
            ManagementFeeRate = request.ManagementFeeRate,
            IsActive = true,
            CreatedByUserId = adminUserId.Value,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.TradingAccounts.AddAsync(tradingAccount);
        await _unitOfWork.CompleteAsync(cancellationToken);

        _logger.LogInformation("Trading account {AccountName} (ID: {TradingAccountId}) created successfully by Admin {AdminUserId}.",
                               tradingAccount.AccountName, tradingAccount.TradingAccountId, adminUserId.Value);

        var dto = new TradingAccountDto
        {
            TradingAccountId = tradingAccount.TradingAccountId,
            AccountName = tradingAccount.AccountName,
            Description = tradingAccount.Description,
            EaName = tradingAccount.EaName,
            BrokerPlatformIdentifier = tradingAccount.BrokerPlatformIdentifier,
            InitialCapital = tradingAccount.InitialCapital,
            TotalSharesIssued = tradingAccount.TotalSharesIssued,
            CurrentNetAssetValue = tradingAccount.CurrentNetAssetValue,
            CurrentSharePrice = tradingAccount.TotalSharesIssued > 0 ? tradingAccount.CurrentNetAssetValue / tradingAccount.TotalSharesIssued : 0,
            ManagementFeeRate = tradingAccount.ManagementFeeRate,
            IsActive = tradingAccount.IsActive,
            CreatedByUserId = tradingAccount.CreatedByUserId,
            CreatorUsername = admin.Username, // Assuming User entity has Username
            CreatedAt = tradingAccount.CreatedAt,
            UpdatedAt = tradingAccount.UpdatedAt
        };
        return (dto, null);
    }

    public async Task<(InitialShareOfferingDto? Offering, string? ErrorMessage)> CreateInitialShareOfferingAsync(
        int tradingAccountId, CreateInitialShareOfferingRequest request, ClaimsPrincipal adminUserPrincipal, CancellationToken cancellationToken = default)
    {
        var adminUserId = GetUserIdFromPrincipal(adminUserPrincipal);
        if (!adminUserId.HasValue) return (null, "Admin user not authenticated.");

        _logger.LogInformation("Admin {AdminUserId} attempting to create initial share offering for TradingAccountID: {TradingAccountId}", adminUserId.Value, tradingAccountId);

        var tradingAccount = await _unitOfWork.TradingAccounts.GetByIdAsync(tradingAccountId);
        if (tradingAccount == null)
        {
            _logger.LogWarning("TradingAccountID {TradingAccountId} not found for creating offering.", tradingAccountId);
            return (null, "Trading account not found.");
        }

        var admin = await _unitOfWork.Users.GetByIdAsync(adminUserId.Value);
        if (admin == null) return (null, "Admin user record not found.");

        // Kiểm tra tổng số cổ phần đã chào bán (chưa bán hết) + số cổ phần đang muốn chào bán
        long currentlyOfferedOrSoldShares = await _unitOfWork.InitialShareOfferings.Query()
                                            .Where(o => o.TradingAccountId == tradingAccountId && o.Status != nameof(OfferingStatus.Cancelled))
                                            .SumAsync(o => o.SharesOffered, cancellationToken); // Hoặc Sum(o => o.SharesSold) tùy logic
                                                                                                // Để đơn giản, ta giả định SharesOffered là số đã đưa ra thị trường
                                                                                                // Logic phức tạp hơn có thể là TotalSharesIssued - SharesAlreadyOwnedByInvestors

        if (currentlyOfferedOrSoldShares + request.SharesOffered > tradingAccount.TotalSharesIssued)
        {
            long availableToOffer = tradingAccount.TotalSharesIssued - currentlyOfferedOrSoldShares;
            _logger.LogWarning("Shares offered ({SharesOffered}) exceeds available shares ({AvailableToOffer}) for TradingAccountID {TradingAccountId}.",
                               request.SharesOffered, availableToOffer, tradingAccountId);
            return (null, $"Number of shares offered exceeds available shares. You can offer up to {availableToOffer} shares.");
        }

        var offering = new InitialShareOffering
        {
            TradingAccountId = tradingAccountId,
            AdminUserId = adminUserId.Value,
            SharesOffered = request.SharesOffered,
            SharesSold = 0, // Ban đầu
            OfferingPricePerShare = request.OfferingPricePerShare,
            FloorPricePerShare = request.FloorPricePerShare,
            CeilingPricePerShare = request.CeilingPricePerShare,
            OfferingStartDate = DateTime.UtcNow,
            OfferingEndDate = request.OfferingEndDate,
            Status = nameof(OfferingStatus.Active),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.InitialShareOfferings.AddAsync(offering);
        await _unitOfWork.CompleteAsync(cancellationToken);

        _logger.LogInformation("Initial Share Offering (ID: {OfferingId}) created successfully for TradingAccountID {TradingAccountId} by Admin {AdminUserId}.",
                               offering.OfferingId, tradingAccountId, adminUserId.Value);

        var dto = new InitialShareOfferingDto
        {
            OfferingId = offering.OfferingId,
            TradingAccountId = offering.TradingAccountId,
            AdminUserId = offering.AdminUserId,
            AdminUsername = admin.Username,
            SharesOffered = offering.SharesOffered,
            SharesSold = offering.SharesSold,
            OfferingPricePerShare = offering.OfferingPricePerShare,
            FloorPricePerShare = offering.FloorPricePerShare,
            CeilingPricePerShare = offering.CeilingPricePerShare,
            OfferingStartDate = offering.OfferingStartDate,
            OfferingEndDate = offering.OfferingEndDate,
            Status = offering.Status,
            CreatedAt = offering.CreatedAt,
            UpdatedAt = offering.UpdatedAt
        };
        return (dto, null);
    }
    public async Task<PaginatedList<TradingAccountDto>> GetPublicTradingAccountsAsync(GetPublicTradingAccountsQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching public trading accounts with query: {@Query}", query);

        var accountsQuery = _unitOfWork.TradingAccounts.Query()
                                .Include(ta => ta.CreatedByUser)
                                .AsQueryable(); // Ensure the query is treated as IQueryable

        // Apply Filter
        if (query.IsActive.HasValue)
        {
            accountsQuery = accountsQuery.Where(ta => ta.IsActive == query.IsActive.Value);
        }
        else
        {
            // Default to only active accounts if no filter is provided
            accountsQuery = accountsQuery.Where(ta => ta.IsActive == true);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string searchTermLower = query.SearchTerm.ToLower();
            accountsQuery = accountsQuery.Where(ta =>
                (ta.AccountName != null && ta.AccountName.ToLower().Contains(searchTermLower)) ||
                (ta.Description != null && ta.Description.ToLower().Contains(searchTermLower))
            );
        }

        // Apply Sorting
        bool isDescending = query.SortOrder?.ToLower() == "desc";
        Expression<Func<TradingAccount, object>> orderByExpression;

        switch (query.SortBy?.ToLowerInvariant())
        {
            case "currentshareprice":
                orderByExpression = ta => (ta.TotalSharesIssued > 0 ? ta.CurrentNetAssetValue / ta.TotalSharesIssued : 0);
                break;
            case "managementfeerate":
                orderByExpression = ta => ta.ManagementFeeRate;
                break;
            case "createdat":
                orderByExpression = ta => ta.CreatedAt;
                break;
            case "accountname":
            default:
                orderByExpression = ta => ta.AccountName;
                break;
        }

        accountsQuery = isDescending
            ? accountsQuery.OrderByDescending(orderByExpression)
            : accountsQuery.OrderBy(orderByExpression);

        var paginatedAccounts = await PaginatedList<TradingAccount>.CreateAsync(
            accountsQuery,
            query.ValidatedPageNumber,
            query.ValidatedPageSize,
            cancellationToken);

        var dtos = paginatedAccounts.Items.Select(ta => new TradingAccountDto
        {
            TradingAccountId = ta.TradingAccountId,
            AccountName = ta.AccountName,
            Description = ta.Description,
            EaName = ta.EaName,
            BrokerPlatformIdentifier = ta.BrokerPlatformIdentifier,
            InitialCapital = ta.InitialCapital,
            TotalSharesIssued = ta.TotalSharesIssued,
            CurrentNetAssetValue = ta.CurrentNetAssetValue,
            CurrentSharePrice = ta.TotalSharesIssued > 0 ? ta.CurrentNetAssetValue / ta.TotalSharesIssued : 0,
            ManagementFeeRate = ta.ManagementFeeRate,
            IsActive = ta.IsActive,
            CreatedByUserId = ta.CreatedByUserId,
            CreatorUsername = ta.CreatedByUser?.Username ?? "N/A",
            CreatedAt = ta.CreatedAt,
            UpdatedAt = ta.UpdatedAt
        }).ToList();

        return new PaginatedList<TradingAccountDto>(
            dtos,
            paginatedAccounts.TotalCount,
            paginatedAccounts.PageNumber,
            paginatedAccounts.PageSize);
    }

    public async Task<TradingAccountDto?> GetTradingAccountByIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching trading account by ID: {AccountId}", accountId);
        var account = await _unitOfWork.TradingAccounts.Query()
                            .Include(ta => ta.CreatedByUser)
                            .FirstOrDefaultAsync(ta => ta.TradingAccountId == accountId, cancellationToken);

        if (account == null)
        {
            return null;
        }

        return new TradingAccountDto
        {
            TradingAccountId = account.TradingAccountId,
            AccountName = account.AccountName,
            Description = account.Description,
            EaName = account.EaName,
            BrokerPlatformIdentifier = account.BrokerPlatformIdentifier,
            InitialCapital = account.InitialCapital,
            TotalSharesIssued = account.TotalSharesIssued,
            CurrentNetAssetValue = account.CurrentNetAssetValue,
            CurrentSharePrice = account.TotalSharesIssued > 0 ? account.CurrentNetAssetValue / account.TotalSharesIssued : 0,
            ManagementFeeRate = account.ManagementFeeRate,
            IsActive = account.IsActive,
            CreatedByUserId = account.CreatedByUserId,
            CreatorUsername = account.CreatedByUser?.Username ?? "N/A",
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }
}