// QuantumBands.Application/Services/TradingAccountService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Queries;
using QuantumBands.Application.Features.TradingAccounts.Enums;
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Interfaces.Repositories; // Assuming specific repositories if needed
using QuantumBands.Application.Services;
using QuantumBands.Domain.Entities;
using QuantumBands.Domain.Entities.Enums;
using System;
using System.IdentityModel.Tokens.Jwt; // For FirstOrDefaultAsync, SumAsync
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.Application.Services;

public class TradingAccountService : ITradingAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TradingAccountService> _logger;
    private readonly IClosedTradeService _closedTradeService;
    private readonly IWalletService _walletService;
    private readonly ChartDataService _chartDataService;
    // private readonly IGenericRepository<TradingAccount> _tradingAccountRepository; // Can get from UnitOfWork
    // private readonly IGenericRepository<InitialShareOffering> _offeringRepository; // Can get from UnitOfWork
    // private readonly IGenericRepository<User> _userRepository; // Can get from UnitOfWork

    public TradingAccountService(
        IUnitOfWork unitOfWork, 
        ILogger<TradingAccountService> logger,
        IClosedTradeService closedTradeService,
        IWalletService walletService,
        ChartDataService chartDataService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _closedTradeService = closedTradeService;
        _walletService = walletService;
        _chartDataService = chartDataService;
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
            Eaname = request.EaName,
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
            EaName = tradingAccount.Eaname,
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
            EaName = ta.Eaname,
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

    public async Task<(TradingAccountDetailDto? Detail, string? ErrorMessage)> GetTradingAccountDetailsAsync(
        int accountId,
        GetTradingAccountDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching details for TradingAccountID: {AccountId} with query {@Query}", accountId, query);

        var account = await _unitOfWork.TradingAccounts.Query()
                            .Include(ta => ta.CreatedByUser) // Để lấy CreatorUsername
                            .AsNoTracking() // Không cần theo dõi cho truy vấn đọc chi tiết
                            .FirstOrDefaultAsync(ta => ta.TradingAccountId == accountId, cancellationToken);

        if (account == null)
        {
            _logger.LogWarning("TradingAccountID {AccountId} not found.", accountId);
            return (null, $"Trading account with ID {accountId} not found.");
        }

        // 1. Lấy Open Positions (giới hạn)
        var openPositions = await _unitOfWork.EAOpenPositions.Query()
            .Where(op => op.TradingAccountId == accountId)
            .OrderByDescending(op => op.OpenTime) // Ví dụ: sắp xếp theo thời gian mở mới nhất
            .Take(query.ValidatedOpenPositionsLimit)
            .Select(op => new EAOpenPositionDto
            {
                OpenPositionId = op.OpenPositionId,
                EaTicketId = op.EaticketId,
                Symbol = op.Symbol,
                TradeType = op.TradeType,
                VolumeLots = op.VolumeLots,
                OpenPrice = op.OpenPrice,
                OpenTime = op.OpenTime,
                CurrentMarketPrice = op.CurrentMarketPrice,
                // Fix for CS0266 and CS8629: Ensure nullable decimal is handled safely with a null-coalescing operator or explicit cast.
                Swap = op.Swap ?? 0m,
                Commission = op.Commission ?? 0m,
                FloatingPAndL = op.FloatingPandL,
                LastUpdateTime = op.LastUpdateTime
            })
            .ToListAsync(cancellationToken);

        // 2. Lấy Closed Trades History (phân trang)
        var closedTradesQuery = _unitOfWork.EAClosedTrades.Query()
            .Where(ct => ct.TradingAccountId == accountId)
            .OrderByDescending(ct => ct.CloseTime); // Sắp xếp theo thời gian đóng mới nhất

        var paginatedClosedTrades = await PaginatedList<EaclosedTrade>.CreateAsync(
            closedTradesQuery,
            query.ValidatedClosedTradesPageNumber,
            query.ValidatedClosedTradesPageSize,
            cancellationToken);

        var closedTradesDtos = paginatedClosedTrades.Items.Select(ct => new EAClosedTradeDto
        {
            ClosedTradeId = ct.ClosedTradeId,
            EaTicketId = ct.EaticketId,
            Symbol = ct.Symbol,
            TradeType = ct.TradeType,
            VolumeLots = ct.VolumeLots,
            OpenPrice = ct.OpenPrice,
            OpenTime = ct.OpenTime,
            ClosePrice = ct.ClosePrice,
            CloseTime = ct.CloseTime,
            Swap = ct.Swap ?? 0m,
            Commission = ct.Commission ?? 0m,
            RealizedPAndL = ct.RealizedPandL,
            RecordedAt = ct.RecordedAt
        }).ToList();
        var closedTradesHistoryPaginatedDto = new PaginatedList<EAClosedTradeDto>(
            closedTradesDtos, paginatedClosedTrades.TotalCount, paginatedClosedTrades.PageNumber, paginatedClosedTrades.PageSize);


        // 3. Lấy Daily Snapshots Info (phân trang)
        var snapshotsQuery = _unitOfWork.TradingAccountSnapshots.Query()
            .Where(s => s.TradingAccountId == accountId)
            .OrderByDescending(s => s.SnapshotDate); // Sắp xếp theo ngày snapshot mới nhất

        var paginatedSnapshots = await PaginatedList<TradingAccountSnapshot>.CreateAsync(
            snapshotsQuery,
            query.ValidatedSnapshotsPageNumber,
            query.ValidatedSnapshotsPageSize,
            cancellationToken);

        var snapshotDtos = paginatedSnapshots.Items.Select(s => new TradingAccountSnapshotDto
        {
            SnapshotId = s.SnapshotId,
            SnapshotDate = s.SnapshotDate,
            OpeningNAV = s.OpeningNav,
            RealizedPAndLForTheDay = s.RealizedPandLforTheDay,
            UnrealizedPAndLForTheDay = s.UnrealizedPandLforTheDay,
            ManagementFeeDeducted = s.ManagementFeeDeducted,
            ProfitDistributed = s.ProfitDistributed,
            ClosingNAV = s.ClosingNav,
            ClosingSharePrice = s.ClosingSharePrice,
            CreatedAt = s.CreatedAt
        }).ToList();
        var dailySnapshotsInfoPaginatedDto = new PaginatedList<TradingAccountSnapshotDto>(
            snapshotDtos, paginatedSnapshots.TotalCount, paginatedSnapshots.PageNumber, paginatedSnapshots.PageSize);


        // Tạo DTO chi tiết
        var detailDto = new TradingAccountDetailDto
        {
            TradingAccountId = account.TradingAccountId,
            AccountName = account.AccountName,
            Description = account.Description,
            EaName = account.Eaname,
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
            UpdatedAt = account.UpdatedAt,
            OpenPositions = openPositions,
            ClosedTradesHistory = closedTradesHistoryPaginatedDto,
            DailySnapshotsInfo = dailySnapshotsInfoPaginatedDto
        };

        return (detailDto, null);
    }
    public async Task<(TradingAccountDto? Account, string? ErrorMessage)> UpdateTradingAccountAsync(
    int accountId,
    UpdateTradingAccountRequest request,
    ClaimsPrincipal adminUserPrincipal,
    CancellationToken cancellationToken = default)
    {
        var adminUserId = GetUserIdFromPrincipal(adminUserPrincipal);
        if (!adminUserId.HasValue) return (null, "Admin user not authenticated.");

        _logger.LogInformation("Admin {AdminUserId} attempting to update TradingAccountID: {TradingAccountId}", adminUserId.Value, accountId);

        var tradingAccount = await _unitOfWork.TradingAccounts.Query()
                                    .Include(ta => ta.CreatedByUser) // Để lấy CreatorUsername cho DTO response
                                    .FirstOrDefaultAsync(ta => ta.TradingAccountId == accountId, cancellationToken);

        if (tradingAccount == null)
        {
            _logger.LogWarning("UpdateTradingAccountAsync: TradingAccountID {TradingAccountId} not found.", accountId);
            return (null, $"Trading account with ID {accountId} not found.");
        }        // Kiểm tra xem admin hiện tại có phải là người tạo quỹ không, hoặc có quyền admin cao hơn không
        // (Tùy theo yêu cầu nghiệp vụ, ở đây giả sử Admin nào cũng có thể sửa)

        bool hasChanges = false;

        // Check for AccountName change with duplicate validation
        if (request.AccountName != null && tradingAccount.AccountName != request.AccountName)
        {
            // Check if new account name is already taken
            var existingAccount = await _unitOfWork.TradingAccounts.Query()
                .FirstOrDefaultAsync(ta => ta.AccountName == request.AccountName && ta.TradingAccountId != accountId, cancellationToken);
            
            if (existingAccount != null)
            {
                _logger.LogWarning("Cannot update TradingAccountID {TradingAccountId} - Account name '{AccountName}' is already taken.", accountId, request.AccountName);
                return (null, $"Account name '{request.AccountName}' is already in use.");
            }
            
            tradingAccount.AccountName = request.AccountName;
            hasChanges = true;
        }

        if (request.Description != null && tradingAccount.Description != request.Description)
        {
            tradingAccount.Description = request.Description;
            hasChanges = true;
        }
        if (request.EaName != null && tradingAccount.Eaname != request.EaName)
        {
            tradingAccount.Eaname = request.EaName;
            hasChanges = true;
        }
        if (request.ManagementFeeRate.HasValue && tradingAccount.ManagementFeeRate != request.ManagementFeeRate.Value)
        {
            tradingAccount.ManagementFeeRate = request.ManagementFeeRate.Value;
            hasChanges = true;
        }
        if (request.IsActive.HasValue && tradingAccount.IsActive != request.IsActive.Value)
        {
            tradingAccount.IsActive = request.IsActive.Value;
            hasChanges = true;
        }

        // Acceptance Criteria: Không cho phép thay đổi TotalSharesIssued sau khi đã có giao dịch cổ phần.
        // Hiện tại DTO không cho phép thay đổi TotalSharesIssued và InitialCapital, nên điều kiện này được đáp ứng.
        // Nếu sau này cho phép, cần thêm logic kiểm tra:
        // bool hasTransactions = await _unitOfWork.InitialShareOfferings.Query()
        //                            .AnyAsync(iso => iso.TradingAccountId == accountId && iso.SharesSold > 0, cancellationToken);
        // if (hasTransactions && (request.TotalSharesIssued.HasValue && tradingAccount.TotalSharesIssued != request.TotalSharesIssued.Value))
        // {
        //     return (null, "Cannot change TotalSharesIssued after shares have been offered or sold.");
        // }

        if (!hasChanges)
        {
            _logger.LogInformation("No changes detected for TradingAccountID {TradingAccountId}.", accountId);
            // Vẫn trả về thông tin hiện tại nếu không có gì thay đổi
        }
        else
        {
            tradingAccount.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.TradingAccounts.Update(tradingAccount); // EF Core chỉ update các trường đã thay đổi
            try
            {
                await _unitOfWork.CompleteAsync(cancellationToken);
                _logger.LogInformation("TradingAccountID {TradingAccountId} updated successfully by Admin {AdminUserId}.", accountId, adminUserId.Value);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while updating TradingAccountID: {TradingAccountId}", accountId);
                return (null, "Could not update trading account due to a concurrency conflict. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating TradingAccountID: {TradingAccountId}", accountId);
                return (null, "An error occurred while updating the trading account.");
            }
        }

        var dto = new TradingAccountDto
        {
            TradingAccountId = tradingAccount.TradingAccountId,
            AccountName = tradingAccount.AccountName, // Now supports AccountName updates
            Description = tradingAccount.Description,
            EaName = tradingAccount.Eaname,
            BrokerPlatformIdentifier = tradingAccount.BrokerPlatformIdentifier,
            InitialCapital = tradingAccount.InitialCapital,
            TotalSharesIssued = tradingAccount.TotalSharesIssued,
            CurrentNetAssetValue = tradingAccount.CurrentNetAssetValue,
            CurrentSharePrice = tradingAccount.TotalSharesIssued > 0 ? tradingAccount.CurrentNetAssetValue / tradingAccount.TotalSharesIssued : 0,
            ManagementFeeRate = tradingAccount.ManagementFeeRate,
            IsActive = tradingAccount.IsActive,
            CreatedByUserId = tradingAccount.CreatedByUserId,
            CreatorUsername = tradingAccount.CreatedByUser?.Username ?? "N/A",
            CreatedAt = tradingAccount.CreatedAt,
            UpdatedAt = tradingAccount.UpdatedAt
        };
        return (dto, null);
    }
    public async Task<(PaginatedList<InitialShareOfferingDto>? Offerings, string? ErrorMessage)> GetInitialShareOfferingsAsync(
    int tradingAccountId,
    GetInitialOfferingsQuery query,
    CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching initial share offerings for TradingAccountID: {TradingAccountId} with query: {@Query}", tradingAccountId, query);

        var tradingAccountExists = await _unitOfWork.TradingAccounts.Query()
                                        .AnyAsync(ta => ta.TradingAccountId == tradingAccountId, cancellationToken);
        if (!tradingAccountExists)
        {
            _logger.LogWarning("TradingAccountID {TradingAccountId} not found when fetching offerings.", tradingAccountId);
            return (null, $"Trading account with ID {tradingAccountId} not found.");
        }

        var offeringsQuery = _unitOfWork.InitialShareOfferings.Query()
                                .Include(iso => iso.AdminUser) // Để lấy AdminUsername
                                .Where(iso => iso.TradingAccountId == tradingAccountId);

        // Áp dụng Filter theo Status
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            string statusFilterLower = query.Status.ToLowerInvariant();
            // Giả sử cột Status trong DB lưu tên của Enum (ví dụ: "Active", "Completed")
            // Và query.Status cũng là một trong các tên đó (đã được validator kiểm tra)
            offeringsQuery = offeringsQuery.Where(iso => iso.Status.ToLower() == statusFilterLower);
        }

        // Áp dụng Sắp xếp
        bool isDescending = query.SortOrder?.ToLower() == "desc";
        Expression<Func<InitialShareOffering, object>> orderByExpression;

        switch (query.SortBy?.ToLowerInvariant())
        {
            case "offeringpricepershare":
                orderByExpression = iso => iso.OfferingPricePerShare;
                break;
            case "sharesoffered":
                orderByExpression = iso => iso.SharesOffered;
                break;
            case "offeringstartdate":
            default:
                orderByExpression = iso => iso.OfferingStartDate;
                break;
        }

        offeringsQuery = isDescending
            ? offeringsQuery.OrderByDescending(orderByExpression)
            : offeringsQuery.OrderBy(orderByExpression);

        var paginatedOfferings = await PaginatedList<InitialShareOffering>.CreateAsync(
            offeringsQuery,
            query.ValidatedPageNumber,
            query.ValidatedPageSize,
            cancellationToken);

        var dtos = paginatedOfferings.Items.Select(iso => new InitialShareOfferingDto
        {
            OfferingId = iso.OfferingId,
            TradingAccountId = iso.TradingAccountId,
            AdminUserId = iso.AdminUserId,
            AdminUsername = iso.AdminUser?.Username ?? "N/A", // Lấy username từ navigation property
            SharesOffered = iso.SharesOffered,
            SharesSold = iso.SharesSold,
            OfferingPricePerShare = iso.OfferingPricePerShare,
            FloorPricePerShare = iso.FloorPricePerShare,
            CeilingPricePerShare = iso.CeilingPricePerShare,
            OfferingStartDate = iso.OfferingStartDate,
            OfferingEndDate = iso.OfferingEndDate,
            Status = iso.Status,
            CreatedAt = iso.CreatedAt,
            UpdatedAt = iso.UpdatedAt
        }).ToList();

        return (new PaginatedList<InitialShareOfferingDto>(
            dtos,
            paginatedOfferings.TotalCount,
            paginatedOfferings.PageNumber,
            paginatedOfferings.PageSize), null);
    }
    public async Task<(InitialShareOfferingDto? Offering, string? ErrorMessage)> UpdateInitialShareOfferingAsync(
    int tradingAccountId,
    int offeringId,
    UpdateInitialShareOfferingRequest request,
    ClaimsPrincipal adminUserPrincipal,
    CancellationToken cancellationToken = default)
    {
        var adminUserId = GetUserIdFromPrincipal(adminUserPrincipal);
        if (!adminUserId.HasValue) return (null, "Admin user not authenticated.");

        // Validate request inputs first
        if (request.SharesOffered.HasValue && request.SharesOffered.Value <= 0)
        {
            return (null, "Shares offered must be greater than 0");
        }
        if (request.OfferingPricePerShare.HasValue && request.OfferingPricePerShare.Value <= 0)
        {
            return (null, "Offering price per share must be greater than 0");
        }
        if (request.FloorPricePerShare.HasValue && request.CeilingPricePerShare.HasValue && 
            request.FloorPricePerShare.Value > request.CeilingPricePerShare.Value)
        {
            return (null, "Ceiling price must be greater than floor price");
        }

        _logger.LogInformation("Admin {AdminUserId} attempting to update InitialShareOfferingID: {OfferingId} for TradingAccountID: {TradingAccountId}",
                               adminUserId.Value, offeringId, tradingAccountId);

        var offering = await _unitOfWork.InitialShareOfferings.Query()
                            .Include(o => o.AdminUser) // Để lấy AdminUsername cho DTO response
                            .Include(o => o.TradingAccount) // Để kiểm tra tradingAccountId
                            .FirstOrDefaultAsync(o => o.OfferingId == offeringId && o.TradingAccountId == tradingAccountId, cancellationToken);

        if (offering == null)
        {
            _logger.LogWarning("InitialShareOfferingID {OfferingId} for TradingAccountID {TradingAccountId} not found.", offeringId, tradingAccountId);
            return (null, $"Initial share offering with ID {offeringId} not found for trading account {tradingAccountId}.");
        }

        // Check if offering can be updated based on current status
        if (offering.Status == nameof(OfferingStatus.Completed))
        {
            return (null, "Cannot change offering status from Completed");
        }
        if (offering.Status == nameof(OfferingStatus.Cancelled))
        {
            return (null, "Cannot change offering status from Cancelled");
        }

        // Kiểm tra các điều kiện nghiệp vụ trước khi cập nhật
        if (offering.SharesSold > 0)
        {
            if (request.SharesOffered.HasValue && request.SharesOffered.Value != offering.SharesOffered)
            {
                return (null, "Cannot change 'SharesOffered' after sales have started.");
            }
            if (request.OfferingPricePerShare.HasValue && request.OfferingPricePerShare.Value != offering.OfferingPricePerShare)
            {
                return (null, "Cannot change 'OfferingPricePerShare' after sales have started.");
            }
            // Tương tự, có thể hạn chế thay đổi Floor/Ceiling price nếu đã có người mua
        }

        bool hasChanges = false;

        if (request.SharesOffered.HasValue && offering.SharesOffered != request.SharesOffered.Value)
        {
            if (request.SharesOffered.Value < offering.SharesSold)
            {
                return (null, $"New 'SharesOffered' ({request.SharesOffered.Value}) cannot be less than current 'SharesSold' ({offering.SharesSold}).");
            }
            offering.SharesOffered = request.SharesOffered.Value;
            hasChanges = true;
        }
        if (request.OfferingPricePerShare.HasValue && offering.OfferingPricePerShare != request.OfferingPricePerShare.Value)
        {
            offering.OfferingPricePerShare = request.OfferingPricePerShare.Value;
            hasChanges = true;
        }
        if (request.FloorPricePerShare.HasValue && offering.FloorPricePerShare != request.FloorPricePerShare.Value)
        {
            offering.FloorPricePerShare = request.FloorPricePerShare.Value;
            hasChanges = true;
        }
        else if (request.FloorPricePerShare == null && offering.FloorPricePerShare != null) // Cho phép xóa floor price
        {
            offering.FloorPricePerShare = null;
            hasChanges = true;
        }

        if (request.CeilingPricePerShare.HasValue && offering.CeilingPricePerShare != request.CeilingPricePerShare.Value)
        {
            offering.CeilingPricePerShare = request.CeilingPricePerShare.Value;
            hasChanges = true;
        }
        else if (request.CeilingPricePerShare == null && offering.CeilingPricePerShare != null) // Cho phép xóa ceiling price
        {
            offering.CeilingPricePerShare = null;
            hasChanges = true;
        }

        if (request.OfferingEndDate.HasValue && offering.OfferingEndDate != request.OfferingEndDate.Value)
        {
            if (request.OfferingEndDate.Value <= DateTime.UtcNow)
            {
                return (null, "Offering end date must be in the future.");
            }
            offering.OfferingEndDate = request.OfferingEndDate.Value;
            hasChanges = true;
        }
        else if (request.OfferingEndDate == null && offering.OfferingEndDate != null) // Cho phép xóa end date (làm cho nó không bao giờ hết hạn tự động)
        {
            offering.OfferingEndDate = null;
            hasChanges = true;
        }


        if (!string.IsNullOrEmpty(request.Status) && !offering.Status.Equals(request.Status, StringComparison.OrdinalIgnoreCase))
        {
            // Kiểm tra xem status mới có hợp lệ không (ví dụ: không thể chuyển từ "Completed" về "Active")
            if (offering.Status == nameof(OfferingStatus.Completed) && request.Status.Equals(nameof(OfferingStatus.Active), StringComparison.OrdinalIgnoreCase))
            {
                return (null, "Cannot change status from 'Completed' back to 'Active'.");
            }
            // Thêm các rule khác nếu cần
            offering.Status = System.Enum.Parse<OfferingStatus>(request.Status, true).ToString(); // Chuẩn hóa tên status
            hasChanges = true;
        }

        if (!hasChanges)
        {
            _logger.LogInformation("No changes detected for InitialShareOfferingID {OfferingId}.", offeringId);
        }
        else
        {
            offering.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.InitialShareOfferings.Update(offering);
            try
            {
                await _unitOfWork.CompleteAsync(cancellationToken);
                _logger.LogInformation("InitialShareOfferingID {OfferingId} updated successfully by Admin {AdminUserId}.", offeringId, adminUserId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating InitialShareOfferingID {OfferingId}", offeringId);
                return (null, "An error occurred while updating the offering.");
            }
        }

        var dto = new InitialShareOfferingDto
        {
            OfferingId = offering.OfferingId,
            TradingAccountId = offering.TradingAccountId,
            AdminUserId = offering.AdminUserId,
            AdminUsername = offering.AdminUser?.Username ?? "N/A",
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

    public async Task<(InitialShareOfferingDto? Offering, string? ErrorMessage)> CancelInitialShareOfferingAsync(
    int tradingAccountId,
    int offeringId,
    CancelInitialShareOfferingRequest request, // Request có thể chứa AdminNotes
    ClaimsPrincipal adminUserPrincipal,
    CancellationToken cancellationToken = default)
    {
        var adminUserId = GetUserIdFromPrincipal(adminUserPrincipal);
        if (!adminUserId.HasValue) return (null, "Admin user not authenticated.");

        _logger.LogInformation("Admin {AdminUserId} attempting to cancel InitialShareOfferingID: {OfferingId} for TradingAccountID: {TradingAccountId}",
                               adminUserId.Value, offeringId, tradingAccountId);

        var offering = await _unitOfWork.InitialShareOfferings.Query()
                            .Include(o => o.AdminUser) // Để lấy AdminUsername cho DTO response
                            .Include(o => o.TradingAccount)
                            .FirstOrDefaultAsync(o => o.OfferingId == offeringId && o.TradingAccountId == tradingAccountId, cancellationToken);

        if (offering == null)
        {
            _logger.LogWarning("InitialShareOfferingID {OfferingId} for TradingAccountID {TradingAccountId} not found for cancellation.", offeringId, tradingAccountId);
            return (null, $"Initial share offering with ID {offeringId} not found for trading account {tradingAccountId}.");
        }

        if (!offering.Status.Equals(nameof(OfferingStatus.Active), StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("InitialShareOfferingID {OfferingId} is not in 'Active' state (current: {CurrentStatus}). Cannot cancel.", offeringId, offering.Status);
            return (null, $"Only 'Active' offerings can be cancelled. Current status is '{offering.Status}'.");
        }

        offering.Status = nameof(OfferingStatus.Cancelled);
        offering.UpdatedAt = DateTime.UtcNow;
        // Ghi chú của Admin có thể được lưu vào một trường riêng của Offering hoặc vào một bảng log khác.
        // Để đơn giản, có thể append vào Description của Offering nếu có.
        //if (!string.IsNullOrEmpty(request.AdminNotes))
        //{
        //    offering.Description = $"{offering.Description?.TrimEnd()} | Cancelled by Admin {adminUserId.Value}. Notes: {request.AdminNotes}";
        //}


        _unitOfWork.InitialShareOfferings.Update(offering);
        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("InitialShareOfferingID {OfferingId} cancelled successfully by Admin {AdminUserId}.", offeringId, adminUserId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling InitialShareOfferingID {OfferingId}", offeringId);
            return (null, "An error occurred while cancelling the offering.");
        }

        var dto = new InitialShareOfferingDto
        {
            OfferingId = offering.OfferingId,
            TradingAccountId = offering.TradingAccountId,
            AdminUserId = offering.AdminUserId,
            AdminUsername = offering.AdminUser?.Username ?? "N/A",
            SharesOffered = offering.SharesOffered,
            SharesSold = offering.SharesSold,
            OfferingPricePerShare = offering.OfferingPricePerShare,
            FloorPricePerShare = offering.FloorPricePerShare,
            CeilingPricePerShare = offering.CeilingPricePerShare,
            OfferingStartDate = offering.OfferingStartDate,
            OfferingEndDate = offering.OfferingEndDate,
            Status = offering.Status, // Trạng thái mới là "Cancelled"
            CreatedAt = offering.CreatedAt,
            UpdatedAt = offering.UpdatedAt
        };
        return (dto, null);
    }

    public async Task<(AccountOverviewDto? Overview, string? ErrorMessage)> GetAccountOverviewAsync(int accountId, int userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting account overview for account {AccountId}, user {UserId}, isAdmin: {IsAdmin}", accountId, userId, isAdmin);

            // Authorization check
            if (!isAdmin)
            {
                // Check if user owns this account
                var accountOwner = await _unitOfWork.TradingAccounts.Query()
                    .Where(ta => ta.TradingAccountId == accountId)
                    .Select(ta => ta.CreatedByUserId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (accountOwner != userId)
                {
                    return (null, "Unauthorized access to this trading account");
                }
            }

            // Get account info
            var account = await _unitOfWork.TradingAccounts.Query()
                .FirstOrDefaultAsync(ta => ta.TradingAccountId == accountId, cancellationToken);

            if (account == null)
            {
                return (null, $"Trading account with ID {accountId} not found");
            }

            // Get performance KPIs from ClosedTradeService
            var (totalTrades, winRate, profitFactor, totalProfit) = await _closedTradeService.GetPerformanceKPIsAsync(accountId, cancellationToken);

            // Get financial summary from WalletService
            var (totalDeposits, totalWithdrawals, initialDeposit) = await _walletService.GetFinancialSummaryAsync(accountId, cancellationToken);

            // Calculate current balance and equity from open positions
            var openPositions = await _unitOfWork.EAOpenPositions.Query()
                .Where(op => op.TradingAccountId == accountId)
                .ToListAsync(cancellationToken);

            var currentBalance = account.CurrentNetAssetValue;
            var floatingPnL = openPositions.Sum(op => op.FloatingPandL);
            var currentEquity = currentBalance + floatingPnL;

            // Calculate margin info (simplified - in real implementation this would be more complex)
            var marginUsed = openPositions.Sum(op => op.VolumeLots * op.OpenPrice * 100); // Simplified calculation
            var freeMargin = currentEquity - marginUsed;
            var marginLevel = marginUsed > 0 ? (currentEquity / marginUsed) * 100 : 0m;

            // Calculate additional KPIs
            var activeDays = (DateTime.UtcNow - account.CreatedAt).Days;
            var growthPercent = initialDeposit > 0 ? ((currentBalance - initialDeposit) / initialDeposit) * 100 : 0m;

            // Calculate max drawdown (simplified - in real implementation this would use daily snapshots)
            var maxDrawdown = 0m; // This would need historical data analysis
            var maxDrawdownAmount = 0m;

            var overview = new AccountOverviewDto
            {
                AccountInfo = new AccountInfoDto
                {
                    AccountId = account.TradingAccountId.ToString(),
                    AccountName = account.AccountName,
                    Login = account.BrokerPlatformIdentifier ?? "",
                    Server = "MT5-Server", // This could be from configuration
                    AccountType = "Real", // This could be from account settings
                    TradingPlatform = "MT5",
                    HedgingAllowed = true, // This could be from account settings
                    Leverage = 100, // This could be from account settings
                    RegistrationDate = account.CreatedAt,
                    LastActivity = account.UpdatedAt,
                    Status = account.IsActive ? "Active" : "Inactive"
                },
                BalanceInfo = new BalanceInfoDto
                {
                    CurrentBalance = currentBalance,
                    CurrentEquity = currentEquity,
                    FreeMargin = freeMargin,
                    MarginLevel = marginLevel,
                    TotalDeposits = totalDeposits,
                    TotalWithdrawals = totalWithdrawals,
                    TotalProfit = totalProfit,
                    InitialDeposit = initialDeposit
                },
                PerformanceKPIs = new PerformanceKPIsDto
                {
                    TotalTrades = totalTrades,
                    WinRate = winRate,
                    ProfitFactor = profitFactor,
                    MaxDrawdown = maxDrawdown,
                    MaxDrawdownAmount = maxDrawdownAmount,
                    GrowthPercent = growthPercent,
                    ActiveDays = activeDays
                }
            };

            _logger.LogInformation("Account overview generated successfully for account {AccountId}", accountId);
            return (overview, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account overview for account {AccountId}", accountId);
            return (null, "An error occurred while retrieving account overview");
        }
    }

    /// <summary>
    /// Gets chart data for trading account performance visualization
    /// </summary>
    public async Task<(ChartDataDto? ChartData, string? ErrorMessage)> GetChartDataAsync(
        int accountId, 
        GetChartDataQuery query, 
        int userId, 
        bool isAdmin, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting chart data for account {AccountId}, user {UserId}, type {ChartType}, period {Period}, interval {Interval}", 
                accountId, userId, query.Type, query.Period, query.Interval);

            // Authorization check - same as account overview
            if (!isAdmin)
            {
                var accountOwner = await _unitOfWork.TradingAccounts.Query()
                    .Where(ta => ta.TradingAccountId == accountId)
                    .Select(ta => ta.CreatedByUserId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (accountOwner != userId)
                {
                    return (null, "Unauthorized access to this trading account");
                }
            }

            // Check if account exists
            var accountExists = await _unitOfWork.TradingAccounts.Query()
                .AnyAsync(ta => ta.TradingAccountId == accountId, cancellationToken);

            if (!accountExists)
            {
                return (null, $"Trading account with ID {accountId} not found");
            }

            // Generate chart data
            var chartData = await _chartDataService.GenerateChartDataAsync(accountId, query, cancellationToken);

            _logger.LogInformation("Chart data generated successfully for account {AccountId}, {DataPointsCount} data points", 
                accountId, chartData.DataPoints.Count);
            
            return (chartData, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chart data for account {AccountId}, type {ChartType}", 
                accountId, query.Type);
            return (null, "An error occurred while retrieving chart data");
        }
    }

    public async Task<(PaginatedTradingHistoryDto? History, string? ErrorMessage)> GetTradingHistoryAsync(
        int accountId, 
        GetTradingHistoryQuery query, 
        int userId, 
        bool isAdmin, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting trading history for account {AccountId}, user {UserId}, page {Page}, pageSize {PageSize}", 
                accountId, userId, query.ValidatedPage, query.ValidatedPageSize);

            // Authorization check - same as other methods
            if (!isAdmin)
            {
                var accountOwner = await _unitOfWork.TradingAccounts.Query()
                    .Where(ta => ta.TradingAccountId == accountId)
                    .Select(ta => ta.CreatedByUserId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (accountOwner != userId)
                {
                    return (null, "Unauthorized access to this trading account");
                }
            }

            // Check if account exists
            var accountExists = await _unitOfWork.TradingAccounts.Query()
                .AnyAsync(ta => ta.TradingAccountId == accountId, cancellationToken);

            if (!accountExists)
            {
                return (null, $"Trading account with ID {accountId} not found");
            }

            // Build base query
            var tradesQuery = _unitOfWork.EAClosedTrades.Query()
                .Where(ct => ct.TradingAccountId == accountId);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(query.Symbol))
            {
                tradesQuery = tradesQuery.Where(ct => ct.Symbol.Contains(query.Symbol));
            }

            if (!string.IsNullOrWhiteSpace(query.Type))
            {
                tradesQuery = tradesQuery.Where(ct => ct.TradeType.ToLower() == query.Type.ToLower());
            }

            if (query.StartDate.HasValue)
            {
                var startDate = query.StartDate.Value;
                tradesQuery = tradesQuery.Where(ct => ct.CloseTime >= startDate);
            }

            if (query.EndDate.HasValue)
            {
                var endDate = query.EndDate.Value.AddDays(1); // Include the entire end date
                tradesQuery = tradesQuery.Where(ct => ct.CloseTime < endDate);
            }

            if (query.MinProfit.HasValue)
            {
                tradesQuery = tradesQuery.Where(ct => ct.RealizedPandL >= query.MinProfit.Value);
            }

            if (query.MaxProfit.HasValue)
            {
                tradesQuery = tradesQuery.Where(ct => ct.RealizedPandL <= query.MaxProfit.Value);
            }

            if (query.MinVolume.HasValue)
            {
                tradesQuery = tradesQuery.Where(ct => ct.VolumeLots >= query.MinVolume.Value);
            }

            if (query.MaxVolume.HasValue)
            {
                tradesQuery = tradesQuery.Where(ct => ct.VolumeLots <= query.MaxVolume.Value);
            }

            // Apply sorting
            tradesQuery = query.ValidatedSortBy.ToLowerInvariant() switch
            {
                "opentime" => query.ValidatedSortOrder == "desc" 
                    ? tradesQuery.OrderByDescending(ct => ct.OpenTime)
                    : tradesQuery.OrderBy(ct => ct.OpenTime),
                "closetime" => query.ValidatedSortOrder == "desc" 
                    ? tradesQuery.OrderByDescending(ct => ct.CloseTime)
                    : tradesQuery.OrderBy(ct => ct.CloseTime),
                "symbol" => query.ValidatedSortOrder == "desc" 
                    ? tradesQuery.OrderByDescending(ct => ct.Symbol)
                    : tradesQuery.OrderBy(ct => ct.Symbol),
                "profit" => query.ValidatedSortOrder == "desc" 
                    ? tradesQuery.OrderByDescending(ct => ct.RealizedPandL)
                    : tradesQuery.OrderBy(ct => ct.RealizedPandL),
                "volume" => query.ValidatedSortOrder == "desc" 
                    ? tradesQuery.OrderByDescending(ct => ct.VolumeLots)
                    : tradesQuery.OrderBy(ct => ct.VolumeLots),
                _ => tradesQuery.OrderByDescending(ct => ct.CloseTime)
            };

            // Calculate summary statistics for filtered data
            var summary = await CalculateTradingHistorySummaryAsync(tradesQuery, cancellationToken);

            // Get total count for pagination
            var totalItems = await tradesQuery.CountAsync(cancellationToken);
            
            // Calculate pagination metadata
            var totalPages = (int)Math.Ceiling(totalItems / (double)query.ValidatedPageSize);
            var firstItemIndex = totalItems > 0 ? ((query.ValidatedPage - 1) * query.ValidatedPageSize) + 1 : 0;
            var lastItemIndex = Math.Min(firstItemIndex + query.ValidatedPageSize - 1, totalItems);

            // Apply pagination and get results
            var trades = await tradesQuery
                .Skip((query.ValidatedPage - 1) * query.ValidatedPageSize)
                .Take(query.ValidatedPageSize)
                .Select(ct => new TradingHistoryDto
                {
                    ClosedTradeId = (int)ct.ClosedTradeId,
                    EaTicketId = string.IsNullOrEmpty(ct.EaticketId) ? 0 : long.Parse(ct.EaticketId),
                    Symbol = ct.Symbol,
                    TradeType = ct.TradeType,
                    VolumeLots = ct.VolumeLots,
                    OpenPrice = ct.OpenPrice,
                    OpenTime = ct.OpenTime,
                    ClosePrice = ct.ClosePrice,
                    CloseTime = ct.CloseTime,
                    Swap = ct.Swap ?? 0,
                    Commission = ct.Commission ?? 0,
                    RealizedPandL = ct.RealizedPandL,
                    RecordedAt = ct.RecordedAt,
                    Duration = CalculateTradeDuration(ct.OpenTime, ct.CloseTime),
                    Pips = CalculatePips(ct.Symbol, ct.OpenPrice, ct.ClosePrice, ct.TradeType),
                    VolumeInUnits = ct.VolumeLots * 100000, // Standard lot size
                    Comment = string.Empty, // Not available in EaclosedTrade
                    MagicNumber = 0, // Not available in EaclosedTrade  
                    StopLoss = null, // Not available in EaclosedTrade
                    TakeProfit = null // Not available in EaclosedTrade
                })
                .ToListAsync(cancellationToken);

            var result = new PaginatedTradingHistoryDto
            {
                Pagination = new PaginationMetadata
                {
                    CurrentPage = query.ValidatedPage,
                    PageSize = query.ValidatedPageSize,
                    TotalPages = totalPages,
                    TotalItems = totalItems,
                    HasNextPage = query.ValidatedPage < totalPages,
                    HasPreviousPage = query.ValidatedPage > 1,
                    FirstItemIndex = firstItemIndex,
                    LastItemIndex = lastItemIndex
                },
                Filters = new AppliedFilters
                {
                    Symbol = query.Symbol,
                    Type = query.Type,
                    DateRange = (query.StartDate.HasValue || query.EndDate.HasValue) 
                        ? new DateRange { StartDate = query.StartDate, EndDate = query.EndDate }
                        : null,
                    ProfitRange = (query.MinProfit.HasValue || query.MaxProfit.HasValue)
                        ? new ProfitRange { MinProfit = query.MinProfit, MaxProfit = query.MaxProfit }
                        : null,
                    VolumeRange = (query.MinVolume.HasValue || query.MaxVolume.HasValue)
                        ? new VolumeRange { MinVolume = query.MinVolume, MaxVolume = query.MaxVolume }
                        : null,
                    SortBy = query.ValidatedSortBy,
                    SortOrder = query.ValidatedSortOrder
                },
                Trades = trades,
                Summary = summary
            };

            _logger.LogInformation("Trading history retrieved successfully for account {AccountId}, {TotalItems} total items, {PageItems} items on page {Page}", 
                accountId, totalItems, trades.Count, query.ValidatedPage);

            return (result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trading history for account {AccountId}", accountId);
            return (null, "An error occurred while retrieving trading history");
        }
    }

    private async Task<TradingHistorySummary> CalculateTradingHistorySummaryAsync(
        IQueryable<EaclosedTrade> query, 
        CancellationToken cancellationToken)
    {
        if (!await query.AnyAsync(cancellationToken))
        {
            return new TradingHistorySummary();
        }

        var trades = await query.ToListAsync(cancellationToken);
        
        var profitableTrades = trades.Where(t => t.RealizedPandL > 0).ToList();
        var losingTrades = trades.Where(t => t.RealizedPandL <= 0).ToList();

        return new TradingHistorySummary
        {
            FilteredTotalProfit = trades.Sum(t => t.RealizedPandL),
            FilteredTotalTrades = trades.Count,
            FilteredProfitableTrades = profitableTrades.Count,
            FilteredLosingTrades = losingTrades.Count,
            FilteredWinRate = trades.Count > 0 ? (decimal)profitableTrades.Count / trades.Count * 100 : 0,
            FilteredGrossProfit = profitableTrades.Sum(t => t.RealizedPandL),
            FilteredGrossLoss = losingTrades.Sum(t => t.RealizedPandL),
            FilteredTotalCommission = trades.Sum(t => t.Commission ?? 0),
            FilteredTotalSwap = trades.Sum(t => t.Swap ?? 0)
        };
    }

    private static string CalculateTradeDuration(DateTime openTime, DateTime closeTime)
    {
        var duration = closeTime - openTime;
        
        if (duration.TotalDays >= 1)
        {
            return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m";
        }
        else if (duration.TotalHours >= 1)
        {
            return $"{duration.Hours}h {duration.Minutes}m";
        }
        else
        {
            return $"{duration.Minutes}m {duration.Seconds}s";
        }
    }

    private static decimal CalculatePips(string symbol, decimal openPrice, decimal closePrice, string tradeType)
    {
        // Basic pip calculation - this is simplified and should be enhanced based on actual broker specs
        var priceDifference = tradeType.ToUpper() == "BUY" 
            ? closePrice - openPrice 
            : openPrice - closePrice;

        // For JPY pairs, pip is typically 0.01, for others 0.0001
        var pipSize = symbol.Contains("JPY") ? 0.01m : 0.0001m;
        
        return Math.Round(priceDifference / pipSize, 2);
    }

    public async Task<(OpenPositionsRealtimeDto? Positions, string? ErrorMessage)> GetOpenPositionsRealtimeAsync(
        int accountId, 
        bool includeMetrics, 
        string? symbols, 
        bool refresh, 
        int userId, 
        bool isAdmin, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting real-time open positions for account {AccountId}, user {UserId}, includeMetrics {IncludeMetrics}, symbols {Symbols}, refresh {Refresh}", 
                accountId, userId, includeMetrics, symbols, refresh);

            // Authorization check - same as other methods
            if (!isAdmin)
            {
                var accountOwner = await _unitOfWork.TradingAccounts.Query()
                    .Where(ta => ta.TradingAccountId == accountId)
                    .Select(ta => ta.CreatedByUserId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (accountOwner != userId)
                {
                    return (null, "Unauthorized access to this trading account");
                }
            }

            // Get trading account with current data
            var tradingAccount = await _unitOfWork.TradingAccounts.Query()
                .FirstOrDefaultAsync(ta => ta.TradingAccountId == accountId, cancellationToken);

            if (tradingAccount == null)
            {
                return (null, $"Trading account with ID {accountId} not found");
            }

            // Build query for open positions
            var positionsQuery = _unitOfWork.EAOpenPositions.Query()
                .Where(op => op.TradingAccountId == accountId);

            // Apply symbol filter if provided
            if (!string.IsNullOrWhiteSpace(symbols))
            {
                var symbolList = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                       .Select(s => s.Trim().ToUpper())
                                       .ToList();
                positionsQuery = positionsQuery.Where(op => symbolList.Contains(op.Symbol.ToUpper()));
            }

            // Get open positions
            var openPositions = await positionsQuery
                .OrderByDescending(op => op.OpenTime)
                .ToListAsync(cancellationToken);

            // Map to detailed DTOs with calculations
            var positionDetails = openPositions.Select(op => MapToOpenPositionDetailDto(op)).ToList();

            // Calculate summary metrics
            var summary = CalculatePositionsSummary(positionDetails, tradingAccount, includeMetrics);

            // Calculate market data
            var marketData = CalculateMarketData(openPositions, tradingAccount);

            var result = new OpenPositionsRealtimeDto
            {
                Positions = positionDetails,
                Summary = summary,
                MarketData = marketData,
                LastUpdated = DateTime.UtcNow
            };

            _logger.LogInformation("Real-time open positions retrieved successfully for account {AccountId}, {PositionCount} positions", 
                accountId, positionDetails.Count);

            return (result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting real-time open positions for account {AccountId}", accountId);
            return (null, "An error occurred while retrieving real-time open positions");
        }
    }

    private OpenPositionDetailDto MapToOpenPositionDetailDto(EaopenPosition position)
    {
        var currentMarketPrice = position.CurrentMarketPrice;
        var unrealizedPnL = CalculateUnrealizedPnL(position, currentMarketPrice);
        var marginRequired = CalculateMarginRequired(position);
        var percentageReturn = CalculatePercentageReturn(position, unrealizedPnL);
        var daysOpen = (int)(DateTime.UtcNow - position.OpenTime).TotalDays;

        return new OpenPositionDetailDto
        {
            OpenPositionId = position.OpenPositionId,
            EaTicketId = position.EaticketId,
            Symbol = position.Symbol,
            TradeType = position.TradeType,
            VolumeLots = position.VolumeLots,
            OpenPrice = position.OpenPrice,
            OpenTime = position.OpenTime,
            CurrentMarketPrice = currentMarketPrice,
            UnrealizedPnL = unrealizedPnL,
            Swap = position.Swap ?? 0,
            Commission = position.Commission ?? 0,
            MarginRequired = marginRequired,
            PercentageReturn = percentageReturn,
            DaysOpen = daysOpen,
            LastUpdateTime = position.LastUpdateTime
        };
    }

    private decimal CalculateUnrealizedPnL(EaopenPosition position, decimal currentMarketPrice)
    {
        // Use existing FloatingPandL if available
        return position.FloatingPandL;
    }

    private decimal CalculateMarginRequired(EaopenPosition position)
    {
        // Simplified margin calculation - should be enhanced with actual leverage and symbol specifications
        var contractSize = GetContractSize(position.Symbol);
        var leverage = 100; // Default leverage, should come from account settings
        
        return Math.Round((position.OpenPrice * position.VolumeLots * contractSize) / leverage, 2);
    }

    private decimal CalculatePercentageReturn(EaopenPosition position, decimal unrealizedPnL)
    {
        var marginRequired = CalculateMarginRequired(position);
        return marginRequired > 0 ? Math.Round((unrealizedPnL / marginRequired) * 100, 2) : 0;
    }

    private decimal GetContractSize(string symbol)
    {
        // Simplified contract size mapping - should be enhanced with actual symbol specifications
        return symbol.Contains("JPY") ? 1000 : 100000;
    }

    private PositionsSummaryDto CalculatePositionsSummary(
        List<OpenPositionDetailDto> positions, 
        TradingAccount tradingAccount, 
        bool includeMetrics)
    {
        var longPositions = positions.Where(p => p.TradeType.ToUpper() == "BUY").ToList();
        var shortPositions = positions.Where(p => p.TradeType.ToUpper() == "SELL").ToList();
        
        var totalUnrealizedPnL = positions.Sum(p => p.UnrealizedPnL);
        var totalMarginUsed = positions.Sum(p => p.MarginRequired);
        var accountEquity = tradingAccount.CurrentNetAssetValue;
        var freeMargin = Math.Max(0, accountEquity - totalMarginUsed);
        var marginLevel = totalMarginUsed > 0 ? (accountEquity / totalMarginUsed) * 100 : 0;

        var summary = new PositionsSummaryDto
        {
            TotalPositions = positions.Count,
            TotalUnrealizedPnL = totalUnrealizedPnL,
            TotalMarginUsed = totalMarginUsed,
            FreeMargin = freeMargin,
            MarginLevel = marginLevel,
            TotalVolume = positions.Sum(p => p.VolumeLots),
            LongPositions = longPositions.Count,
            ShortPositions = shortPositions.Count,
            LongVolume = longPositions.Sum(p => p.VolumeLots),
            ShortVolume = shortPositions.Sum(p => p.VolumeLots),
            DailyPnL = 0, // Would require additional calculation
            WeeklyPnL = 0, // Would require additional calculation
            MonthlyPnL = 0 // Would require additional calculation
        };

        return summary;
    }

    private MarketDataDto CalculateMarketData(
        List<EaopenPosition> positions, 
        TradingAccount tradingAccount)
    {
        var uniqueSymbols = positions.Select(p => p.Symbol).Distinct().ToList();
        var quotes = new List<SymbolQuoteDto>();

        // Generate mock quotes for each symbol (in real implementation, this would fetch from market data provider)
        foreach (var symbol in uniqueSymbols)
        {
            var lastPrice = positions.Where(p => p.Symbol == symbol)
                                   .Select(p => p.CurrentMarketPrice)
                                   .FirstOrDefault();

            quotes.Add(new SymbolQuoteDto
            {
                Symbol = symbol,
                Bid = lastPrice - 0.0001m, // Mock spread
                Ask = lastPrice + 0.0001m,
                Spread = 0.0002m,
                DailyChange = 0, // Would require historical data
                DailyChangePercent = 0, // Would require historical data
                LastUpdate = DateTime.UtcNow
            });
        }

        var accountEquity = tradingAccount.CurrentNetAssetValue;
        var accountBalance = tradingAccount.InitialCapital; // Simplified - should be current balance
        var drawdownPercent = accountBalance > 0 ? Math.Max(0, ((accountBalance - accountEquity) / accountBalance) * 100) : 0;

        return new MarketDataDto
        {
            LastPriceUpdate = DateTime.UtcNow,
            Quotes = quotes,
            AccountEquity = accountEquity,
            AccountBalance = accountBalance,
            DrawdownPercent = drawdownPercent
        };
    }

    public async Task<(TradingStatisticsDto? Statistics, string? ErrorMessage)> GetStatisticsAsync(
        int accountId, 
        GetStatisticsQuery query, 
        int userId, 
        bool isAdmin, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting statistics for account {AccountId} with query {@Query}", accountId, query);

            // Verify account exists and user has access
            var tradingAccount = await _unitOfWork.TradingAccounts
                .GetByIdAsync(accountId);

            if (tradingAccount == null)
            {
                return (null, $"Trading account with ID {accountId} not found");
            }

            // Check authorization
            if (!isAdmin && tradingAccount.CreatedByUserId != userId)
            {
                return (null, "Unauthorized access to trading account");
            }

            // Get date range based on period
            var (startDate, endDate) = GetDateRangeForPeriod(query.Period, tradingAccount.CreatedAt);

            // Simple statistics implementation for now
            var statistics = new TradingStatisticsDto
            {
                Period = query.Period.ToString(),
                DateRange = new DateRangeDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalDays = (int)(endDate - startDate).TotalDays + 1,
                    TradingDays = CalculateTradingDays(startDate, endDate)
                },
                TradingStats = new TradingStatsDto
                {
                    TotalTrades = 0,
                    ProfitableTrades = new TradeCountDto { Count = 0, Percentage = 0 },
                    LosingTrades = new TradeCountDto { Count = 0, Percentage = 0 },
                    BreakEvenTrades = new TradeCountDto { Count = 0, Percentage = 0 },
                    BestTrade = 0,
                    WorstTrade = 0,
                    AverageProfit = 0,
                    AverageLoss = 0,
                    LargestProfitTrade = 0,
                    LargestLossTrade = 0,
                    MaxConsecutiveWins = 0,
                    MaxConsecutiveLosses = 0,
                    AverageTradeDuration = "00:00:00",
                    TradesPerDay = 0,
                    TradesPerWeek = 0,
                    TradesPerMonth = 0
                },
                FinancialStats = new FinancialStatsDto
                {
                    GrossProfit = 0,
                    GrossLoss = 0,
                    TotalNetProfit = 0,
                    ProfitFactor = 0,
                    ExpectedPayoff = 0,
                    AverageTradeNetProfit = 0,
                    ReturnOnInvestment = 0,
                    AnnualizedReturn = 0,
                    TotalCommission = 0,
                    TotalSwap = 0,
                    NetProfitAfterCosts = 0
                },
                RiskMetrics = new RiskMetricsDto
                {
                    MaxDrawdown = new MaxDrawdownInfoDto
                    {
                        Amount = 0,
                        Percentage = 0,
                        Date = DateTime.UtcNow,
                        Duration = "0 days",
                        RecoveryTime = "N/A"
                    },
                    AverageDrawdown = 0,
                    CalmarRatio = 0,
                    MaxDailyLoss = 0,
                    MaxDailyProfit = 0,
                    AverageDailyPL = 0,
                    Volatility = 0,
                    StandardDeviation = 0,
                    DownsideDeviation = 0,
                    RiskOfRuin = 0,
                    WinLossRatio = 0,
                    PayoffRatio = 0
                },
                AdvancedMetrics = query.IncludeAdvanced ? new AdvancedMetricsDto
                {
                    SharpeRatio = 0,
                    SortinoRatio = 0,
                    InformationRatio = 0,
                    TreynorRatio = 0,
                    Alpha = 0,
                    Beta = 1,
                    RSquared = 0,
                    TrackingError = 0,
                    ValueAtRisk95 = 0,
                    ValueAtRisk99 = 0,
                    ConditionalVaR = 0,
                    MaxLeverageUsed = 0,
                    AverageLeverage = 0
                } : null,
                SymbolBreakdown = new List<SymbolBreakdownDto>(),
                MonthlyPerformance = new List<MonthlyPerformanceDto>()
            };

            return (statistics, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for account {AccountId}", accountId);
            return (null, "An error occurred while calculating statistics");
        }
    }

    private static (DateTime startDate, DateTime endDate) GetDateRangeForPeriod(TimePeriod period, DateTime accountCreationDate)
    {
        var endDate = DateTime.UtcNow.Date;
        DateTime startDate;

        switch (period)
        {
            case TimePeriod.OneMonth:
                startDate = endDate.AddDays(-30);
                break;
            case TimePeriod.ThreeMonths:
                startDate = endDate.AddDays(-90);
                break;
            case TimePeriod.SixMonths:
                startDate = endDate.AddDays(-180);
                break;
            case TimePeriod.OneYear:
                startDate = endDate.AddDays(-365);
                break;
            case TimePeriod.All:
            default:
                startDate = accountCreationDate.Date;
                break;
        }

        // Ensure start date is not before account creation
        startDate = startDate < accountCreationDate.Date ? accountCreationDate.Date : startDate;

        return (startDate, endDate);
    }

    private static int CalculateTradingDays(DateTime startDate, DateTime endDate)
    {
        var tradingDays = 0;
        var current = startDate;

        while (current <= endDate)
        {
            // Exclude weekends (simplified - doesn't account for holidays)
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
            {
                tradingDays++;
            }
            current = current.AddDays(1);
        }

        return tradingDays;
    }
}
