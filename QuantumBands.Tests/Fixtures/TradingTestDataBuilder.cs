using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Exchange.Commands.CreateOrder;
using QuantumBands.Application.Features.Exchange.Dtos;
using QuantumBands.Application.Features.Exchange.Queries;
using QuantumBands.Application.Features.Portfolio.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Queries;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
using QuantumBands.Application.Features.TradingAccounts.Dtos;
using QuantumBands.Application.Features.Wallets.Dtos;

namespace QuantumBands.Tests.Fixtures;

/// <summary>
/// QuantumBands Trading Domain Test Data Builder
/// 
/// Tách từ TestDataBuilder.cs - Chứa 11 class Trading domain:
/// 1. Exchange - Trading orders và share exchange
/// 2. TradingAccounts - Quản lý tài khoản trading
/// 3. CreateTradingAccounts - Tạo tài khoản trading mới
/// 4. UpdateTradingAccounts - Cập nhật tài khoản trading
/// 5. InitialShareOfferings - Chào bán cổ phần ban đầu
/// 6. GetMyOrders - Lấy orders của user
/// 7. CancelOrder - Hủy order
/// 8. GetOrderBook - Lấy order book
/// 9. GetMarketData - Dữ liệu thị trường
/// 10. GetMyTrades - Lấy trades của user  
/// 11. GetMyPortfolio - Portfolio của user
/// 
/// Estimated ~2800 lines, covers all trading functionality including:
/// - Share order management (buy/sell orders, market/limit orders)
/// - Trading account lifecycle (creation, updates, management)
/// - Initial share offerings and IPO management
/// - Portfolio tracking and performance analytics
/// - Market data and order book functionality
/// - Trade history and transaction records
/// </summary>
public static class TradingTestDataBuilder
{
    /// <summary>
    /// SCRUM-44: Test data for Exchange PlaceOrder endpoint testing
    /// Provides comprehensive test scenarios for share trading order placement including:
    /// - Valid market and limit orders (buy/sell)
    /// - Invalid input validation scenarios
    /// - Edge cases and boundary testing
    /// - Case sensitivity and format validation
    /// - Response DTOs for different order states
    /// 
    /// Usage:
    /// - TradingTestDataBuilder.Exchange.ValidMarketBuyOrderRequest() - Standard market buy order
    /// - TradingTestDataBuilder.Exchange.ValidLimitSellOrderRequest() - Limit sell order with price
    /// - TradingTestDataBuilder.Exchange.RequestWithZeroQuantity() - Invalid validation test
    /// - TradingTestDataBuilder.Exchange.ValidMarketBuyOrderResponse() - Expected response DTO
    /// </summary>
    public static class Exchange
    {
        // Valid request scenarios for different order types
        public static CreateShareOrderRequest ValidMarketBuyOrderRequest() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 1, // Market order
            OrderSide = "Buy",
            QuantityOrdered = 100
        };

        public static CreateShareOrderRequest ValidLimitBuyOrderRequest() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 2, // Limit order
            OrderSide = "Buy",
            QuantityOrdered = 100,
            LimitPrice = 50.00m
        };

        public static CreateShareOrderRequest ValidMarketSellOrderRequest() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 1, // Market order
            OrderSide = "Sell",
            QuantityOrdered = 50
        };

        public static CreateShareOrderRequest ValidLimitSellOrderRequest() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 2, // Limit order
            OrderSide = "Sell",
            QuantityOrdered = 50,
            LimitPrice = 55.00m
        };

        // Invalid request scenarios for validation testing
        public static CreateShareOrderRequest RequestWithInvalidTradingAccountId() => new()
        {
            TradingAccountId = 0,
            OrderTypeId = 1,
            OrderSide = "Buy",
            QuantityOrdered = 100
        };

        public static CreateShareOrderRequest RequestWithNegativeTradingAccountId() => new()
        {
            TradingAccountId = -1,
            OrderTypeId = 1,
            OrderSide = "Buy",
            QuantityOrdered = 100
        };

        public static CreateShareOrderRequest RequestWithInvalidOrderTypeId() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 0,
            OrderSide = "Buy",
            QuantityOrdered = 100
        };

        public static CreateShareOrderRequest RequestWithNegativeOrderTypeId() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = -1,
            OrderSide = "Buy",
            QuantityOrdered = 100
        };

        public static CreateShareOrderRequest RequestWithInvalidOrderSide() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 1,
            OrderSide = "Invalid",
            QuantityOrdered = 100
        };

        public static CreateShareOrderRequest RequestWithEmptyOrderSide() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 1,
            OrderSide = "",
            QuantityOrdered = 100
        };

        public static CreateShareOrderRequest RequestWithNullOrderSide() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 1,
            OrderSide = null!,
            QuantityOrdered = 100
        };

        public static CreateShareOrderRequest RequestWithZeroQuantity() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 1,
            OrderSide = "Buy",
            QuantityOrdered = 0
        };

        public static CreateShareOrderRequest RequestWithNegativeQuantity() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 1,
            OrderSide = "Buy",
            QuantityOrdered = -50
        };

        public static CreateShareOrderRequest RequestWithZeroLimitPrice() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 2,
            OrderSide = "Buy",
            QuantityOrdered = 100,
            LimitPrice = 0.00m
        };

        public static CreateShareOrderRequest RequestWithNegativeLimitPrice() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 2,
            OrderSide = "Buy",
            QuantityOrdered = 100,
            LimitPrice = -10.00m
        };

        public static CreateShareOrderRequest LimitOrderWithoutLimitPrice() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 2, // Limit order but no LimitPrice
            OrderSide = "Buy",
            QuantityOrdered = 100
        };

        public static CreateShareOrderRequest MarketOrderWithUnnecessaryLimitPrice() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 1, // Market order with LimitPrice (should be ignored)
            OrderSide = "Buy",
            QuantityOrdered = 100,
            LimitPrice = 50.00m
        };

        // Edge case scenarios
        public static CreateShareOrderRequest RequestWithVeryLargeQuantity() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 1,
            OrderSide = "Buy",
            QuantityOrdered = 999999999
        };

        public static CreateShareOrderRequest RequestWithVeryHighLimitPrice() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 2,
            OrderSide = "Buy",
            QuantityOrdered = 100,
            LimitPrice = 999999.99m
        };

        public static CreateShareOrderRequest RequestWithVeryLowLimitPrice() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 2,
            OrderSide = "Buy",
            QuantityOrdered = 100,
            LimitPrice = 0.01m
        };

        // Case sensitivity test scenarios
        public static CreateShareOrderRequest RequestWithLowercaseBuy() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 1,
            OrderSide = "buy",
            QuantityOrdered = 100
        };

        public static CreateShareOrderRequest RequestWithUppercaseSell() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 1,
            OrderSide = "SELL",
            QuantityOrdered = 50
        };

        public static CreateShareOrderRequest RequestWithMixedCaseBuy() => new()
        {
            TradingAccountId = 1,
            OrderTypeId = 1,
            OrderSide = "BuY",
            QuantityOrdered = 100
        };

        // Response DTOs for different scenarios
        public static ShareOrderDto ValidMarketBuyOrderResponse() => new()
        {
            OrderId = 1001,
            TradingAccountId = 1,
            OrderTypeId = 1,
            OrderTypeName = "Market",
            OrderSide = "Buy",
            QuantityOrdered = 100,
            QuantityFilled = 0,
            LimitPrice = null,
            OrderStatusName = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        public static ShareOrderDto ValidLimitSellOrderResponse() => new()
        {
            OrderId = 1002,
            TradingAccountId = 1,
            OrderTypeId = 2,
            OrderTypeName = "Limit",
            OrderSide = "Sell",
            QuantityOrdered = 50,
            QuantityFilled = 0,
            LimitPrice = 55.00m,
            OrderStatusName = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        public static ShareOrderDto PartiallyFilledOrderResponse() => new()
        {
            OrderId = 1003,
            TradingAccountId = 1,
            OrderTypeId = 1,
            OrderTypeName = "Market",
            OrderSide = "Buy",
            QuantityOrdered = 100,
            QuantityFilled = 50,
            LimitPrice = null,
            OrderStatusName = "PartiallyFilled",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            UpdatedAt = DateTime.UtcNow
        };

        public static ShareOrderDto FilledOrderResponse() => new()
        {
            OrderId = 1004,
            TradingAccountId = 1,
            OrderTypeId = 2,
            OrderTypeName = "Limit",
            OrderSide = "Sell",
            QuantityOrdered = 25,
            QuantityFilled = 25,
            LimitPrice = 52.50m,
            OrderStatusName = "Filled",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        public static ShareOrderDto OrderWithHighFeeResponse() => new()
        {
            OrderId = 1005,
            TradingAccountId = 1,
            OrderTypeId = 1,
            OrderTypeName = "Market",
            OrderSide = "Buy",
            QuantityOrdered = 1000,
            QuantityFilled = 1000,
            LimitPrice = null,
            OrderStatusName = "Filled",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-1).AddMinutes(5)
        };

        public static ShareOrderDto OrderFromDifferentUserResponse() => new()
        {
            OrderId = 2001,
            TradingAccountId = 2,
            OrderTypeId = 1,
            OrderTypeName = "Market",
            OrderSide = "Buy",
            QuantityOrdered = 200,
            QuantityFilled = 0,
            LimitPrice = null,
            OrderStatusName = "Active",
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-1)
        };
    }

    /// <summary>
    /// Test data for TradingAccounts management endpoints
    /// Provides test scenarios for trading account operations including:
    /// - Public trading accounts listing with pagination and filtering
    /// - Trading account details with positions and history
    /// - Initial share offerings management
    /// - Snapshots and closed trades history
    /// 
    /// Usage:
    /// - TradingTestDataBuilder.TradingAccounts.ValidDefaultQuery() - Default listing query
    /// - TradingTestDataBuilder.TradingAccounts.ValidDetailResponse() - Account details response
    /// - TradingTestDataBuilder.TradingAccounts.ValidOfferingsResponse() - Share offerings data
    /// </summary>
    public static class TradingAccounts
    {
        /// <summary>
        /// Valid query for getting public trading accounts with default parameters
        /// </summary>
        public static GetPublicTradingAccountsQuery ValidDefaultQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = null,
            IsActive = false,
            SortBy = "AccountName",
            SortOrder = "asc"
        };

        /// <summary>
        /// Query for second page of results
        /// </summary>
        public static GetPublicTradingAccountsQuery SecondPageQuery() => new()
        {
            PageNumber = 2,
            PageSize = 10,
            SearchTerm = null,
            IsActive = false,
            SortBy = "AccountName",
            SortOrder = "asc"
        };

        /// <summary>
        /// Query with search term filter
        /// </summary>
        public static GetPublicTradingAccountsQuery SearchQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "Tech",
            IsActive = false,
            SortBy = "AccountName",
            SortOrder = "asc"
        };

        /// <summary>
        /// Query for active accounts only
        /// </summary>
        public static GetPublicTradingAccountsQuery ActiveOnlyQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = null,
            IsActive = true,
            SortBy = "AccountName",
            SortOrder = "asc"
        };

        /// <summary>
        /// Query with maximum page size
        /// </summary>
        public static GetPublicTradingAccountsQuery MaxPageSizeQuery() => new()
        {
            PageNumber = 1,
            PageSize = 50,
            SearchTerm = null,
            IsActive = false,
            SortBy = "CurrentBalance",
            SortOrder = "desc"
        };

        /// <summary>
        /// Valid paginated response with multiple trading accounts
        /// </summary>
        public static PaginatedList<TradingAccountDto> ValidPaginatedResponse() => new(
            new List<TradingAccountDto>
            {
                new()
                {
                    TradingAccountId = 1,
                    AccountName = "Tech Growth Fund",
                    Description = "Focused on technology sector growth stocks",
                    InitialCapital = 100000.00m,
                    CurrentBalance = 125000.50m,
                    TotalShares = 10000,
                    SharesAvailable = 2500,
                    SharePrice = 12.50m,
                    ManagementFeePercentage = 2.0m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1),
                    EaName = "TechGrowthEA_v1.0"
                },
                new()
                {
                    TradingAccountId = 2,
                    AccountName = "Value Investment Strategy",
                    Description = "Long-term value investing approach",
                    InitialCapital = 250000.00m,
                    CurrentBalance = 287500.75m,
                    TotalShares = 25000,
                    SharesAvailable = 5000,
                    SharePrice = 11.50m,
                    ManagementFeePercentage = 1.5m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-12),
                    UpdatedAt = DateTime.UtcNow.AddHours(-3),
                    EaName = "ValueInvestEA_v2.1"
                }
            },
            2, 1, 10
        );

        /// <summary>
        /// Empty response for no results
        /// </summary>
        public static PaginatedList<TradingAccountDto> EmptyResponse() => new(
            new List<TradingAccountDto>(),
            0, 1, 10
        );

        // Trading Account Details Queries and Responses

        /// <summary>
        /// Valid query for getting trading account details
        /// </summary>
        public static GetTradingAccountDetailsQuery ValidDetailsQuery() => new()
        {
            TradingAccountId = 1,
            ClosedTradesPageNumber = 1,
            ClosedTradesPageSize = 20,
            SnapshotsPageNumber = 1,
            SnapshotsPageSize = 30
        };

        /// <summary>
        /// Custom pagination query
        /// </summary>
        public static GetTradingAccountDetailsQuery CustomPaginationQuery() => new()
        {
            TradingAccountId = 2,
            ClosedTradesPageNumber = 2,
            ClosedTradesPageSize = 10,
            SnapshotsPageNumber = 1,
            SnapshotsPageSize = 15
        };

        /// <summary>
        /// Query with maximum limits
        /// </summary>
        public static GetTradingAccountDetailsQuery MaxLimitsQuery() => new()
        {
            TradingAccountId = 1,
            ClosedTradesPageNumber = 1,
            ClosedTradesPageSize = 100,
            SnapshotsPageNumber = 1,
            SnapshotsPageSize = 100
        };

        /// <summary>
        /// Valid trading account detail response
        /// </summary>
        public static TradingAccountDetailDto ValidDetailResponse() => new()
        {
            TradingAccountId = 1,
            AccountName = "Tech Growth Fund",
            Description = "Focused on technology sector growth stocks with moderate risk tolerance",
            InitialCapital = 100000.00m,
            CurrentBalance = 125000.50m,
            TotalShares = 10000,
            SharesAvailable = 2500,
            SharePrice = 12.50m,
            ManagementFeePercentage = 2.0m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            EaName = "TechGrowthEA_v1.0",
            OpenPositions = ValidOpenPositions(),
            ClosedTradesHistory = ValidClosedTradesHistory(),
            SnapshotsHistory = ValidSnapshotsHistory()
        };

        /// <summary>
        /// Valid open positions list
        /// </summary>
        public static List<EAOpenPositionDto> ValidOpenPositions() => new()
        {
            new()
            {
                PositionId = 1001,
                Symbol = "AAPL",
                Volume = 100.0,
                OpenPrice = 150.25,
                CurrentPrice = 155.50,
                Profit = 525.00,
                OpenTime = DateTime.UtcNow.AddDays(-5),
                Comment = "Tech growth position"
            },
            new()
            {
                PositionId = 1002,
                Symbol = "GOOGL",
                Volume = 50.0,
                OpenPrice = 2500.00,
                CurrentPrice = 2550.75,
                Profit = 2537.50,
                OpenTime = DateTime.UtcNow.AddDays(-10),
                Comment = "Long-term growth"
            }
        };

        /// <summary>
        /// Valid closed trades history
        /// </summary>
        public static PaginatedList<EAClosedTradeDto> ValidClosedTradesHistory() => new(
            new List<EAClosedTradeDto>
            {
                new()
                {
                    TradeId = 2001,
                    Symbol = "MSFT",
                    Volume = 75.0,
                    OpenPrice = 300.00,
                    ClosePrice = 315.50,
                    Profit = 1162.50,
                    OpenTime = DateTime.UtcNow.AddDays(-30),
                    CloseTime = DateTime.UtcNow.AddDays(-25),
                    Comment = "Successful tech trade"
                },
                new()
                {
                    TradeId = 2002,
                    Symbol = "TSLA",
                    Volume = 25.0,
                    OpenPrice = 800.00,
                    ClosePrice = 750.25,
                    Profit = -1243.75,
                    OpenTime = DateTime.UtcNow.AddDays(-45),
                    CloseTime = DateTime.UtcNow.AddDays(-40),
                    Comment = "Stop loss triggered"
                }
            },
            2, 1, 20
        );

        /// <summary>
        /// Valid snapshots history
        /// </summary>
        public static PaginatedList<TradingAccountSnapshotDto> ValidSnapshotsHistory() => new(
            new List<TradingAccountSnapshotDto>
            {
                new()
                {
                    SnapshotId = 3001,
                    Balance = 125000.50m,
                    Equity = 128537.50m,
                    Margin = 5000.00m,
                    FreeMargin = 123537.50m,
                    MarginLevel = 2570.75m,
                    SnapshotDate = DateTime.UtcNow.AddDays(-1)
                },
                new()
                {
                    SnapshotId = 3002,
                    Balance = 124500.25m,
                    Equity = 127200.00m,
                    Margin = 4800.00m,
                    FreeMargin = 122400.00m,
                    MarginLevel = 2650.00m,
                    SnapshotDate = DateTime.UtcNow.AddDays(-2)
                }
            },
            2, 1, 30
        );

        /// <summary>
        /// Empty detail response for inactive or non-existent account
        /// </summary>
        public static TradingAccountDetailDto EmptyDetailResponse() => new()
        {
            TradingAccountId = 999,
            AccountName = "Non-existent Account",
            Description = "",
            InitialCapital = 0.00m,
            CurrentBalance = 0.00m,
            TotalShares = 0,
            SharesAvailable = 0,
            SharePrice = 0.00m,
            ManagementFeePercentage = 0.0m,
            IsActive = false,
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
            EaName = "",
            OpenPositions = new List<EAOpenPositionDto>(),
            ClosedTradesHistory = new PaginatedList<EAClosedTradeDto>(new List<EAClosedTradeDto>(), 0, 1, 20),
            SnapshotsHistory = new PaginatedList<TradingAccountSnapshotDto>(new List<TradingAccountSnapshotDto>(), 0, 1, 30)
        };

        // Initial Share Offerings

        /// <summary>
        /// Valid query for getting initial share offerings
        /// </summary>
        public static GetInitialOfferingsQuery ValidOfferingsQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            Status = null,
            SortBy = "CreatedAt",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query for active offerings only
        /// </summary>
        public static GetInitialOfferingsQuery ActiveOfferingsQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            Status = "Active",
            SortBy = "EndDate",
            SortOrder = "asc"
        };

        /// <summary>
        /// Query for completed offerings
        /// </summary>
        public static GetInitialOfferingsQuery CompletedOfferingsQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            Status = "Completed",
            SortBy = "EndDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Custom offerings query
        /// </summary>
        public static GetInitialOfferingsQuery CustomOfferingsQuery() => new()
        {
            PageNumber = 2,
            PageSize = 5,
            Status = "Active",
            SortBy = "OfferingPrice",
            SortOrder = "asc"
        };

        /// <summary>
        /// Query with maximum page size
        /// </summary>
        public static GetInitialOfferingsQuery MaxPageSizeOfferingsQuery() => new()
        {
            PageNumber = 1,
            PageSize = 50,
            Status = null,
            SortBy = "SharesOffered",
            SortOrder = "desc"
        };

        /// <summary>
        /// Valid initial share offerings response
        /// </summary>
        public static PaginatedList<InitialShareOfferingDto> ValidOfferingsResponse() => new(
            new List<InitialShareOfferingDto>
            {
                new()
                {
                    OfferingId = 1,
                    TradingAccountId = 1,
                    TradingAccountName = "Tech Growth Fund",
                    SharesOffered = 2500,
                    SharesSold = 1750,
                    OfferingPrice = 12.50m,
                    FloorPrice = 10.00m,
                    CeilingPrice = 15.00m,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(30),
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow.AddDays(-35),
                    UpdatedAt = DateTime.UtcNow.AddHours(-2)
                },
                new()
                {
                    OfferingId = 2,
                    TradingAccountId = 2,
                    TradingAccountName = "Value Investment Strategy",
                    SharesOffered = 5000,
                    SharesSold = 5000,
                    OfferingPrice = 11.50m,
                    FloorPrice = 9.00m,
                    CeilingPrice = 14.00m,
                    StartDate = DateTime.UtcNow.AddDays(-90),
                    EndDate = DateTime.UtcNow.AddDays(-30),
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow.AddDays(-95),
                    UpdatedAt = DateTime.UtcNow.AddDays(-30)
                }
            },
            2, 1, 10
        );

        /// <summary>
        /// Active offerings response
        /// </summary>
        public static PaginatedList<InitialShareOfferingDto> ActiveOfferingsResponse() => new(
            new List<InitialShareOfferingDto>
            {
                new()
                {
                    OfferingId = 1,
                    TradingAccountId = 1,
                    TradingAccountName = "Tech Growth Fund",
                    SharesOffered = 2500,
                    SharesSold = 1750,
                    OfferingPrice = 12.50m,
                    FloorPrice = 10.00m,
                    CeilingPrice = 15.00m,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(30),
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow.AddDays(-35),
                    UpdatedAt = DateTime.UtcNow.AddHours(-2)
                }
            },
            1, 1, 10
        );

        /// <summary>
        /// Empty offerings response
        /// </summary>
        public static PaginatedList<InitialShareOfferingDto> EmptyOfferingsResponse() => new(
            new List<InitialShareOfferingDto>(),
            0, 1, 10
        );
    }

    // Note: Due to context limits, remaining Trading classes will be added incrementally:
    // - CreateTradingAccounts, UpdateTradingAccounts, InitialShareOfferings  
    // - GetMyOrders, CancelOrder, GetOrderBook, GetMarketData
    // - GetMyTrades, GetMyPortfolio
    // Each containing comprehensive test scenarios and documentation
} 