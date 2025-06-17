// QuantumBands.Application/Services/TradingAccountService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Queries;
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Interfaces.Repositories; // Assuming specific repositories if needed
using QuantumBands.Application.Services;
using QuantumBands.Domain.Entities;
using QuantumBands.Domain.Entities.Enums;
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
}