// QuantumBands.Application/Services/ExchangeService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Exchange.Commands.CreateOrder;
using QuantumBands.Application.Features.Exchange.Dtos;
using QuantumBands.Application.Features.Exchange.Queries;
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Interfaces.Repositories; // Nếu có specific repositories
using QuantumBands.Domain.Entities;
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
        var orderSideEntity = await _unitOfWork.ShareOrderSides.Query() // Giả sử có repo này
                                    .FirstOrDefaultAsync(s => s.SideName.Equals(request.OrderSide, StringComparison.OrdinalIgnoreCase), cancellationToken);
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
        var orderStatusOpen = await _unitOfWork.ShareOrderStatuses.Query() // Giả sử có repo này
                                   .FirstOrDefaultAsync(s => s.StatusName.Equals("Open", StringComparison.OrdinalIgnoreCase), cancellationToken);
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
            .Include(o => o.User) // Cần User để cập nhật portfolio
                .ThenInclude(u => u.Wallet) // Cần Wallet để trừ/cộng tiền
            .Include(o => o.TradingAccount)
            .FirstOrDefaultAsync(o => o.OrderId == orderId &&
                                     (o.ShareOrderStatus.StatusName == nameof(OrderStatus.Open) ||
                                      o.ShareOrderStatus.StatusName == nameof(OrderStatus.PartiallyFilled)),
                                 cancellationToken);

        if (orderToMatch == null)
        {
            _logger.LogInformation("Order {OrderId} not found or not in a matchable state.", orderId);
            return false;
        }

        _logger.LogInformation("Attempting to match OrderID: {OrderId}, Side: {OrderSide}, Type: {OrderType}, Qty: {QuantityOrdered}, Price: {LimitPrice}",
            orderToMatch.OrderId, orderToMatch.ShareOrderSide.SideName, orderToMatch.ShareOrderType.TypeName,
            orderToMatch.QuantityOrdered - orderToMatch.QuantityFilled, orderToMatch.LimitPrice);

        bool matchedOccurred = false;
        long remainingQuantityToFill = orderToMatch.QuantityOrdered - orderToMatch.QuantityFilled;

        // Lấy các ID trạng thái cần thiết
        var statusOpenId = (await _shareOrderStatusRepository.GetByNameAsync(nameof(OrderStatus.Open), cancellationToken))?.OrderStatusId;
        var statusPartiallyFilledId = (await _shareOrderStatusRepository.GetByNameAsync(nameof(OrderStatus.PartiallyFilled), cancellationToken))?.OrderStatusId;
        var statusFilledId = (await _shareOrderStatusRepository.GetByNameAsync(nameof(OrderStatus.Filled), cancellationToken))?.OrderStatusId;

        if (!statusOpenId.HasValue || !statusPartiallyFilledId.HasValue || !statusFilledId.HasValue)
        {
            _logger.LogError("Critical: Order statuses (Open, PartiallyFilled, Filled) not found in database.");
            return false; // Không thể tiếp tục nếu thiếu trạng thái cơ bản
        }


        if (orderToMatch.ShareOrderSide.SideName.Equals("Buy", StringComparison.OrdinalIgnoreCase))
        {
            // Tìm lệnh bán đối ứng (Limit Sell Orders)
            var counterSellOrders = await _unitOfWork.ShareOrders.Query()
                .Include(o => o.ShareOrderSide)
                .Include(o => o.ShareOrderType)
                .Include(o => o.ShareOrderStatus)
                .Include(o => o.User)
                    .ThenInclude(u => u.Wallet)
                .Where(o => o.TradingAccountId == orderToMatch.TradingAccountId &&
                            o.ShareOrderSide.SideName == "Sell" &&
                            o.UserId != orderToMatch.UserId && // Không khớp với chính mình
                            (o.ShareOrderStatus.StatusName == nameof(OrderStatus.Open) || o.ShareOrderStatus.StatusName == nameof(OrderStatus.PartiallyFilled)) &&
                            o.LimitPrice.HasValue && // Chỉ khớp lệnh Limit với Limit (ví dụ đơn giản)
                            (orderToMatch.LimitPrice.HasValue ? o.LimitPrice.Value <= orderToMatch.LimitPrice.Value : true) // Giá bán <= giá mua giới hạn (hoặc bất kỳ nếu lệnh mua là Market)
                       )
                .OrderBy(o => o.LimitPrice) // Giá bán thấp nhất trước
                .ThenBy(o => o.OrderDate)    // Lệnh cũ hơn trước
                .ToListAsync(cancellationToken);

            foreach (var sellOrder in counterSellOrders)
            {
                if (remainingQuantityToFill <= 0) break;

                long sellOrderRemainingQty = sellOrder.QuantityOrdered - sellOrder.QuantityFilled;
                if (sellOrderRemainingQty <= 0) continue;

                decimal tradePrice = sellOrder.LimitPrice.Value; // Giá khớp là giá của lệnh trên sổ (lệnh bán)
                if (orderToMatch.ShareOrderType.TypeName.Equals("Limit", StringComparison.OrdinalIgnoreCase) && orderToMatch.LimitPrice.HasValue && tradePrice > orderToMatch.LimitPrice.Value)
                {
                    // Lệnh mua là Limit và giá bán hiện tại cao hơn giá mua chấp nhận -> không khớp
                    continue;
                }


                long tradeQuantity = Math.Min(remainingQuantityToFill, sellOrderRemainingQty);

                // --- BẮT ĐẦU TRANSACTION (EF Core sẽ quản lý qua SaveChangesAsync của UnitOfWork) ---
                _logger.LogInformation("MATCH FOUND: Buy Order {BuyOrderId} ({BuyQty}@{BuyPrice}) vs Sell Order {SellOrderId} ({SellQty}@{SellPrice}). Trade Qty: {TradeQty} @ {TradePrice}",
                    orderToMatch.OrderId, remainingQuantityToFill, orderToMatch.LimitPrice,
                    sellOrder.OrderId, sellOrderRemainingQty, sellOrder.LimitPrice,
                    tradeQuantity, tradePrice);

                // 1. Tạo ShareTrade
                var trade = new ShareTrade
                {
                    TradingAccountId = orderToMatch.TradingAccountId,
                    BuyOrderId = orderToMatch.OrderId,
                    SellOrderId = sellOrder.OrderId,
                    BuyerUserId = orderToMatch.UserId,
                    SellerUserId = sellOrder.UserId,
                    QuantityTraded = tradeQuantity,
                    TradePrice = tradePrice,
                    TradeDate = DateTime.UtcNow
                    // BuyerFeeAmount, SellerFeeAmount sẽ tính sau
                };
                await _unitOfWork.ShareTrades.AddAsync(trade);

                // 2. Cập nhật lệnh mua
                orderToMatch.QuantityFilled += tradeQuantity;
                orderToMatch.AverageFillPrice = CalculateNewAveragePrice(orderToMatch.AverageFillPrice, orderToMatch.QuantityFilled - tradeQuantity, tradePrice, tradeQuantity);
                orderToMatch.OrderStatusId = orderToMatch.QuantityFilled == orderToMatch.QuantityOrdered ? statusFilledId.Value : statusPartiallyFilledId.Value;
                orderToMatch.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.ShareOrders.Update(orderToMatch);

                // 3. Cập nhật lệnh bán
                sellOrder.QuantityFilled += tradeQuantity;
                sellOrder.AverageFillPrice = CalculateNewAveragePrice(sellOrder.AverageFillPrice, sellOrder.QuantityFilled - tradeQuantity, tradePrice, tradeQuantity);
                sellOrder.OrderStatusId = sellOrder.QuantityFilled == sellOrder.QuantityOrdered ? statusFilledId.Value : statusPartiallyFilledId.Value;
                sellOrder.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.ShareOrders.Update(sellOrder);

                // 4. Cập nhật Portfolio
                await _portfolioService.UpdatePortfolioOnBuyAsync(orderToMatch.UserId, orderToMatch.TradingAccountId, tradeQuantity, tradePrice, cancellationToken);
                await _portfolioService.UpdatePortfolioOnSellAsync(sellOrder.UserId, sellOrder.TradingAccountId, tradeQuantity, tradePrice, cancellationToken);

                // 5. Cập nhật Wallet (Cần WalletService có các phương thức phù hợp)
                decimal totalTradeValue = tradeQuantity * tradePrice;
                // Giả sử có phương thức trong WalletService: DebitUserWallet, CreditUserWallet
                // Và các TransactionType: "SharePurchase", "ShareSaleProceeds", "ExchangeFee"
                // await _walletService.DebitUserWalletAsync(orderToMatch.UserId, totalTradeValue, "USD", $"Purchased {tradeQuantity} shares of TA_ID {orderToMatch.TradingAccountId} @ {tradePrice}", $"TRADE_{trade.TradeId}", "SharePurchase");
                // await _walletService.CreditUserWalletAsync(sellOrder.UserId, totalTradeValue, "USD", $"Sold {tradeQuantity} shares of TA_ID {sellOrder.TradingAccountId} @ {tradePrice}", $"TRADE_{trade.TradeId}", "ShareSaleProceeds");
                // TODO: Xử lý phí giao dịch

                remainingQuantityToFill -= tradeQuantity;
                matchedOccurred = true;

                // Lưu tất cả thay đổi cho trade này
                // await _unitOfWork.CompleteAsync(cancellationToken); // Hoặc lưu một lần ở cuối vòng lặp/phương thức
            }
        }
        else // orderToMatch is a Sell Order
        {
            // Tương tự, tìm lệnh mua đối ứng (Limit Buy Orders)
            // Sắp xếp theo LimitPrice giảm dần, rồi OrderDate tăng dần
            // Giá khớp là giá của lệnh mua trên sổ
            // ... logic tương tự như trên nhưng đảo ngược vai trò buyer/seller ...
            _logger.LogWarning("Matching logic for Sell Orders is not fully implemented in this snippet.");
        }


        // Nếu có khớp lệnh, lưu tất cả thay đổi
        if (matchedOccurred)
        {
            try
            {
                await _unitOfWork.CompleteAsync(cancellationToken);
                _logger.LogInformation("All matched trades and updates for OrderID {OrderId} saved successfully.", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save changes after matching OrderID {OrderId}. Potential data inconsistency.", orderId);
                // Xử lý lỗi, có thể cần cơ chế retry hoặc đưa vào queue để xử lý lại.
                // Rất quan trọng: Nếu lỗi ở đây, dữ liệu có thể không nhất quán.
                return false; // Báo hiệu khớp lệnh không hoàn tất thành công
            }
        }


        // Nếu lệnh gốc vẫn còn số lượng chưa khớp và là lệnh Limit, nó sẽ nằm lại trên order book.
        // Nếu là lệnh Market và vẫn còn số lượng chưa khớp, có thể cần xử lý thêm (ví dụ: hủy phần còn lại hoặc chuyển thành Limit)

        return matchedOccurred;
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
        bool canCancel = currentStatusName.Equals(nameof(OrderStatus.Open), StringComparison.OrdinalIgnoreCase) ||
                         currentStatusName.Equals(nameof(OrderStatus.PartiallyFilled), StringComparison.OrdinalIgnoreCase);

        if (!canCancel)
        {
            _logger.LogInformation("Order {OrderId} cannot be cancelled. Current status: {OrderStatus}", orderId, currentStatusName);
            return (null, $"Order cannot be cancelled as it is already '{currentStatusName}'.");
        }

        var statusCancelled = await _shareOrderStatusRepository.GetByNameAsync(nameof(OrderStatus.Cancelled), cancellationToken);
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
        var statusOpen = await _shareOrderStatusRepository.GetByNameAsync(nameof(OrderStatus.Open), cancellationToken);
        var statusPartiallyFilled = await _shareOrderStatusRepository.GetByNameAsync(nameof(OrderStatus.PartiallyFilled), cancellationToken);
        var orderTypeLimit = await _unitOfWork.ShareOrderTypes.Query() // Giả sử có repo này
                                    .FirstOrDefaultAsync(ot => ot.TypeName.Equals("Limit", StringComparison.OrdinalIgnoreCase), cancellationToken);

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
                if (!targetAccountIds.Any()) // Nếu sau khi parse không có ID nào hợp lệ
                {
                    _logger.LogInformation("No valid trading account IDs provided in the filter, fetching for all active accounts.");
                    // Để trống targetAccountIds sẽ lấy tất cả active bên dưới
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

        var accounts = await accountsQuery.Select(ta => new { ta.TradingAccountId, ta.AccountName }).ToListAsync(cancellationToken);

        if (!accounts.Any())
        {
            return (new MarketDataResponse { GeneratedAt = DateTime.UtcNow }, null); // Trả về rỗng nếu không có account nào khớp
        }

        var marketDataResponse = new MarketDataResponse { GeneratedAt = DateTime.UtcNow };

        // Lấy ID của các trạng thái và loại lệnh cần thiết một lần
        var statusOpen = await _shareOrderStatusRepository.GetByNameAsync(nameof(OrderStatus.Open), cancellationToken);
        var statusPartiallyFilled = await _shareOrderStatusRepository.GetByNameAsync(nameof(OrderStatus.PartiallyFilled), cancellationToken);
        var orderTypeLimit = await _unitOfWork.ShareOrderTypes.Query()
                                    .FirstOrDefaultAsync(ot => ot.TypeName.Equals("Limit", StringComparison.OrdinalIgnoreCase), cancellationToken);

        if (statusOpen == null || statusPartiallyFilled == null || orderTypeLimit == null)
        {
            _logger.LogError("System error: Required order statuses (Open, PartiallyFilled) or order type 'Limit' not found for market data.");
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

            // Lấy Best Bids (Top 3)
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

            // Lấy Best Asks (Top 3)
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

            // Lấy Recent Trades
            accountMarketData.RecentTrades = await _unitOfWork.ShareTrades.Query()
                .Where(st => st.TradingAccountId == account.TradingAccountId)
                .OrderByDescending(st => st.TradeDate)
                .Take(query.ValidatedRecentTradesLimit)
                .Select(st => new SimpleTradeDto { Price = st.TradePrice, Quantity = st.QuantityTraded, TradeTime = st.TradeDate })
                .ToListAsync(cancellationToken);

            // (Tùy chọn) Lấy LastTradePrice
            if (accountMarketData.RecentTrades.Any())
            {
                accountMarketData.LastTradePrice = accountMarketData.RecentTrades.First().Price;
            }
            else
            {
                var lastTradeFromDb = await _unitOfWork.ShareTrades.Query()
                                        .Where(st => st.TradingAccountId == account.TradingAccountId)
                                        .OrderByDescending(st => st.TradeDate)
                                        .Select(st => (decimal?)st.TradePrice) // Select nullable decimal
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