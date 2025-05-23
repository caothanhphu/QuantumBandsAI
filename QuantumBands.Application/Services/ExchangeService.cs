// QuantumBands.Application/Services/ExchangeService.cs
using QuantumBands.Application.Features.Exchange.Commands.CreateOrder;
using QuantumBands.Application.Features.Exchange.Dtos;
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Interfaces.Repositories; // Nếu có specific repositories
using QuantumBands.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt; // For Include, FirstOrDefaultAsync

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
    // ... (Các phương thức khác của ExchangeService)
}