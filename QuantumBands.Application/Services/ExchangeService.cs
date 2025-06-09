// QuantumBands.Application/Services/ExchangeService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.ExchangeMonitor.Dtos;
using QuantumBands.Application.Features.Admin.ExchangeMonitor.Queries;
using QuantumBands.Application.Features.Exchange.Commands.CreateOrder;
using QuantumBands.Application.Features.Exchange.Dtos;
using QuantumBands.Application.Features.Exchange.Queries;
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Interfaces.Repositories; // Nếu có specific repositories
using QuantumBands.Domain.Entities;
using QuantumBands.Domain.Entities.Enums;
using System;
using System.IdentityModel.Tokens.Jwt; // For Include, FirstOrDefaultAsync
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.Application.Services;

public class ExchangeService : IExchangeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExchangeService> _logger;
    private readonly ISharePortfolioService _portfolioService; // Để cập nhật portfolio
    private readonly IWalletService _walletService; // Cần service để tạo WalletTransactions
    private readonly ISystemSettingRepository _systemSettingRepository; // Để lấy phí giao dịch
    private readonly ITransactionTypeRepository _transactionTypeRepository;
    private readonly IShareOrderStatusRepository _shareOrderStatusRepository; // Để lấy ID các trạng thái

    public ExchangeService(
        IUnitOfWork unitOfWork,
        ILogger<ExchangeService> logger,
        ISharePortfolioService portfolioService,
        IWalletService walletService,
        ISystemSettingRepository systemSettingRepository,
        ITransactionTypeRepository transactionTypeRepository,
        IShareOrderStatusRepository shareOrderStatusRepository)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _portfolioService = portfolioService;
        _walletService = walletService;
        _systemSettingRepository = systemSettingRepository;
        _transactionTypeRepository = transactionTypeRepository;
        _shareOrderStatusRepository = shareOrderStatusRepository;

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

    public async Task<PaginatedList<AdminShareOrderViewDto>> GetAdminAllOrdersAsync(GetAdminAllOrdersQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Admin fetching all share orders with query: {@Query}", query);

        var ordersQuery = _unitOfWork.ShareOrders.Query()
                            .Include(o => o.User) // Nạp thông tin người dùng
                            .Include(o => o.TradingAccount)
                            .Include(o => o.ShareOrderSide)
                            .Include(o => o.ShareOrderType)
                            .Include(o => o.ShareOrderStatus)
                            .AsQueryable();

        // Áp dụng Filter
        if (query.TradingAccountId.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.TradingAccountId == query.TradingAccountId.Value);
        }
        if (query.UserId.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.UserId == query.UserId.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string searchTermLower = query.SearchTerm.ToLowerInvariant();
            ordersQuery = ordersQuery.Where(o =>
                (o.User.Username != null && o.User.Username.ToLower().Contains(searchTermLower)) ||
                (o.User.Email != null && o.User.Email.ToLower().Contains(searchTermLower)) ||
                (o.User.FullName != null && o.User.FullName.ToLower().Contains(searchTermLower))
            );
        }
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statusFilters = query.Status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(s => s.ToLowerInvariant()).ToList();
            if (statusFilters.Any())
            {
                ordersQuery = ordersQuery.Where(o => statusFilters.Contains(o.ShareOrderStatus.StatusName.ToLower()));
            }
        }
        if (!string.IsNullOrWhiteSpace(query.OrderSide))
        {
            ordersQuery = ordersQuery.Where(o => o.ShareOrderSide.SideName.Equals(query.OrderSide, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(query.OrderType))
        {
            ordersQuery = ordersQuery.Where(o => o.ShareOrderType.TypeName.Equals(query.OrderType, StringComparison.OrdinalIgnoreCase));
        }
        if (query.DateFrom.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.OrderDate >= query.DateFrom.Value.Date);
        }
        if (query.DateTo.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.OrderDate < query.DateTo.Value.Date.AddDays(1));
        }


        // Áp dụng Sắp xếp
        bool isDescending = query.SortOrder?.ToLower() == "desc";
        Expression<Func<ShareOrder, object>> orderByExpression;

        switch (query.SortBy?.ToLowerInvariant())
        {
            case "userid": orderByExpression = o => o.UserId; break;
            case "username": orderByExpression = o => o.User.Username; break;
            case "tradingaccountname": orderByExpression = o => o.TradingAccount.AccountName; break;
            case "quantityordered": orderByExpression = o => o.QuantityOrdered; break;
            case "limitprice": orderByExpression = o => o.LimitPrice!; break;
            case "status": orderByExpression = o => o.ShareOrderStatus.StatusName; break;
            case "orderdate": default: orderByExpression = o => o.OrderDate; break;
        }
        ordersQuery = isDescending ? ordersQuery.OrderByDescending(orderByExpression) : ordersQuery.OrderBy(orderByExpression);

        var paginatedOrders = await PaginatedList<ShareOrder>.CreateAsync(
            ordersQuery, query.ValidatedPageNumber, query.ValidatedPageSize, cancellationToken);

        var dtos = paginatedOrders.Items.Select(o => new AdminShareOrderViewDto
        {
            OrderId = o.OrderId,
            UserId = o.UserId,
            Username = o.User.Username,
            UserEmail = o.User.Email,
            TradingAccountId = o.TradingAccountId,
            TradingAccountName = o.TradingAccount.AccountName,
            OrderSide = o.ShareOrderSide.SideName,
            OrderType = o.ShareOrderType.TypeName,
            QuantityOrdered = o.QuantityOrdered,
            QuantityFilled = o.QuantityFilled,
            LimitPrice = o.LimitPrice,
            AverageFillPrice = o.AverageFillPrice,
            OrderStatus = o.ShareOrderStatus.StatusName,
            OrderDate = o.OrderDate,
            UpdatedAt = o.UpdatedAt,
            TransactionFee = o.TransactionFeeAmount
        }).ToList();

        return new PaginatedList<AdminShareOrderViewDto>(
            dtos, paginatedOrders.TotalCount, paginatedOrders.PageNumber, paginatedOrders.PageSize);
    }

    public async Task<PaginatedList<AdminShareTradeViewDto>> GetAdminAllTradesAsync(GetAdminAllTradesQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Admin fetching all share trades with query: {@Query}", query);

        var tradesQuery = _unitOfWork.ShareTrades.Query()
                            .Include(st => st.TradingAccount)
                            .Include(st => st.BuyerUser)
                            .Include(st => st.SellerUser)
                            .AsQueryable();

        // Áp dụng Filter
        if (query.TradingAccountId.HasValue)
        {
            tradesQuery = tradesQuery.Where(st => st.TradingAccountId == query.TradingAccountId.Value);
        }
        if (query.BuyerUserId.HasValue)
        {
            tradesQuery = tradesQuery.Where(st => st.BuyerUserId == query.BuyerUserId.Value);
        }
        if (query.SellerUserId.HasValue)
        {
            tradesQuery = tradesQuery.Where(st => st.SellerUserId == query.SellerUserId.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.BuyerSearchTerm))
        {
            string searchTerm = query.BuyerSearchTerm.ToLowerInvariant();
            tradesQuery = tradesQuery.Where(st => st.BuyerUser.Username.ToLower().Contains(searchTerm) || st.BuyerUser.Email.ToLower().Contains(searchTerm));
        }
        if (!string.IsNullOrWhiteSpace(query.SellerSearchTerm))
        {
            string searchTerm = query.SellerSearchTerm.ToLowerInvariant();
            tradesQuery = tradesQuery.Where(st => st.SellerUser.Username.ToLower().Contains(searchTerm) || st.SellerUser.Email.ToLower().Contains(searchTerm));
        }
        if (query.MinAmount.HasValue)
        {
            tradesQuery = tradesQuery.Where(st => (st.QuantityTraded * st.TradePrice) >= query.MinAmount.Value);
        }
        if (query.MaxAmount.HasValue)
        {
            tradesQuery = tradesQuery.Where(st => (st.QuantityTraded * st.TradePrice) <= query.MaxAmount.Value);
        }
        if (query.DateFrom.HasValue)
        {
            tradesQuery = tradesQuery.Where(st => st.TradeDate >= query.DateFrom.Value.Date);
        }
        if (query.DateTo.HasValue)
        {
            tradesQuery = tradesQuery.Where(st => st.TradeDate < query.DateTo.Value.Date.AddDays(1));
        }

        // Áp dụng Sắp xếp
        bool isDescending = query.SortOrder?.ToLower() == "desc";
        // Tạo expression cho trường sắp xếp
        var parameter = Expression.Parameter(typeof(ShareTrade), "st");
        Expression property;
        switch (query.SortBy?.ToLowerInvariant())
        {
            case "tradingaccountname": property = Expression.Property(Expression.Property(parameter, "TradingAccount"), "AccountName"); break;
            case "quantitytraded": property = Expression.Property(parameter, "QuantityTraded"); break;
            case "tradeprice": property = Expression.Property(parameter, "TradePrice"); break;
            case "totalvalue": property = Expression.Multiply(Expression.Convert(Expression.Property(parameter, "QuantityTraded"), typeof(decimal)), Expression.Property(parameter, "TradePrice")); break;
            case "tradedate": default: property = Expression.Property(parameter, "TradeDate"); break;
        }
        var orderByExpression = Expression.Lambda<Func<ShareTrade, object>>(Expression.Convert(property, typeof(object)), parameter);

        tradesQuery = isDescending
            ? tradesQuery.OrderByDescending(orderByExpression)
            : tradesQuery.OrderBy(orderByExpression);


        var paginatedTrades = await PaginatedList<ShareTrade>.CreateAsync(
            tradesQuery, query.ValidatedPageNumber, query.ValidatedPageSize, cancellationToken);

        var dtos = paginatedTrades.Items.Select(st => new AdminShareTradeViewDto
        {
            TradeId = st.TradeId,
            TradingAccountId = st.TradingAccountId,
            TradingAccountName = st.TradingAccount.AccountName,
            BuyerUserId = st.BuyerUserId,
            BuyerUsername = st.BuyerUser.Username,
            SellerUserId = st.SellerUserId,
            SellerUsername = st.SellerUser.Username,
            QuantityTraded = st.QuantityTraded,
            TradePrice = st.TradePrice,
            TotalValue = st.QuantityTraded * st.TradePrice,
            BuyerFeeAmount = st.BuyerFeeAmount,
            SellerFeeAmount = st.SellerFeeAmount,
            TradeDate = st.TradeDate
        }).ToList();

        return new PaginatedList<AdminShareTradeViewDto>(
            dtos, paginatedTrades.TotalCount, paginatedTrades.PageNumber, paginatedTrades.PageSize);
    }
    public async Task<(ShareOrderDto? Order, string? ErrorMessage)> PlaceOrderAsync(CreateShareOrderRequest request, ClaimsPrincipal currentUser, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromPrincipal(currentUser);
        if (!userId.HasValue)
        {
            return (null, "User not authenticated.");
        }

        _logger.LogInformation("UserID {UserId} attempting to place {OrderSide} order for TradingAccountID {TradingAccountId}, Quantity: {Quantity}, TypeID: {OrderTypeId}, LimitPrice: {LimitPrice}",
                               userId.Value, request.OrderSide, request.TradingAccountId, request.QuantityOrdered, request.OrderTypeId, request.LimitPrice);

        // 1. Lấy thông tin cần thiết từ DB
        var tradingAccount = await _unitOfWork.TradingAccounts.GetByIdAsync(request.TradingAccountId);
        if (tradingAccount == null || !tradingAccount.IsActive)
        {
            return (null, "Trading account not found or is inactive.");
        }

        var orderTypeEntity = await _unitOfWork.ShareOrderTypes.GetByIdAsync(request.OrderTypeId); // Giả sử có repo này
        if (orderTypeEntity == null)
        {
            return (null, "Invalid order type specified.");
        }

        // Xác định ShareOrderSideId dựa trên request.OrderSide
        // Giả sử bạn có bảng ShareOrderSides với các bản ghi "Buy" (ID 1) và "Sell" (ID 2)
        string requestedOrderSideLower = request.OrderSide.ToLowerInvariant();
        var orderSideEntity = await _unitOfWork.ShareOrderSides.Query()
                                    // So sánh cột SideName đã được chuyển sang chữ thường với input đã được chuẩn hóa
                                    .FirstOrDefaultAsync(s => s.SideName.ToLower() == requestedOrderSideLower, cancellationToken);
        if (orderSideEntity == null)
        {
            return (null, "Invalid OrderSide specified. Must be 'Buy' or 'Sell'.");
        }

        // Nếu là lệnh Limit, phải có LimitPrice
        if (orderTypeEntity.TypeName.Equals("Limit", StringComparison.OrdinalIgnoreCase) && !request.LimitPrice.HasValue)
        {
            return (null, "Limit price is required for Limit orders.");
        }
        // Nếu là lệnh Market, bỏ qua LimitPrice
        decimal? effectiveLimitPrice = orderTypeEntity.TypeName.Equals("Limit", StringComparison.OrdinalIgnoreCase) ? request.LimitPrice : null;


        // 2. Kiểm tra điều kiện đặt lệnh
        if (orderSideEntity.SideName.Equals("Buy", StringComparison.OrdinalIgnoreCase))
        {
            var userWallet = await _unitOfWork.Wallets.Query()
                                 .FirstOrDefaultAsync(w => w.UserId == userId.Value, cancellationToken);
            if (userWallet == null) return (null, "User wallet not found.");

            decimal estimatedCost = (decimal)(request.QuantityOrdered * (effectiveLimitPrice ?? tradingAccount.CurrentSharePrice)); // Ước tính chi phí
            // Thêm logic phí giao dịch ở đây nếu có
            // estimatedCost += CalculateTransactionFee(estimatedCost);

            if (userWallet.Balance < estimatedCost)
            {
                return (null, "Insufficient wallet balance.");
            }
            // TODO: Logic "tạm giữ" tiền trong ví (có thể tạo một giao dịch ví trạng thái "PendingOrder")
        }
        else // Sell Order
        {
            var userPortfolioItem = await _unitOfWork.SharePortfolios.Query()
                                        .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.TradingAccountId == request.TradingAccountId, cancellationToken);
            if (userPortfolioItem == null || userPortfolioItem.Quantity < request.QuantityOrdered)
            {
                return (null, "Insufficient shares to sell.");
            }
            // TODO: Logic "tạm giữ" cổ phần (có thể cập nhật một trường "HeldQuantity" trong SharePortfolios)
        }

        // 3. Tạo bản ghi ShareOrder
        string openStatusNameLower = nameof(ShareOrderStatusName.Open).ToLowerInvariant();
        var orderStatusOpen = await _unitOfWork.ShareOrderStatuses.Query()
                                     .FirstOrDefaultAsync(s => s.StatusName.ToLower() == openStatusNameLower, cancellationToken);
        if (orderStatusOpen == null)
        {
            _logger.LogError("Default order status 'Open' not found in database.");
            return (null, "System error: Order status configuration missing.");
        }

        var newOrder = new ShareOrder
        {
            UserId = userId.Value,
            TradingAccountId = request.TradingAccountId,
            OrderSideId = orderSideEntity.OrderSideId,
            OrderTypeId = request.OrderTypeId,
            QuantityOrdered = request.QuantityOrdered,
            QuantityFilled = 0,
            LimitPrice = effectiveLimitPrice,
            OrderStatusId = orderStatusOpen.OrderStatusId,
            OrderDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            // TransactionFee = CalculateTransactionFee(...) // Tính phí nếu có
        };

        await _unitOfWork.ShareOrders.AddAsync(newOrder);
        await _unitOfWork.CompleteAsync(cancellationToken);

        _logger.LogInformation("Order (ID: {OrderId}) placed successfully for UserID {UserId}.", newOrder.OrderId, userId.Value);

        _logger.LogInformation("Order (ID: {OrderId}) placed by UserID {UserId}, attempting to match.", newOrder.OrderId, userId.Value);
        bool matched = await TryMatchOrderAsync(newOrder.OrderId, cancellationToken);
        if (matched)
        {
            _logger.LogInformation("Order {OrderId} had immediate matches. Fetching updated order details.", newOrder.OrderId);
        }



        // 4. Tạo DTO trả về
        var orderDto = new ShareOrderDto
        {
            OrderId = newOrder.OrderId,
            UserId = newOrder.UserId,
            TradingAccountId = newOrder.TradingAccountId,
            TradingAccountName = tradingAccount.AccountName,
            OrderSide = orderSideEntity.SideName,
            OrderType = orderTypeEntity.TypeName,
            QuantityOrdered = newOrder.QuantityOrdered,
            QuantityFilled = newOrder.QuantityFilled,
            LimitPrice = newOrder.LimitPrice,
            AverageFillPrice = null, // Chưa khớp
            OrderStatus = orderStatusOpen.StatusName,
            OrderDate = newOrder.OrderDate,
            UpdatedAt = newOrder.UpdatedAt,
            TransactionFee = null // Tính toán sau hoặc nếu có
        };

        return (orderDto, null);
    }

    public async Task<bool> TryMatchOrderAsync(long orderId, CancellationToken cancellationToken = default)
    {
        var orderToMatch = await _unitOfWork.ShareOrders.Query()
            .Include(o => o.ShareOrderSide)
            .Include(o => o.ShareOrderType)
            .Include(o => o.ShareOrderStatus)
            .Include(o => o.User).ThenInclude(u => u.Wallet)
            .Include(o => o.TradingAccount)
            .FirstOrDefaultAsync(o => o.OrderId == orderId &&
                                     (o.ShareOrderStatus.StatusName == nameof(ShareOrderStatusName.Open) ||
                                      o.ShareOrderStatus.StatusName == nameof(ShareOrderStatusName.PartiallyFilled)),
                                 cancellationToken);

        if (orderToMatch == null)
        {
            _logger.LogDebug("TryMatchOrderAsync: OrderID {OrderId} not found or not in a matchable state.", orderId);
            return false;
        }

        _logger.LogInformation("MatchingEngine: Attempting to match OrderID: {OrderId}, Side: {OrderSide}, Type: {OrderType}, QtyLeft: {QuantityLeft}, LimitPrice: {LimitPrice}",
            orderToMatch.OrderId, orderToMatch.ShareOrderSide.SideName, orderToMatch.ShareOrderType.TypeName,
            orderToMatch.QuantityOrdered - orderToMatch.QuantityFilled, orderToMatch.LimitPrice);

        bool anyMatchOccurredThisRun = false;
        long remainingQtyToFillForOrderToMatch = orderToMatch.QuantityOrdered - orderToMatch.QuantityFilled;

        var statusOpen = await _shareOrderStatusRepository.GetByNameAsync(nameof(ShareOrderStatusName.Open), cancellationToken);
        var statusPartiallyFilled = await _shareOrderStatusRepository.GetByNameAsync(nameof(ShareOrderStatusName.PartiallyFilled), cancellationToken);
        var statusFilled = await _shareOrderStatusRepository.GetByNameAsync(nameof(ShareOrderStatusName.Filled), cancellationToken);

        if (statusOpen == null || statusPartiallyFilled == null || statusFilled == null)
        {
            _logger.LogError("Critical: Order statuses (Open, PartiallyFilled, Filled) not found in database via repository.");
            return false;
        }

        var sharePurchaseType = await _transactionTypeRepository.GetByNameAsync("SharePurchase", cancellationToken);
        var shareSaleProceedsType = await _transactionTypeRepository.GetByNameAsync("ShareSaleProceeds", cancellationToken);
        var exchangeFeeType = await _transactionTypeRepository.GetByNameAsync("ExchangeFee", cancellationToken);

        if (sharePurchaseType == null || shareSaleProceedsType == null || exchangeFeeType == null) // Giả sử phí luôn có
        {
            _logger.LogError("Required transaction types for exchange (SharePurchase, ShareSaleProceeds, ExchangeFee) not found.");
            return false;
        }

        decimal feeRate = 0;
        var feeRateStr = await _systemSettingRepository.GetSettingValueAsync("ShareTradingFeeRate", cancellationToken);
        if (decimal.TryParse(feeRateStr, out decimal parsedRate))
        {
            feeRate = parsedRate;
        }

        if (orderToMatch.ShareOrderSide.SideName.Equals("Buy", StringComparison.OrdinalIgnoreCase))
        {
            // ---- START MATCHING WITH INITIAL SHARE OFFERINGS ----
            if (remainingQtyToFillForOrderToMatch > 0)
            {
                var activeOfferings = await _unitOfWork.InitialShareOfferings.Query()
                    .Include(offering => offering.AdminUser) // To get SellerUserId (Admin)
                    .Where(offering => offering.TradingAccountId == orderToMatch.TradingAccountId &&
                                        offering.Status == nameof(OfferingStatus.Active) &&
                                        (offering.SharesOffered - offering.SharesSold) > 0 &&
                                        (orderToMatch.ShareOrderType.TypeName == "Market" ||
                                         (orderToMatch.LimitPrice.HasValue && offering.OfferingPricePerShare <= orderToMatch.LimitPrice.Value)))
                    .OrderBy(offering => offering.OfferingPricePerShare)
                    .ThenBy(offering => offering.OfferingStartDate)
                    .ToListAsync(cancellationToken);

                foreach (var offering in activeOfferings)
                {
                    if (remainingQtyToFillForOrderToMatch <= 0) break;

                    long offeringAvailableShares = offering.SharesOffered - offering.SharesSold;
                    decimal tradePrice = offering.OfferingPricePerShare;

                    if (orderToMatch.ShareOrderType.TypeName == "Limit" && orderToMatch.LimitPrice.HasValue && tradePrice > orderToMatch.LimitPrice.Value)
                    {
                        continue; // Price too high for limit buy order
                    }

                    long tradeQuantity = Math.Min(remainingQtyToFillForOrderToMatch, offeringAvailableShares);
                    decimal totalTradeValue = tradeQuantity * tradePrice;
                    decimal buyerFee = Math.Round(totalTradeValue * feeRate, 8);

                    if (orderToMatch.User.Wallet == null || orderToMatch.User.Wallet.Balance < (totalTradeValue + buyerFee))
                    {
                        _logger.LogWarning("Buy Order {OrderId}: Insufficient balance for UserID {BuyerUserId} to match with offering {OfferingId}. Required: {Required}, Available: {Available}.",
                                           orderToMatch.OrderId, orderToMatch.UserId, offering.OfferingId, totalTradeValue + buyerFee, orderToMatch.User.Wallet.Balance);
                        continue;
                    }

                    _logger.LogInformation("MATCH (OFFERING): Buy Order {BuyOrderId} vs Offering {OfferingId}. Trade Qty: {TradeQty} @ {TradePrice}",
                                           orderToMatch.OrderId, offering.OfferingId, tradeQuantity, tradePrice);

                    var trade = new ShareTrade
                    {
                        TradingAccountId = orderToMatch.TradingAccountId,
                        BuyOrderId = orderToMatch.OrderId,
                        SellOrderId = null,
                        InitialShareOfferingId = offering.OfferingId,
                        BuyerUserId = orderToMatch.UserId,
                        SellerUserId = offering.AdminUserId,
                        QuantityTraded = tradeQuantity,
                        TradePrice = tradePrice,
                        BuyerFeeAmount = buyerFee,
                        SellerFeeAmount = 0, // Assume no fee for system selling from offering
                        TradeDate = DateTime.UtcNow
                    };
                    await _unitOfWork.ShareTrades.AddAsync(trade);
                    // Consider saving here to get trade.TradeId if needed immediately for WalletTransaction reference

                    orderToMatch.QuantityFilled += tradeQuantity;
                    orderToMatch.AverageFillPrice = CalculateNewAveragePrice(orderToMatch.AverageFillPrice, orderToMatch.QuantityFilled - tradeQuantity, tradePrice, tradeQuantity);
                    orderToMatch.OrderStatusId = orderToMatch.QuantityFilled == orderToMatch.QuantityOrdered ? statusFilled.OrderStatusId : statusPartiallyFilled.OrderStatusId;
                    orderToMatch.UpdatedAt = DateTime.UtcNow;
                    orderToMatch.TransactionFeeAmount = (orderToMatch.TransactionFeeAmount ?? 0) + buyerFee;
                    _unitOfWork.ShareOrders.Update(orderToMatch);

                    offering.SharesSold += tradeQuantity;
                    if (offering.SharesSold >= offering.SharesOffered)
                    {
                        offering.Status = nameof(OfferingStatus.Completed);
                    }
                    offering.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.InitialShareOfferings.Update(offering);

                    await _portfolioService.UpdatePortfolioOnBuyAsync(orderToMatch.UserId, orderToMatch.TradingAccountId, tradeQuantity, tradePrice, cancellationToken);
                    var buyerWallet = orderToMatch.User.Wallet; // Đảm bảo đã Include User.Wallet
                    var now = DateTime.UtcNow;

                    // 1. Giao dịch mua chính
                    var purchaseTransaction = new WalletTransaction
                    {
                        WalletId = buyerWallet.WalletId,
                        TransactionTypeId = sharePurchaseType.TransactionTypeId, // Đảm bảo sharePurchaseType đã được lấy và không null
                        Amount = totalTradeValue, // Số tiền hàng
                        CurrencyCode = buyerWallet.CurrencyCode,
                        BalanceBefore = buyerWallet.Balance,
                        BalanceAfter = buyerWallet.Balance - (totalTradeValue + buyerFee), // Trừ cả tiền hàng và phí
                        Description = $"Purchased {tradeQuantity} shares TA_ID {orderToMatch.TradingAccountId} @ {tradePrice:F8}. Trade ID: {trade.TradeId}", // Thêm TradeId nếu có
                        ReferenceId = $"TRADE_{trade.TradeId}", // Sử dụng TradeId (cần lưu trade trước hoặc có cơ chế tạo Ref ID khác)
                        PaymentMethod = "Exchange",
                        Status = "Completed",
                        TransactionDate = now,
                        UpdatedAt = now
                    };
                    await _unitOfWork.WalletTransactions.AddAsync(purchaseTransaction);

                    // Cập nhật số dư ví người mua
                    buyerWallet.Balance -= (totalTradeValue + buyerFee);
                    buyerWallet.UpdatedAt = now;
                    _unitOfWork.Wallets.Update(buyerWallet);

                    // 2. Giao dịch phí của người mua (nếu có)
                    if (buyerFee > 0 && exchangeFeeType != null)
                    {
                        var feeTransaction = new WalletTransaction
                        {
                            WalletId = buyerWallet.WalletId,
                            TransactionTypeId = exchangeFeeType.TransactionTypeId,
                            Amount = buyerFee,
                            CurrencyCode = buyerWallet.CurrencyCode,
                            BalanceBefore = buyerWallet.Balance + buyerFee, // Balance trước khi trừ phí này (sau khi đã trừ tiền hàng)
                            BalanceAfter = buyerWallet.Balance, // Balance cuối cùng (đã được cập nhật ở trên)
                            Description = $"Fee for purchase TRADE_{trade.TradeId}",
                            ReferenceId = $"FEE_TRADE_{trade.TradeId}",
                            PaymentMethod = "ExchangeFee",
                            Status = "Completed",
                            TransactionDate = now,
                            UpdatedAt = now
                        };
                        await _unitOfWork.WalletTransactions.AddAsync(feeTransaction);
                    }


                    // TODO: Logic for crediting funds from offering sale to a system/fund wallet if necessary

                    remainingQtyToFillForOrderToMatch -= tradeQuantity;
                    anyMatchOccurredThisRun = true;
                }
            }
            // ---- END MATCHING WITH INITIAL SHARE OFFERINGS ----

            // ---- START MATCHING WITH COUNTER SELL ORDERS ----
            if (remainingQtyToFillForOrderToMatch > 0)
            {
                var counterSellOrders = await _unitOfWork.ShareOrders.Query()
                    .Include(o => o.ShareOrderSide)
                    .Include(o => o.ShareOrderType)
                    .Include(o => o.ShareOrderStatus)
                    .Include(o => o.User).ThenInclude(u => u.Wallet)
                    .Where(o => o.TradingAccountId == orderToMatch.TradingAccountId &&
                                o.ShareOrderSide.SideName == "Sell" &&
                                o.UserId != orderToMatch.UserId &&
                                (o.ShareOrderStatus.OrderStatusId == statusOpen.OrderStatusId || o.ShareOrderStatus.OrderStatusId == statusPartiallyFilled.OrderStatusId) &&
                                o.LimitPrice.HasValue &&
                                (orderToMatch.ShareOrderType.TypeName == "Market" || (orderToMatch.LimitPrice.HasValue && o.LimitPrice.Value <= orderToMatch.LimitPrice.Value))
                           )
                    .OrderBy(o => o.LimitPrice)
                    .ThenBy(o => o.OrderDate)
                    .ToListAsync(cancellationToken);

                foreach (var sellOrder in counterSellOrders)
                {
                    if (remainingQtyToFillForOrderToMatch <= 0) break;
                    long sellOrderRemainingQty = sellOrder.QuantityOrdered - sellOrder.QuantityFilled;
                    if (sellOrderRemainingQty <= 0) continue;

                    decimal tradePrice = sellOrder.LimitPrice!.Value;
                    if (orderToMatch.ShareOrderType.TypeName == "Limit" && orderToMatch.LimitPrice.HasValue && tradePrice > orderToMatch.LimitPrice.Value) continue;

                    long tradeQuantity = Math.Min(remainingQtyToFillForOrderToMatch, sellOrderRemainingQty);
                    decimal totalTradeValue = tradeQuantity * tradePrice;
                    decimal buyerFee = Math.Round(totalTradeValue * feeRate, 8);
                    decimal sellerFee = Math.Round(totalTradeValue * feeRate, 8);

                    if (orderToMatch.User.Wallet == null || orderToMatch.User.Wallet.Balance < (totalTradeValue + buyerFee))
                    {
                        _logger.LogWarning("Insufficient balance for buyer UserID {BuyerUserId} for trade. BuyOrder: {BuyOrderId}", orderToMatch.UserId, orderToMatch.OrderId);
                        continue;
                    }
                    if (sellOrder.User.Wallet == null) // Wallet of seller
                    {
                        _logger.LogError("Seller UserID {SellerUserId} does not have a wallet. Cannot process trade. SellOrder: {SellOrderId}", sellOrder.UserId, sellOrder.OrderId);
                        continue;
                    }


                    _logger.LogInformation("MATCH (ORDER): BuyOrder {BuyOrderId} vs SellOrder {SellOrderId}. Trade: {TradeQty}@{TradePrice}",
                                           orderToMatch.OrderId, sellOrder.OrderId, tradeQuantity, tradePrice);

                    var trade = new ShareTrade
                    { /* ... populate ... */
                        TradingAccountId = orderToMatch.TradingAccountId,
                        BuyOrderId = orderToMatch.OrderId,
                        SellOrderId = sellOrder.OrderId,
                        BuyerUserId = orderToMatch.UserId,
                        SellerUserId = sellOrder.UserId,
                        QuantityTraded = tradeQuantity,
                        TradePrice = tradePrice,
                        BuyerFeeAmount = buyerFee,
                        SellerFeeAmount = sellerFee,
                        TradeDate = DateTime.UtcNow
                    };
                    await _unitOfWork.ShareTrades.AddAsync(trade);
                    // await _unitOfWork.CompleteAsync(cancellationToken); // To get trade.TradeId for wallet tx ref

                    orderToMatch.QuantityFilled += tradeQuantity;
                    orderToMatch.AverageFillPrice = CalculateNewAveragePrice(orderToMatch.AverageFillPrice, orderToMatch.QuantityFilled - tradeQuantity, tradePrice, tradeQuantity);
                    orderToMatch.OrderStatusId = orderToMatch.QuantityFilled == orderToMatch.QuantityOrdered ? statusFilled.OrderStatusId : statusPartiallyFilled.OrderStatusId;
                    orderToMatch.UpdatedAt = DateTime.UtcNow;
                    orderToMatch.TransactionFeeAmount = (orderToMatch.TransactionFeeAmount ?? 0) + buyerFee;
                    _unitOfWork.ShareOrders.Update(orderToMatch);

                    sellOrder.QuantityFilled += tradeQuantity;
                    sellOrder.AverageFillPrice = CalculateNewAveragePrice(sellOrder.AverageFillPrice, sellOrder.QuantityFilled - tradeQuantity, tradePrice, tradeQuantity);
                    sellOrder.OrderStatusId = sellOrder.QuantityFilled == sellOrder.QuantityOrdered ? statusFilled.OrderStatusId : statusPartiallyFilled.OrderStatusId;
                    sellOrder.UpdatedAt = DateTime.UtcNow;
                    sellOrder.TransactionFeeAmount = (sellOrder.TransactionFeeAmount ?? 0) + sellerFee;
                    _unitOfWork.ShareOrders.Update(sellOrder);

                    await _portfolioService.UpdatePortfolioOnBuyAsync(orderToMatch.UserId, orderToMatch.TradingAccountId, tradeQuantity, tradePrice, cancellationToken);
                    await _portfolioService.UpdatePortfolioOnSellAsync(sellOrder.UserId, sellOrder.TradingAccountId, tradeQuantity, tradePrice, cancellationToken);

                    var buyerWallet = orderToMatch.User.Wallet; // Đảm bảo đã Include User.Wallet
                    var now = DateTime.UtcNow;

                    // 1. Giao dịch mua chính
                    var purchaseTransaction = new WalletTransaction
                    {
                        WalletId = buyerWallet.WalletId,
                        TransactionTypeId = sharePurchaseType.TransactionTypeId, // Đảm bảo sharePurchaseType đã được lấy và không null
                        Amount = totalTradeValue, // Số tiền hàng
                        CurrencyCode = buyerWallet.CurrencyCode,
                        BalanceBefore = buyerWallet.Balance,
                        BalanceAfter = buyerWallet.Balance - (totalTradeValue + buyerFee), // Trừ cả tiền hàng và phí
                        Description = $"Purchased {tradeQuantity} shares TA_ID {orderToMatch.TradingAccountId} @ {tradePrice:F8}. Trade ID: {trade.TradeId}", // Thêm TradeId nếu có
                        ReferenceId = $"TRADE_{trade.TradeId}", // Sử dụng TradeId (cần lưu trade trước hoặc có cơ chế tạo Ref ID khác)
                        PaymentMethod = "Exchange",
                        Status = "Completed",
                        TransactionDate = now,
                        UpdatedAt = now
                    };
                    await _unitOfWork.WalletTransactions.AddAsync(purchaseTransaction);

                    // Cập nhật số dư ví người mua
                    buyerWallet.Balance -= (totalTradeValue + buyerFee);
                    buyerWallet.UpdatedAt = now;
                    _unitOfWork.Wallets.Update(buyerWallet);

                    // 2. Giao dịch phí của người mua (nếu có)
                    if (buyerFee > 0 && exchangeFeeType != null)
                    {
                        var feeTransaction = new WalletTransaction
                        {
                            WalletId = buyerWallet.WalletId,
                            TransactionTypeId = exchangeFeeType.TransactionTypeId,
                            Amount = buyerFee,
                            CurrencyCode = buyerWallet.CurrencyCode,
                            BalanceBefore = buyerWallet.Balance + buyerFee, // Balance trước khi trừ phí này (sau khi đã trừ tiền hàng)
                            BalanceAfter = buyerWallet.Balance, // Balance cuối cùng (đã được cập nhật ở trên)
                            Description = $"Fee for purchase TRADE_{trade.TradeId}",
                            ReferenceId = $"FEE_TRADE_{trade.TradeId}",
                            PaymentMethod = "ExchangeFee",
                            Status = "Completed",
                            TransactionDate = now,
                            UpdatedAt = now
                        };
                        await _unitOfWork.WalletTransactions.AddAsync(feeTransaction);
                    }


                    // Tương tự cho việc cộng tiền và trừ phí của người bán (seller):
                    var sellerWallet = sellOrder.User.Wallet; // Đảm bảo đã Include User.Wallet
                    decimal amountToCreditSeller = totalTradeValue - sellerFee;

                    var saleTransaction = new WalletTransaction
                    {
                        WalletId = sellerWallet.WalletId,
                        TransactionTypeId = shareSaleProceedsType.TransactionTypeId, // Đảm bảo shareSaleProceedsType đã được lấy và không null
                        Amount = amountToCreditSeller, // Số tiền thực nhận sau phí
                        CurrencyCode = sellerWallet.CurrencyCode,
                        BalanceBefore = sellerWallet.Balance,
                        BalanceAfter = sellerWallet.Balance + amountToCreditSeller,
                        Description = $"Sold {tradeQuantity} shares TA_ID {sellOrder.TradingAccountId} @ {tradePrice:F8}. Trade ID: {trade.TradeId}",
                        ReferenceId = $"TRADE_{trade.TradeId}",
                        PaymentMethod = "Exchange",
                        Status = "Completed",
                        TransactionDate = now,
                        UpdatedAt = now
                    };
                    await _unitOfWork.WalletTransactions.AddAsync(saleTransaction);

                    sellerWallet.Balance += amountToCreditSeller;
                    sellerWallet.UpdatedAt = now;
                    _unitOfWork.Wallets.Update(sellerWallet);
                    // Seller fee handled if system collects it

                    remainingQtyToFillForOrderToMatch -= tradeQuantity;
                    anyMatchOccurredThisRun = true;
                }
            }
            // ---- END MATCHING WITH COUNTER SELL ORDERS ----
        }
        else // orderToMatch is a Sell Order
        {
            // ---- START MATCHING SELL ORDER WITH COUNTER BUY ORDERS ----
            // Logic is symmetric to the Buy Order case, but queries for Buy orders and Admin/Fund is not a buyer here.
            var counterBuyOrders = await _unitOfWork.ShareOrders.Query()
                .Include(o => o.ShareOrderSide)
                .Include(o => o.ShareOrderType)
                .Include(o => o.ShareOrderStatus)
                .Include(o => o.User).ThenInclude(u => u.Wallet)
                .Where(o => o.TradingAccountId == orderToMatch.TradingAccountId &&
                            o.ShareOrderSide.SideName == "Buy" &&
                            o.UserId != orderToMatch.UserId &&
                            (o.ShareOrderStatus.OrderStatusId == statusOpen.OrderStatusId || o.ShareOrderStatus.OrderStatusId == statusPartiallyFilled.OrderStatusId) &&
                            o.LimitPrice.HasValue &&
                            (orderToMatch.ShareOrderType.TypeName == "Market" || (orderToMatch.LimitPrice.HasValue && o.LimitPrice.Value >= orderToMatch.LimitPrice.Value))
                       )
                .OrderByDescending(o => o.LimitPrice) // Highest buy price first
                .ThenBy(o => o.OrderDate)
                .ToListAsync(cancellationToken);

            foreach (var buyOrder in counterBuyOrders)
            {
                if (remainingQtyToFillForOrderToMatch <= 0) break;
                long buyOrderRemainingQty = buyOrder.QuantityOrdered - buyOrder.QuantityFilled;
                if (buyOrderRemainingQty <= 0) continue;

                decimal tradePrice = buyOrder.LimitPrice!.Value; // Trade price is the price of the order on the book (buy order)
                if (orderToMatch.ShareOrderType.TypeName == "Limit" && orderToMatch.LimitPrice.HasValue && tradePrice < orderToMatch.LimitPrice.Value) continue;


                long tradeQuantity = Math.Min(remainingQtyToFillForOrderToMatch, buyOrderRemainingQty);
                decimal totalTradeValue = tradeQuantity * tradePrice;
                decimal sellerFee = Math.Round(totalTradeValue * feeRate, 8);
                decimal buyerFee = Math.Round(totalTradeValue * feeRate, 8);

                if (buyOrder.User.Wallet == null || buyOrder.User.Wallet.Balance < (totalTradeValue + buyerFee))
                {
                    _logger.LogWarning("Insufficient balance for buyer UserID {BuyerUserId} for trade. BuyOrder: {BuyOrderId}. Skipping this buy order.",
                       buyOrder.UserId, buyOrder.OrderId);
                    continue; // Buyer doesn't have enough funds, skip this buy order
                }
                if (orderToMatch.User.Wallet == null)
                {
                    _logger.LogError("Seller UserID {SellerUserId} does not have a wallet. Cannot process trade. SellOrder: {SellOrderId}", orderToMatch.UserId, orderToMatch.OrderId);
                    continue; // Should not happen if user placed order
                }


                _logger.LogInformation("MATCH (ORDER): SellOrder {SellOrderId} vs BuyOrder {BuyOrderId}. Trade: {TradeQty}@{TradePrice}",
                                       orderToMatch.OrderId, buyOrder.OrderId, tradeQuantity, tradePrice);

                var trade = new ShareTrade
                { /* ... populate ... */
                    TradingAccountId = orderToMatch.TradingAccountId,
                    BuyOrderId = buyOrder.OrderId,
                    SellOrderId = orderToMatch.OrderId,
                    BuyerUserId = buyOrder.UserId,
                    SellerUserId = orderToMatch.UserId,
                    QuantityTraded = tradeQuantity,
                    TradePrice = tradePrice,
                    BuyerFeeAmount = buyerFee,
                    SellerFeeAmount = sellerFee,
                    TradeDate = DateTime.UtcNow
                };
                await _unitOfWork.ShareTrades.AddAsync(trade);
                // await _unitOfWork.CompleteAsync(cancellationToken); // To get trade.TradeId

                orderToMatch.QuantityFilled += tradeQuantity;
                orderToMatch.AverageFillPrice = CalculateNewAveragePrice(orderToMatch.AverageFillPrice, orderToMatch.QuantityFilled - tradeQuantity, tradePrice, tradeQuantity);
                orderToMatch.OrderStatusId = orderToMatch.QuantityFilled == orderToMatch.QuantityOrdered ? statusFilled.OrderStatusId : statusPartiallyFilled.OrderStatusId;
                orderToMatch.UpdatedAt = DateTime.UtcNow;
                orderToMatch.TransactionFeeAmount = (orderToMatch.TransactionFeeAmount ?? 0) + sellerFee;
                _unitOfWork.ShareOrders.Update(orderToMatch);

                buyOrder.QuantityFilled += tradeQuantity;
                buyOrder.AverageFillPrice = CalculateNewAveragePrice(buyOrder.AverageFillPrice, buyOrder.QuantityFilled - tradeQuantity, tradePrice, tradeQuantity);
                buyOrder.OrderStatusId = buyOrder.QuantityFilled == buyOrder.QuantityOrdered ? statusFilled.OrderStatusId : statusPartiallyFilled.OrderStatusId;
                buyOrder.UpdatedAt = DateTime.UtcNow;
                buyOrder.TransactionFeeAmount = (buyOrder.TransactionFeeAmount ?? 0) + buyerFee;
                _unitOfWork.ShareOrders.Update(buyOrder);

                await _portfolioService.UpdatePortfolioOnSellAsync(orderToMatch.UserId, orderToMatch.TradingAccountId, tradeQuantity, tradePrice, cancellationToken);
                await _portfolioService.UpdatePortfolioOnBuyAsync(buyOrder.UserId, buyOrder.TradingAccountId, tradeQuantity, tradePrice, cancellationToken);

                var buyerWallet = buyOrder.User.Wallet; // Đảm bảo đã Include User.Wallet
                var now = DateTime.UtcNow;

                // 1. Giao dịch mua chính
                var purchaseTransaction = new WalletTransaction
                {
                    WalletId = buyerWallet.WalletId,
                    TransactionTypeId = sharePurchaseType.TransactionTypeId, // Đảm bảo sharePurchaseType đã được lấy và không null
                    Amount = totalTradeValue, // Số tiền hàng
                    CurrencyCode = buyerWallet.CurrencyCode,
                    BalanceBefore = buyerWallet.Balance,
                    BalanceAfter = buyerWallet.Balance - (totalTradeValue + buyerFee), // Trừ cả tiền hàng và phí
                    Description = $"Purchased {tradeQuantity} shares TA_ID {orderToMatch.TradingAccountId} @ {tradePrice:F8}. Trade ID: {trade.TradeId}", // Thêm TradeId nếu có
                    ReferenceId = $"TRADE_{trade.TradeId}", // Sử dụng TradeId (cần lưu trade trước hoặc có cơ chế tạo Ref ID khác)
                    PaymentMethod = "Exchange",
                    Status = "Completed",
                    TransactionDate = now,
                    UpdatedAt = now
                };
                await _unitOfWork.WalletTransactions.AddAsync(purchaseTransaction);

                // Cập nhật số dư ví người mua
                buyerWallet.Balance -= (totalTradeValue + buyerFee);
                buyerWallet.UpdatedAt = now;
                _unitOfWork.Wallets.Update(buyerWallet);

                // 2. Giao dịch phí của người mua (nếu có)
                if (buyerFee > 0 && exchangeFeeType != null)
                {
                    var feeTransaction = new WalletTransaction
                    {
                        WalletId = buyerWallet.WalletId,
                        TransactionTypeId = exchangeFeeType.TransactionTypeId,
                        Amount = buyerFee,
                        CurrencyCode = buyerWallet.CurrencyCode,
                        BalanceBefore = buyerWallet.Balance + buyerFee, // Balance trước khi trừ phí này (sau khi đã trừ tiền hàng)
                        BalanceAfter = buyerWallet.Balance, // Balance cuối cùng (đã được cập nhật ở trên)
                        Description = $"Fee for purchase TRADE_{trade.TradeId}",
                        ReferenceId = $"FEE_TRADE_{trade.TradeId}",
                        PaymentMethod = "ExchangeFee",
                        Status = "Completed",
                        TransactionDate = now,
                        UpdatedAt = now
                    };
                    await _unitOfWork.WalletTransactions.AddAsync(feeTransaction);
                }


                // Tương tự cho việc cộng tiền và trừ phí của người bán (seller):
                var sellerWallet = orderToMatch.User.Wallet; // Đảm bảo đã Include User.Wallet
                decimal amountToCreditSeller = totalTradeValue - sellerFee;

                var saleTransaction = new WalletTransaction
                {
                    WalletId = sellerWallet.WalletId,
                    TransactionTypeId = shareSaleProceedsType.TransactionTypeId, // Đảm bảo shareSaleProceedsType đã được lấy và không null
                    Amount = amountToCreditSeller, // Số tiền thực nhận sau phí
                    CurrencyCode = sellerWallet.CurrencyCode,
                    BalanceBefore = sellerWallet.Balance,
                    BalanceAfter = sellerWallet.Balance + amountToCreditSeller,
                    Description = $"Sold {tradeQuantity} shares TA_ID {orderToMatch.TradingAccountId} @ {tradePrice:F8}. Trade ID: {trade.TradeId}",
                    ReferenceId = $"TRADE_{trade.TradeId}",
                    PaymentMethod = "Exchange",
                    Status = "Completed",
                    TransactionDate = now,
                    UpdatedAt = now
                };
                await _unitOfWork.WalletTransactions.AddAsync(saleTransaction);

                sellerWallet.Balance += amountToCreditSeller;
                sellerWallet.UpdatedAt = now;
                _unitOfWork.Wallets.Update(sellerWallet);

                remainingQtyToFillForOrderToMatch -= tradeQuantity;
                anyMatchOccurredThisRun = true;
            }
            // ---- END MATCHING SELL ORDER WITH COUNTER BUY ORDERS ----
        }
        if (anyMatchOccurredThisRun)
        {
            try
            {
                await _unitOfWork.CompleteAsync(cancellationToken);
                _logger.LogInformation("Matching run complete for OrderID {OrderId}. All changes saved.", orderId);

                var finalMatchedOrderState = await _unitOfWork.ShareOrders.Query()
                    .Include(o => o.ShareOrderStatus)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

                if (finalMatchedOrderState != null &&
                    finalMatchedOrderState.ShareOrderStatus.StatusName == nameof(ShareOrderStatusName.PartiallyFilled) &&
                    (finalMatchedOrderState.QuantityOrdered - finalMatchedOrderState.QuantityFilled > 0))
                {
                    _logger.LogInformation("Order {OrderId} is still partially filled ({QtyFilled}/{QtyOrdered}). Attempting to match remainder in a new run.",
                        orderId, finalMatchedOrderState.QuantityFilled, finalMatchedOrderState.QuantityOrdered);
                    // Important: Avoid deep recursion. Schedule a re-match or let new incoming orders trigger it.
                    // For now, we just log. A sophisticated engine might re-queue or re-trigger.
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save changes after matching run for OrderID {OrderId}. Data might be inconsistent.", orderId);
                return false; // Indicate failure to save
            }
        }
        else
        {
            _logger.LogInformation("No matches found for OrderID {OrderId} in this run.", orderId);
        }

        return anyMatchOccurredThisRun;
    }
    private decimal? CalculateNewAveragePrice(decimal? currentAveragePrice, long currentFilledQuantity, decimal newTradePrice, long newTradeQuantity)
    {
        if (newTradeQuantity <= 0) return currentAveragePrice; // Không có gì để tính

        long totalFilledQuantityBeforeNewTrade = currentFilledQuantity;
        decimal totalCostBeforeNewTrade = (currentAveragePrice ?? 0) * totalFilledQuantityBeforeNewTrade;

        decimal newTradeCost = newTradePrice * newTradeQuantity;
        long newTotalFilledQuantity = totalFilledQuantityBeforeNewTrade + newTradeQuantity;

        if (newTotalFilledQuantity == 0) return null; // Tránh chia cho 0

        return (totalCostBeforeNewTrade + newTradeCost) / newTotalFilledQuantity;
    }
    public async Task<PaginatedList<ShareOrderDto>> GetMyOrdersAsync(ClaimsPrincipal currentUser, GetMyShareOrdersQuery query, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromPrincipal(currentUser);
        if (!userId.HasValue)
        {
            _logger.LogWarning("GetMyOrdersAsync: User is not authenticated.");
            return new PaginatedList<ShareOrderDto>(new List<ShareOrderDto>(), 0, query.ValidatedPageNumber, query.ValidatedPageSize);
        }

        _logger.LogInformation("Fetching orders for UserID: {UserId} with query: {@Query}", userId.Value, query);

        var ordersQuery = _unitOfWork.ShareOrders.Query()
                            .Where(o => o.UserId == userId.Value)
                            .Include(o => o.TradingAccount)
                            .Include(o => o.ShareOrderSide)
                            .Include(o => o.ShareOrderType)
                            .Include(o => o.ShareOrderStatus)
                            .AsQueryable();

        // Áp dụng Filter
        if (query.TradingAccountId.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.TradingAccountId == query.TradingAccountId.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statusFilters = query.Status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                       .Select(s => s.ToLowerInvariant())
                                       .ToList();
            if (statusFilters.Any())
            {
                ordersQuery = ordersQuery.Where(o => statusFilters.Contains(o.ShareOrderStatus.StatusName.ToLower()));
            }
        }
        if (!string.IsNullOrWhiteSpace(query.OrderSide))
        {
            ordersQuery = ordersQuery.Where(o => o.ShareOrderSide.SideName.Equals(query.OrderSide, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(query.OrderType))
        {
            ordersQuery = ordersQuery.Where(o => o.ShareOrderType.TypeName.Equals(query.OrderType, StringComparison.OrdinalIgnoreCase));
        }
        if (query.DateFrom.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.OrderDate >= query.DateFrom.Value.Date);
        }
        if (query.DateTo.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.OrderDate < query.DateTo.Value.Date.AddDays(1));
        }

        // Áp dụng Sắp xếp
        bool isDescending = query.SortOrder?.ToLower() == "desc";
        Expression<Func<ShareOrder, object>> orderByExpression;

        switch (query.SortBy?.ToLowerInvariant())
        {
            case "tradingaccountname":
                orderByExpression = o => o.TradingAccount.AccountName;
                break;
            case "quantityordered":
                orderByExpression = o => o.QuantityOrdered;
                break;
            case "limitprice":
                orderByExpression = o => o.LimitPrice!; // Thêm ! nếu bạn chắc chắn nó không null khi sort
                break;
            case "status":
                orderByExpression = o => o.ShareOrderStatus.StatusName;
                break;
            case "orderdate":
            default:
                orderByExpression = o => o.OrderDate;
                break;
        }

        ordersQuery = isDescending
            ? ordersQuery.OrderByDescending(orderByExpression)
            : ordersQuery.OrderBy(orderByExpression);

        var paginatedOrders = await PaginatedList<ShareOrder>.CreateAsync(
            ordersQuery,
            query.ValidatedPageNumber,
            query.ValidatedPageSize,
            cancellationToken);

        var dtos = paginatedOrders.Items.Select(o => new ShareOrderDto
        {
            OrderId = o.OrderId,
            UserId = o.UserId,
            TradingAccountId = o.TradingAccountId,
            TradingAccountName = o.TradingAccount.AccountName,
            OrderSide = o.ShareOrderSide.SideName,
            OrderType = o.ShareOrderType.TypeName,
            QuantityOrdered = o.QuantityOrdered,
            QuantityFilled = o.QuantityFilled,
            LimitPrice = o.LimitPrice,
            AverageFillPrice = o.AverageFillPrice,
            OrderStatus = o.ShareOrderStatus.StatusName,
            OrderDate = o.OrderDate,
            UpdatedAt = o.UpdatedAt,
            TransactionFee = o.TransactionFeeAmount // Lấy từ TransactionFeeAmount của entity
        }).ToList();

        return new PaginatedList<ShareOrderDto>(
            dtos,
            paginatedOrders.TotalCount,
            paginatedOrders.PageNumber,
            paginatedOrders.PageSize);
    }
    public async Task<(ShareOrderDto? CancelledOrder, string? ErrorMessage)> CancelOrderAsync(long orderId, ClaimsPrincipal currentUser, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromPrincipal(currentUser);
        if (!userId.HasValue)
        {
            return (null, "User not authenticated.");
        }

        _logger.LogInformation("UserID {UserId} attempting to cancel ShareOrderID: {OrderId}", userId.Value, orderId);

        var orderToCancel = await _unitOfWork.ShareOrders.Query()
            .Include(o => o.ShareOrderStatus)
            .Include(o => o.ShareOrderSide)
            .Include(o => o.TradingAccount) // Cần để tính toán số tiền refund nếu là lệnh Market đã tạm giữ
            .Include(o => o.ShareOrderType)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

        if (orderToCancel == null)
        {
            _logger.LogWarning("CancelOrderAsync: OrderID {OrderId} not found.", orderId);
            return (null, $"Order with ID {orderId} not found.");
        }

        if (orderToCancel.UserId != userId.Value)
        {
            _logger.LogWarning("User {UserId} attempted to cancel order {OrderId} belonging to another user {OrderUserId}.", userId.Value, orderId, orderToCancel.UserId);
            return (null, "You are not authorized to cancel this order.");
        }

        var currentStatusName = orderToCancel.ShareOrderStatus.StatusName;
        bool canCancel = currentStatusName.Equals(nameof(ShareOrderStatusName.Open), StringComparison.OrdinalIgnoreCase) ||
                         currentStatusName.Equals(nameof(   ShareOrderStatusName.PartiallyFilled), StringComparison.OrdinalIgnoreCase);

        if (!canCancel)
        {
            _logger.LogInformation("Order {OrderId} cannot be cancelled. Current status: {OrderStatus}", orderId, currentStatusName);
            return (null, $"Order cannot be cancelled as it is already '{currentStatusName}'.");
        }

        var statusCancelled = await _shareOrderStatusRepository.GetByNameAsync(nameof(ShareOrderStatusName.Cancelled), cancellationToken);
        if (statusCancelled == null)
        {
            _logger.LogError("System error: 'Cancelled' order status not found in database.");
            return (null, "System error: Order status configuration missing.");
        }

        long quantityRemainingToCancel = orderToCancel.QuantityOrdered - orderToCancel.QuantityFilled;

        if (quantityRemainingToCancel > 0)
        {
            if (orderToCancel.ShareOrderSide.SideName.Equals("Buy", StringComparison.OrdinalIgnoreCase))
            {
                // Tính toán số tiền đã tạm giữ cho phần chưa khớp của lệnh mua
                // Giả định: khi đặt lệnh mua, tiền đã được tạm giữ dựa trên limitPrice (cho lệnh Limit)
                // hoặc một ước tính giá thị trường (cho lệnh Market).
                // Nếu là lệnh Market và không có giá tham chiếu rõ ràng lúc đặt lệnh, việc tính amountToRefund phức tạp.
                // Ở đây, chúng ta ưu tiên LimitPrice nếu có.
                decimal pricePerShareToRefund = (decimal)(orderToCancel.LimitPrice ?? orderToCancel.TradingAccount.CurrentSharePrice); // Cần cơ chế lấy giá tốt hơn cho Market Order
                decimal amountToRefund = quantityRemainingToCancel * pricePerShareToRefund;
                // Cộng thêm phí giao dịch đã tạm tính cho phần chưa khớp (nếu có)
                // Ví dụ: if (orderToCancel.TransactionFeeRate.HasValue) {
                //            amountToRefund += amountToRefund * orderToCancel.TransactionFeeRate.Value;
                //        }

                if (amountToRefund > 0)
                {
                    var (refundSuccess, refundMessage, _) = await _walletService.ReleaseHeldFundsForOrderAsync(
                        orderToCancel.UserId,
                        orderToCancel.OrderId,
                        amountToRefund,
                        "USD", // Giả sử TradingAccount có thông tin CurrencyCode cho giao dịch này
                                                                                    // Hoặc lấy từ Wallet của user nếu tất cả là USD
                        "Order Cancelled - Fund Release",
                        cancellationToken);

                    if (!refundSuccess)
                    {
                        _logger.LogError("Failed to release held funds for cancelled Buy Order {OrderId}: {ErrorMessage}", orderId, refundMessage);
                        return (null, $"Failed to release held funds: {refundMessage}. Please contact support.");
                    }
                    _logger.LogInformation("Funds {AmountToRefund} {Currency} released for cancelled Buy Order {OrderId}.", amountToRefund, "USD", orderId);
                }
            }
            else // Sell Order
            {
                // Giải phóng số cổ phần đã tạm giữ (phần chưa khớp)
                var (releaseSuccess, releaseMessage) = await _portfolioService.ReleaseHeldSharesAsync(
                    orderToCancel.UserId,
                    orderToCancel.TradingAccountId,
                    quantityRemainingToCancel,
                    "Order Cancelled",
                    cancellationToken);

                if (!releaseSuccess)
                {
                    _logger.LogError("Failed to release held shares for cancelled Sell Order {OrderId}: {ErrorMessage}", orderId, releaseMessage);
                    return (null, $"Failed to release held shares: {releaseMessage}. Please contact support.");
                }
                _logger.LogInformation("{QuantityToRelease} shares released for cancelled Sell Order {OrderId}.", quantityRemainingToCancel, orderId);
            }
        }

        orderToCancel.OrderStatusId = statusCancelled.OrderStatusId;
        orderToCancel.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.ShareOrders.Update(orderToCancel);
        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken); // Lưu tất cả thay đổi (order status, wallet, portfolio)
            _logger.LogInformation("Order {OrderId} cancelled successfully by UserID {UserId}.", orderId, userId.Value);

            var cancelledOrderDto = new ShareOrderDto
            {
                OrderId = orderToCancel.OrderId,
                UserId = orderToCancel.UserId,
                TradingAccountId = orderToCancel.TradingAccountId,
                TradingAccountName = orderToCancel.TradingAccount.AccountName,
                OrderSide = orderToCancel.ShareOrderSide.SideName,
                OrderType = orderToCancel.ShareOrderType.TypeName,
                QuantityOrdered = orderToCancel.QuantityOrdered,
                QuantityFilled = orderToCancel.QuantityFilled,
                LimitPrice = orderToCancel.LimitPrice,
                AverageFillPrice = orderToCancel.AverageFillPrice,
                OrderStatus = statusCancelled.StatusName,
                OrderDate = orderToCancel.OrderDate,
                UpdatedAt = orderToCancel.UpdatedAt,
                TransactionFee = orderToCancel.TransactionFeeAmount
            };
            return (cancelledOrderDto, "Order cancelled successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing changes for cancelling order {OrderId} for UserID {UserId}.", orderId, userId.Value);
            return (null, "An error occurred while finalizing order cancellation.");
        }
    }
    public async Task<(OrderBookDto? OrderBook, string? ErrorMessage)> GetOrderBookAsync(
    int tradingAccountId,
    GetOrderBookQuery query,
    CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching order book for TradingAccountID: {TradingAccountId} with Depth: {Depth}",
                               tradingAccountId, query.ValidatedDepth);

        var tradingAccount = await _unitOfWork.TradingAccounts.GetByIdAsync(tradingAccountId);
        if (tradingAccount == null)
        {
            _logger.LogWarning("GetOrderBookAsync: TradingAccountID {TradingAccountId} not found.", tradingAccountId);
            return (null, $"Trading account with ID {tradingAccountId} not found.");
        }

        // Lấy ID của các trạng thái và loại lệnh cần thiết
        var statusOpen = await _shareOrderStatusRepository.GetByNameAsync(nameof(ShareOrderStatusName.Open), cancellationToken);
        var statusPartiallyFilled = await _shareOrderStatusRepository.GetByNameAsync(nameof(ShareOrderStatusName.PartiallyFilled), cancellationToken);
        string limitTypeNameLower = "limit".ToLowerInvariant(); // Chuẩn hóa chuỗi tìm kiếm
        var orderTypeLimit = await _unitOfWork.ShareOrderTypes.Query()
                                    .FirstOrDefaultAsync(ot => ot.TypeName.ToLower() == limitTypeNameLower, cancellationToken);

        if (statusOpen == null || statusPartiallyFilled == null || orderTypeLimit == null)
        {
            _logger.LogError("System error: Required order statuses (Open, PartiallyFilled) or order type 'Limit' not found.");
            return (null, "System configuration error for order book.");
        }

        var relevantStatusIds = new List<int> { statusOpen.OrderStatusId, statusPartiallyFilled.OrderStatusId };

        // Lấy các lệnh Mua (Bids)
        var bids = await _unitOfWork.ShareOrders.Query()
            .Where(o => o.TradingAccountId == tradingAccountId &&
                        o.ShareOrderSide.SideName == "Buy" && // Lệnh Mua
                        o.OrderTypeId == orderTypeLimit.OrderTypeId && // Chỉ lệnh Limit
                        relevantStatusIds.Contains(o.OrderStatusId) && // Trạng thái Open hoặc PartiallyFilled
                        o.LimitPrice.HasValue &&
                        (o.QuantityOrdered - o.QuantityFilled) > 0) // Còn số lượng chưa khớp
            .GroupBy(o => o.LimitPrice.Value) // Nhóm theo giá
            .Select(g => new OrderBookEntryDto
            {
                Price = g.Key,
                TotalQuantity = g.Sum(o => o.QuantityOrdered - o.QuantityFilled)
            })
            .OrderByDescending(b => b.Price) // Giá mua cao nhất ở trên
            .Take(query.ValidatedDepth)
            .ToListAsync(cancellationToken);

        // Lấy các lệnh Bán (Asks)
        var asks = await _unitOfWork.ShareOrders.Query()
            .Where(o => o.TradingAccountId == tradingAccountId &&
                        o.ShareOrderSide.SideName == "Sell" && // Lệnh Bán
                        o.OrderTypeId == orderTypeLimit.OrderTypeId && // Chỉ lệnh Limit
                        relevantStatusIds.Contains(o.OrderStatusId) && // Trạng thái Open hoặc PartiallyFilled
                        o.LimitPrice.HasValue &&
                        (o.QuantityOrdered - o.QuantityFilled) > 0) // Còn số lượng chưa khớp
            .GroupBy(o => o.LimitPrice.Value) // Nhóm theo giá
            .Select(g => new OrderBookEntryDto
            {
                Price = g.Key,
                TotalQuantity = g.Sum(o => o.QuantityOrdered - o.QuantityFilled)
            })
            .OrderBy(a => a.Price) // Giá bán thấp nhất ở trên
            .Take(query.ValidatedDepth)
            .ToListAsync(cancellationToken);

        // (Tùy chọn) Lấy giá khớp lệnh gần nhất
        var lastTrade = await _unitOfWork.ShareTrades.Query()
                            .Where(st => st.TradingAccountId == tradingAccountId)
                            .OrderByDescending(st => st.TradeDate)
                            .FirstOrDefaultAsync(cancellationToken);

        var orderBookDto = new OrderBookDto
        {
            TradingAccountId = tradingAccount.TradingAccountId,
            TradingAccountName = tradingAccount.AccountName,
            LastTradePrice = lastTrade?.TradePrice,
            Timestamp = DateTime.UtcNow,
            Bids = bids,
            Asks = asks
        };

        return (orderBookDto, null);
    }
    public async Task<(MarketDataResponse? Data, string? ErrorMessage)> GetMarketDataAsync(GetMarketDataQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching market data with query: {@Query}", query);

        List<int> targetAccountIds = new List<int>();
        if (!string.IsNullOrWhiteSpace(query.TradingAccountIds))
        {
            try
            {
                targetAccountIds = query.TradingAccountIds
                                       .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                       .Select(int.Parse)
                                       .Distinct()
                                       .ToList();
                if (!targetAccountIds.Any())
                {
                    _logger.LogInformation("No valid trading account IDs provided in the filter, fetching for all active accounts.");
                }
            }
            catch (FormatException)
            {
                _logger.LogWarning("Invalid format for tradingAccountIds: {TradingAccountIds}", query.TradingAccountIds);
                return (null, "Invalid format for tradingAccountIds. Must be comma-separated integers.");
            }
        }

        IQueryable<TradingAccount> accountsQuery = _unitOfWork.TradingAccounts.Query().Where(ta => ta.IsActive);
        if (targetAccountIds.Any())
        {
            accountsQuery = accountsQuery.Where(ta => targetAccountIds.Contains(ta.TradingAccountId));
        }

        var accounts = await accountsQuery
                            .Select(ta => new { ta.TradingAccountId, ta.AccountName }) // Chỉ lấy các trường cần thiết
                            .ToListAsync(cancellationToken);

        if (!accounts.Any())
        {
            return (new MarketDataResponse { GeneratedAt = DateTime.UtcNow }, null);
        }

        var marketDataResponse = new MarketDataResponse { GeneratedAt = DateTime.UtcNow };

        var statusOpen = await _shareOrderStatusRepository.GetByNameAsync(nameof(ShareOrderStatusName.Open), cancellationToken);
        var statusPartiallyFilled = await _shareOrderStatusRepository.GetByNameAsync(nameof(ShareOrderStatusName.PartiallyFilled), cancellationToken);
        string limitTypeNameLower = "limit".ToLowerInvariant(); // Chuẩn hóa chuỗi tìm kiếm
        var orderTypeLimit = await _unitOfWork.ShareOrderTypes.Query()
                                    .FirstOrDefaultAsync(ot => ot.TypeName.ToLower() == limitTypeNameLower, cancellationToken);

        if (statusOpen == null || statusPartiallyFilled == null || orderTypeLimit == null)
        {
            _logger.LogError("System error: Required order statuses or 'Limit' order type not found for market data.");
            return (null, "System configuration error for market data.");
        }
        var relevantStatusIds = new List<int> { statusOpen.OrderStatusId, statusPartiallyFilled.OrderStatusId };

        foreach (var account in accounts)
        {
            var accountMarketData = new TradingAccountMarketDataDto
            {
                TradingAccountId = account.TradingAccountId,
                TradingAccountName = account.AccountName
            };

            // Best Bids (Top 3)
            accountMarketData.BestBids = await _unitOfWork.ShareOrders.Query()
                .Where(o => o.TradingAccountId == account.TradingAccountId &&
                            o.ShareOrderSide.SideName == "Buy" &&
                            o.OrderTypeId == orderTypeLimit.OrderTypeId &&
                            relevantStatusIds.Contains(o.OrderStatusId) &&
                            o.LimitPrice.HasValue && (o.QuantityOrdered - o.QuantityFilled) > 0)
                .GroupBy(o => o.LimitPrice.Value)
                .Select(g => new OrderBookEntryDto { Price = g.Key, TotalQuantity = g.Sum(o => o.QuantityOrdered - o.QuantityFilled) })
                .OrderByDescending(b => b.Price)
                .Take(3)
                .ToListAsync(cancellationToken);

            // Best Asks (Top 3)
            accountMarketData.BestAsks = await _unitOfWork.ShareOrders.Query()
                .Where(o => o.TradingAccountId == account.TradingAccountId &&
                            o.ShareOrderSide.SideName == "Sell" &&
                            o.OrderTypeId == orderTypeLimit.OrderTypeId &&
                            relevantStatusIds.Contains(o.OrderStatusId) &&
                            o.LimitPrice.HasValue && (o.QuantityOrdered - o.QuantityFilled) > 0)
                .GroupBy(o => o.LimitPrice.Value)
                .Select(g => new OrderBookEntryDto { Price = g.Key, TotalQuantity = g.Sum(o => o.QuantityOrdered - o.QuantityFilled) })
                .OrderBy(a => a.Price)
                .Take(3)
                .ToListAsync(cancellationToken);

            // --- THÊM LOGIC LẤY ACTIVE INITIAL OFFERINGS ---
            accountMarketData.ActiveOfferings = await _unitOfWork.InitialShareOfferings.Query()
                .Where(iso => iso.TradingAccountId == account.TradingAccountId &&
                              iso.Status == nameof(OfferingStatus.Active) && // Chỉ lấy đợt chào bán đang Active
                              (iso.SharesOffered - iso.SharesSold) > 0 && // Còn cổ phần để bán
                              (!iso.OfferingEndDate.HasValue || iso.OfferingEndDate.Value > DateTime.UtcNow)) // Chưa hết hạn
                .OrderBy(iso => iso.OfferingPricePerShare) // Sắp xếp theo giá chào bán thấp nhất
                .Take(query.ValidatedActiveOfferingsLimit) // Giới hạn số lượng offerings
                .Select(iso => new ActiveOfferingDto
                {
                    OfferingId = iso.OfferingId,
                    Price = iso.OfferingPricePerShare,
                    AvailableQuantity = iso.SharesOffered - iso.SharesSold
                })
                .ToListAsync(cancellationToken);
            // --- KẾT THÚC LOGIC LẤY ACTIVE INITIAL OFFERINGS ---

            // Recent Trades
            accountMarketData.RecentTrades = await _unitOfWork.ShareTrades.Query()
                .Where(st => st.TradingAccountId == account.TradingAccountId)
                .OrderByDescending(st => st.TradeDate)
                .Take(query.ValidatedRecentTradesLimit)
                .Select(st => new SimpleTradeDto { Price = st.TradePrice, Quantity = st.QuantityTraded, TradeTime = st.TradeDate })
                .ToListAsync(cancellationToken);

            // LastTradePrice
            if (accountMarketData.RecentTrades.Any())
            {
                accountMarketData.LastTradePrice = accountMarketData.RecentTrades.First().Price;
            }
            else
            {
                var lastTradeFromDb = await _unitOfWork.ShareTrades.Query()
                                        .Where(st => st.TradingAccountId == account.TradingAccountId)
                                        .OrderByDescending(st => st.TradeDate)
                                        .Select(st => (decimal?)st.TradePrice)
                                        .FirstOrDefaultAsync(cancellationToken);
                accountMarketData.LastTradePrice = lastTradeFromDb;
            }

            marketDataResponse.Items.Add(accountMarketData);
        }

        return (marketDataResponse, null);
    }
    public async Task<PaginatedList<MyShareTradeDto>> GetMyTradesAsync(ClaimsPrincipal currentUser, GetMyShareTradesQuery query, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromPrincipal(currentUser);
        if (!userId.HasValue)
        {
            _logger.LogWarning("GetMyTradesAsync: User is not authenticated.");
            return new PaginatedList<MyShareTradeDto>(new List<MyShareTradeDto>(), 0, query.ValidatedPageNumber, query.ValidatedPageSize);
        }

        _logger.LogInformation("Fetching trades for UserID: {UserId} with query: {@Query}", userId.Value, query);

        var tradesQuery = _unitOfWork.ShareTrades.Query()
            .Include(st => st.TradingAccount)
            // Nạp BuyOrder và SellOrder để xác định vai trò của user và phí
            .Include(st => st.BuyOrder).ThenInclude(bo => bo.ShareOrderSide)
            .Include(st => st.SellOrder).ThenInclude(so => so.ShareOrderSide) // SellOrder có thể null nếu khớp với InitialOffering
            .Where(st => st.BuyerUserId == userId.Value || st.SellerUserId == userId.Value);

        // Áp dụng Filter
        if (query.TradingAccountId.HasValue)
        {
            tradesQuery = tradesQuery.Where(st => st.TradingAccountId == query.TradingAccountId.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.OrderSide))
        {
            if (query.OrderSide.Equals("Buy", StringComparison.OrdinalIgnoreCase))
            {
                tradesQuery = tradesQuery.Where(st => st.BuyerUserId == userId.Value);
            }
            else if (query.OrderSide.Equals("Sell", StringComparison.OrdinalIgnoreCase))
            {
                tradesQuery = tradesQuery.Where(st => st.SellerUserId == userId.Value);
            }
        }
        if (query.DateFrom.HasValue)
        {
            tradesQuery = tradesQuery.Where(st => st.TradeDate >= query.DateFrom.Value.Date);
        }
        if (query.DateTo.HasValue)
        {
            tradesQuery = tradesQuery.Where(st => st.TradeDate < query.DateTo.Value.Date.AddDays(1));
        }

        // Áp dụng Sắp xếp
        bool isDescending = query.SortOrder?.ToLower() == "desc";
        Expression<Func<ShareTrade, object>> orderByExpression;

        switch (query.SortBy?.ToLowerInvariant())
        {
            case "tradingaccountname":
                orderByExpression = st => st.TradingAccount.AccountName;
                break;
            case "quantitytraded":
                orderByExpression = st => st.QuantityTraded;
                break;
            case "tradeprice":
                orderByExpression = st => st.TradePrice;
                break;
            case "tradedate":
            default:
                orderByExpression = st => st.TradeDate;
                break;
        }

        tradesQuery = isDescending
            ? tradesQuery.OrderByDescending(orderByExpression)
            : tradesQuery.OrderBy(orderByExpression);

        var paginatedTrades = await PaginatedList<ShareTrade>.CreateAsync(
            tradesQuery,
            query.ValidatedPageNumber,
            query.ValidatedPageSize,
            cancellationToken);

        var dtos = paginatedTrades.Items.Select(st =>
        {
            string userOrderSide = "Unknown";
            decimal? userFeeAmount = null;

            if (st.BuyerUserId == userId.Value)
            {
                userOrderSide = st.BuyOrder?.ShareOrderSide?.SideName ?? "Buy"; // Lấy từ lệnh mua gốc
                userFeeAmount = st.BuyerFeeAmount;
            }
            else if (st.SellerUserId == userId.Value)
            {
                // Nếu khớp với InitialOffering, SellOrder có thể null
                userOrderSide = st.SellOrder?.ShareOrderSide?.SideName ?? "Sell"; // Lấy từ lệnh bán gốc
                userFeeAmount = st.SellerFeeAmount;
            }

            return new MyShareTradeDto
            {
                TradeId = st.TradeId,
                TradingAccountId = st.TradingAccountId,
                TradingAccountName = st.TradingAccount.AccountName,
                OrderSide = userOrderSide,
                QuantityTraded = st.QuantityTraded,
                TradePrice = st.TradePrice,
                TotalValue = st.QuantityTraded * st.TradePrice,
                FeeAmount = userFeeAmount,
                TradeDate = st.TradeDate
            };
        }).ToList();

        return new PaginatedList<MyShareTradeDto>(
            dtos,
            paginatedTrades.TotalCount,
            paginatedTrades.PageNumber,
            paginatedTrades.PageSize);
    }
}