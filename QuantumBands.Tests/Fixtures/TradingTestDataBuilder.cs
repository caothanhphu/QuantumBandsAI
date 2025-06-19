using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.ExchangeMonitor.Dtos;
using QuantumBands.Application.Features.Admin.ExchangeMonitor.Queries;
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;
using QuantumBands.Application.Features.Exchange.Commands.CreateOrder;
using QuantumBands.Application.Features.Exchange.Dtos;
using QuantumBands.Application.Features.Exchange.Queries;
using QuantumBands.Application.Features.Portfolio.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Dtos;
using QuantumBands.Application.Features.TradingAccounts.Queries;
using QuantumBands.Application.Features.Wallets.Dtos;
using System;
using System.Linq;

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
            UserId = 1,
            TradingAccountId = 1,
            TradingAccountName = "Test Trading Account",
            OrderSide = "Buy",
            OrderType = "Market",
            QuantityOrdered = 100,
            QuantityFilled = 0,
            LimitPrice = null,
            AverageFillPrice = null,
            OrderStatus = "Active",
            OrderDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TransactionFee = 2.50m
        };

        public static ShareOrderDto ValidLimitSellOrderResponse() => new()
        {
            OrderId = 1002,
            UserId = 1,
            TradingAccountId = 1,
            TradingAccountName = "Test Trading Account",
            OrderSide = "Sell",
            OrderType = "Limit",
            QuantityOrdered = 50,
            QuantityFilled = 0,
            LimitPrice = 55.00m,
            AverageFillPrice = null,
            OrderStatus = "Active",
            OrderDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TransactionFee = 1.25m
        };

        public static ShareOrderDto PartiallyFilledOrderResponse() => new()
        {
            OrderId = 1003,
            UserId = 1,
            TradingAccountId = 1,
            TradingAccountName = "Test Trading Account",
            OrderSide = "Buy",
            OrderType = "Market",
            QuantityOrdered = 100,
            QuantityFilled = 50,
            LimitPrice = null,
            AverageFillPrice = 50.25m,
            OrderStatus = "PartiallyFilled",
            OrderDate = DateTime.UtcNow.AddMinutes(-5),
            UpdatedAt = DateTime.UtcNow,
            TransactionFee = 1.26m
        };

        public static ShareOrderDto FilledOrderResponse() => new()
        {
            OrderId = 1004,
            UserId = 1,
            TradingAccountId = 1,
            TradingAccountName = "Test Trading Account",
            OrderSide = "Sell",
            OrderType = "Limit",
            QuantityOrdered = 25,
            QuantityFilled = 25,
            LimitPrice = 52.50m,
            AverageFillPrice = 52.50m,
            OrderStatus = "Filled",
            OrderDate = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-2),
            TransactionFee = 0.66m
        };

        public static ShareOrderDto OrderWithHighFeeResponse() => new()
        {
            OrderId = 1005,
            UserId = 1,
            TradingAccountId = 1,
            TradingAccountName = "Test Trading Account",
            OrderType = "Market",
            OrderSide = "Buy",
            QuantityOrdered = 1000,
            QuantityFilled = 1000,
            LimitPrice = null,
            AverageFillPrice = 45.75m,
            OrderStatus = "Filled",
            OrderDate = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-1).AddMinutes(5),
            TransactionFee = 22.88m
        };

        public static ShareOrderDto OrderFromDifferentUserResponse() => new()
        {
            OrderId = 2001,
            UserId = 1,
            TradingAccountId = 2,
            TradingAccountName = "Test Trading Account",
            OrderType = "Market",
            OrderSide = "Buy",
            QuantityOrdered = 200,
            QuantityFilled = 0,
            LimitPrice = null,
            AverageFillPrice = null,
            OrderStatus = "Active",
            OrderDate = DateTime.UtcNow.AddMinutes(-1),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-1),
            TransactionFee = 5.00m
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
                    EaName = "TechGrowthEA_v1.0",
                    BrokerPlatformIdentifier = "QB-TECH-001",
                    InitialCapital = 100000.00m,
                    TotalSharesIssued = 10000,
                    CurrentNetAssetValue = 125000.50m,
                    CurrentSharePrice = 12.50m,
                    ManagementFeeRate = 0.02m,
                    IsActive = true,
                    CreatedByUserId = 1,
                    CreatorUsername = "admin",
                    CreatedAt = DateTime.UtcNow.AddMonths(-6),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new()
                {
                    TradingAccountId = 2,
                    AccountName = "Value Investment Strategy",
                    Description = "Long-term value investing approach",
                    EaName = "ValueInvestEA_v2.1",
                    BrokerPlatformIdentifier = "QB-VALUE-002",
                    InitialCapital = 250000.00m,
                    TotalSharesIssued = 25000,
                    CurrentNetAssetValue = 287500.75m,
                    CurrentSharePrice = 11.50m,
                    ManagementFeeRate = 0.015m,
                    IsActive = true,
                    CreatedByUserId = 1,
                    CreatorUsername = "admin",
                    CreatedAt = DateTime.UtcNow.AddMonths(-12),
                    UpdatedAt = DateTime.UtcNow.AddHours(-3)
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
            ClosedTradesPageNumber = 1,
            ClosedTradesPageSize = 20,
            SnapshotsPageNumber = 1,
            SnapshotsPageSize = 30,
            OpenPositionsLimit = 20
        };

        /// <summary>
        /// Custom pagination query
        /// </summary>
        public static GetTradingAccountDetailsQuery CustomPaginationQuery() => new()
        {
            ClosedTradesPageNumber = 2,
            ClosedTradesPageSize = 10,
            SnapshotsPageNumber = 1,
            SnapshotsPageSize = 15,
            OpenPositionsLimit = 30
        };

        /// <summary>
        /// Query with maximum limits
        /// </summary>
        public static GetTradingAccountDetailsQuery MaxLimitsQuery() => new()
        {
            ClosedTradesPageNumber = 1,
            ClosedTradesPageSize = 50,
            SnapshotsPageNumber = 1,
            SnapshotsPageSize = 30,
            OpenPositionsLimit = 50
        };

        /// <summary>
        /// Valid trading account detail response
        /// </summary>
        public static TradingAccountDetailDto ValidDetailResponse() => new()
        {
            TradingAccountId = 1,
            AccountName = "Tech Growth Fund",
            Description = "Focused on technology sector growth stocks with moderate risk tolerance",
            EaName = "TechGrowthEA_v1.0",
            BrokerPlatformIdentifier = "QB-TECH-001",
            InitialCapital = 100000.00m,
            TotalSharesIssued = 10000,
            CurrentNetAssetValue = 125000.50m,
            CurrentSharePrice = 12.50m,
            ManagementFeeRate = 0.02m,
            IsActive = true,
            CreatedByUserId = 1,
            CreatorUsername = "admin",
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            OpenPositions = ValidOpenPositions(),
            ClosedTradesHistory = ValidClosedTradesHistory(),
            DailySnapshotsInfo = ValidSnapshotsHistory()
        };

        /// <summary>
        /// Valid open positions list
        /// </summary>
        public static List<EAOpenPositionDto> ValidOpenPositions() => new()
        {
            new()
            {
                OpenPositionId = 1001,
                EaTicketId = "EA001",
                Symbol = "AAPL",
                TradeType = "BUY",
                VolumeLots = 1.0m,
                OpenPrice = 150.25m,
                OpenTime = DateTime.UtcNow.AddDays(-5),
                CurrentMarketPrice = 155.50m,
                Swap = 0.0m,
                Commission = 2.50m,
                FloatingPAndL = 525.00m,
                LastUpdateTime = DateTime.UtcNow.AddMinutes(-5)
            },
            new()
            {
                OpenPositionId = 1002,
                EaTicketId = "EA002",
                Symbol = "GOOGL",
                TradeType = "BUY",
                VolumeLots = 0.5m,
                OpenPrice = 2500.00m,
                OpenTime = DateTime.UtcNow.AddDays(-10),
                CurrentMarketPrice = 2550.75m,
                Swap = 0.0m,
                Commission = 5.00m,
                FloatingPAndL = 2537.50m,
                LastUpdateTime = DateTime.UtcNow.AddMinutes(-3)
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
                    ClosedTradeId = 2001,
                    EaTicketId = "EA2001",
                    Symbol = "MSFT",
                    TradeType = "BUY",
                    VolumeLots = 0.75m,
                    OpenPrice = 300.00m,
                    OpenTime = DateTime.UtcNow.AddDays(-30),
                    ClosePrice = 315.50m,
                    CloseTime = DateTime.UtcNow.AddDays(-25),
                    Swap = 0.0m,
                    Commission = 3.75m,
                    RealizedPAndL = 1162.50m,
                    RecordedAt = DateTime.UtcNow.AddDays(-25)
                },
                new()
                {
                    ClosedTradeId = 2002,
                    EaTicketId = "EA2002",
                    Symbol = "TSLA",
                    TradeType = "BUY",
                    VolumeLots = 0.25m,
                    OpenPrice = 800.00m,
                    OpenTime = DateTime.UtcNow.AddDays(-45),
                    ClosePrice = 750.25m,
                    CloseTime = DateTime.UtcNow.AddDays(-40),
                    Swap = 0.0m,
                    Commission = 2.00m,
                    RealizedPAndL = -1243.75m,
                    RecordedAt = DateTime.UtcNow.AddDays(-40)
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
                    SnapshotDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                    OpeningNAV = 124000.00m,
                    RealizedPAndLForTheDay = 800.50m,
                    UnrealizedPAndLForTheDay = 200.00m,
                    ManagementFeeDeducted = 25.00m,
                    ProfitDistributed = 975.50m,
                    ClosingNAV = 125000.50m,
                    ClosingSharePrice = 12.50m,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new()
                {
                    SnapshotId = 3002,
                    SnapshotDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
                    OpeningNAV = 123500.25m,
                    RealizedPAndLForTheDay = 400.75m,
                    UnrealizedPAndLForTheDay = 150.00m,
                    ManagementFeeDeducted = 51.00m,
                    ProfitDistributed = 499.75m,
                    ClosingNAV = 124000.00m,
                    ClosingSharePrice = 12.40m,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
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
            EaName = "",
            BrokerPlatformIdentifier = "",
            InitialCapital = 0.00m,
            TotalSharesIssued = 0,
            CurrentNetAssetValue = 0.00m,
            CurrentSharePrice = 0.00m,
            ManagementFeeRate = 0.0m,
            IsActive = false,
            CreatedByUserId = 0,
            CreatorUsername = "Unknown",
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
            OpenPositions = new List<EAOpenPositionDto>(),
            ClosedTradesHistory = new PaginatedList<EAClosedTradeDto>(new List<EAClosedTradeDto>(), 0, 1, 20),
            DailySnapshotsInfo = new PaginatedList<TradingAccountSnapshotDto>(new List<TradingAccountSnapshotDto>(), 0, 1, 30)
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
                    AdminUserId = 1,
                    AdminUsername = "admin",
                    SharesOffered = 2500,
                    SharesSold = 1750,
                    OfferingPricePerShare = 12.50m,
                    FloorPricePerShare = 10.00m,
                    CeilingPricePerShare = 15.00m,
                    OfferingStartDate = DateTime.UtcNow.AddDays(-30),
                    OfferingEndDate = DateTime.UtcNow.AddDays(30),
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow.AddDays(-35),
                    UpdatedAt = DateTime.UtcNow.AddHours(-2)
                },
                new()
                {
                    OfferingId = 2,
                    TradingAccountId = 2,
                    AdminUserId = 1,
                    AdminUsername = "admin",
                    SharesOffered = 5000,
                    SharesSold = 5000,
                    OfferingPricePerShare = 11.50m,
                    FloorPricePerShare = 9.00m,
                    CeilingPricePerShare = 14.00m,
                    OfferingStartDate = DateTime.UtcNow.AddDays(-90),
                    OfferingEndDate = DateTime.UtcNow.AddDays(-30),
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
                    AdminUserId = 1,
                    AdminUsername = "admin",
                    SharesOffered = 2500,
                    SharesSold = 1750,
                    OfferingPricePerShare = 12.50m,
                    FloorPricePerShare = 10.00m,
                    CeilingPricePerShare = 15.00m,
                    OfferingStartDate = DateTime.UtcNow.AddDays(-30),
                    OfferingEndDate = DateTime.UtcNow.AddDays(30),
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

    /// <summary>
    /// SCRUM-62: Test data for GET /portfolio/me endpoint testing
    /// Provides comprehensive test scenarios for user portfolio management including:
    /// - Valid portfolio responses with multiple positions
    /// - Financial calculations and PnL accuracy
    /// - Edge cases (empty portfolios, zero quantities)
    /// - Large portfolios and precise decimal handling
    /// - Multi-account portfolio scenarios
    /// 
    /// Usage:
    /// - TradingTestDataBuilder.GetMyPortfolio.ValidPortfolioResponse() - Standard portfolio data
    /// - TradingTestDataBuilder.GetMyPortfolio.ProfitablePortfolioResponse() - Profitable positions
    /// - TradingTestDataBuilder.GetMyPortfolio.EmptyPortfolioResponse() - Empty portfolio
    /// </summary>
    public static class GetMyPortfolio
    {
        /// <summary>
        /// Valid user portfolio with multiple positions
        /// </summary>
        public static List<SharePortfolioItemDto> ValidPortfolioResponse() => new()
        {
            new()
            {
                PortfolioId = 1,
                TradingAccountId = 1,
                TradingAccountName = "Tech Solutions Inc.",
                Quantity = 150,
                AverageBuyPrice = 25.50m,
                CurrentSharePrice = 28.75m,
                CurrentValue = 4312.50m,
                UnrealizedPAndL = 487.50m,
                LastUpdatedAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new()
            {
                PortfolioId = 2,
                TradingAccountId = 2,
                TradingAccountName = "Green Energy Corp.",
                Quantity = 200,
                AverageBuyPrice = 18.90m,
                CurrentSharePrice = 19.25m,
                CurrentValue = 3850.00m,
                UnrealizedPAndL = 70.00m,
                LastUpdatedAt = DateTime.UtcNow.AddMinutes(-3)
            }
        };

        /// <summary>
        /// Portfolio with profitable positions
        /// </summary>
        public static List<SharePortfolioItemDto> ProfitablePortfolioResponse() => new()
        {
            new()
            {
                PortfolioId = 3,
                TradingAccountId = 1,
                TradingAccountName = "Tech Solutions Inc.",
                Quantity = 100,
                AverageBuyPrice = 20.00m,
                CurrentSharePrice = 30.00m,
                CurrentValue = 3000.00m,
                UnrealizedPAndL = 1000.00m,
                LastUpdatedAt = DateTime.UtcNow.AddMinutes(-2)
            }
        };

        /// <summary>
        /// Portfolio with losing positions
        /// </summary>
        public static List<SharePortfolioItemDto> LosingPortfolioResponse() => new()
        {
            new()
            {
                PortfolioId = 4,
                TradingAccountId = 2,
                TradingAccountName = "Green Energy Corp.",
                Quantity = 80,
                AverageBuyPrice = 35.00m,
                CurrentSharePrice = 28.50m,
                CurrentValue = 2280.00m,
                UnrealizedPAndL = -520.00m,
                LastUpdatedAt = DateTime.UtcNow.AddMinutes(-1)
            }
        };

        /// <summary>
        /// Empty portfolio response
        /// </summary>
        public static List<SharePortfolioItemDto> EmptyPortfolioResponse() => new();

        /// <summary>
        /// Portfolio with zero quantity positions (edge case)
        /// </summary>
        public static List<SharePortfolioItemDto> ZeroQuantityPortfolioResponse() => new()
        {
            new()
            {
                PortfolioId = 5,
                TradingAccountId = 1,
                TradingAccountName = "Tech Solutions Inc.",
                Quantity = 0,
                AverageBuyPrice = 25.00m,
                CurrentSharePrice = 27.00m,
                CurrentValue = 0.00m,
                UnrealizedPAndL = 0.00m,
                LastUpdatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        /// <summary>
        /// Large portfolio with high values
        /// </summary>
        public static List<SharePortfolioItemDto> LargePortfolioResponse() => new()
        {
            new()
            {
                PortfolioId = 6,
                TradingAccountId = 3,
                TradingAccountName = "Financial Holdings Ltd.",
                Quantity = 5000,
                AverageBuyPrice = 100.00m,
                CurrentSharePrice = 125.50m,
                CurrentValue = 627500.00m,
                UnrealizedPAndL = 127500.00m,
                LastUpdatedAt = DateTime.UtcNow.AddMinutes(-10)
            }
        };

        /// <summary>
        /// Portfolio with multiple trading accounts
        /// </summary>
        public static List<SharePortfolioItemDto> MultiAccountPortfolioResponse() => new()
        {
            new()
            {
                PortfolioId = 7,
                TradingAccountId = 1,
                TradingAccountName = "Tech Solutions Inc.",
                Quantity = 300,
                AverageBuyPrice = 22.50m,
                CurrentSharePrice = 24.75m,
                CurrentValue = 7425.00m,
                UnrealizedPAndL = 675.00m,
                LastUpdatedAt = DateTime.UtcNow.AddMinutes(-7)
            },
            new()
            {
                PortfolioId = 8,
                TradingAccountId = 2,
                TradingAccountName = "Green Energy Corp.",
                Quantity = 500,
                AverageBuyPrice = 15.80m,
                CurrentSharePrice = 16.90m,
                CurrentValue = 8450.00m,
                UnrealizedPAndL = 550.00m,
                LastUpdatedAt = DateTime.UtcNow.AddMinutes(-4)
            },
            new()
            {
                PortfolioId = 9,
                TradingAccountId = 3,
                TradingAccountName = "Financial Holdings Ltd.",
                Quantity = 200,
                AverageBuyPrice = 45.25m,
                CurrentSharePrice = 42.10m,
                CurrentValue = 8420.00m,
                UnrealizedPAndL = -630.00m,
                LastUpdatedAt = DateTime.UtcNow.AddMinutes(-6)
            }
        };

        /// <summary>
        /// Portfolio with precise decimal calculations
        /// </summary>
        public static List<SharePortfolioItemDto> PreciseDecimalPortfolioResponse() => new()
        {
            new()
            {
                PortfolioId = 10,
                TradingAccountId = 1,
                TradingAccountName = "Tech Solutions Inc.",
                Quantity = 123,
                AverageBuyPrice = 12.3456m,
                CurrentSharePrice = 13.7891m,
                CurrentValue = 1696.0593m,
                UnrealizedPAndL = 177.7548m,
                LastUpdatedAt = DateTime.UtcNow.AddMinutes(-8)
            }
        };

        /// <summary>
        /// Custom portfolio item for testing
        /// </summary>
        public static SharePortfolioItemDto CustomPortfolioItem(
            int portfolioId,
            int tradingAccountId,
            string tradingAccountName,
            long quantity,
            decimal averageBuyPrice,
            decimal currentSharePrice) => new()
        {
            PortfolioId = portfolioId,
            TradingAccountId = tradingAccountId,
            TradingAccountName = tradingAccountName,
            Quantity = quantity,
            AverageBuyPrice = averageBuyPrice,
            CurrentSharePrice = currentSharePrice,
            CurrentValue = quantity * currentSharePrice,
            UnrealizedPAndL = (quantity * currentSharePrice) - (quantity * averageBuyPrice),
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    // SCRUM-82: Test data for GET /admin/exchange/orders endpoint testing
    /// <summary>
    /// AdminExchangeMonitor - Test data builder for Admin Exchange Monitoring functionality
    /// 
    /// This class provides comprehensive test data for admin exchange monitoring including:
    /// - All orders retrieval with pagination, filtering, and sorting
    /// - Authorization scenarios for admin access
    /// - Complex filtering combinations (user, trading account, status, order side, date range)
    /// - Various sorting options and edge cases
    /// 
    /// Usage:
    /// - TradingTestDataBuilder.AdminExchangeMonitor.ValidQuery() - Basic query
    /// - TradingTestDataBuilder.AdminExchangeMonitor.AllOrdersList() - Sample orders
    /// - TradingTestDataBuilder.AdminExchangeMonitor.FilteredOrdersList() - Filtered results
    /// </summary>
    public static class AdminExchangeMonitor
    {
        /// <summary>
        /// Valid GetAdminAllOrdersQuery for basic testing
        /// </summary>
        public static GetAdminAllOrdersQuery ValidQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "OrderDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with trading account filter
        /// </summary>
        public static GetAdminAllOrdersQuery QueryWithTradingAccountFilter() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            TradingAccountId = 1,
            SortBy = "OrderDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with user filter
        /// </summary>
        public static GetAdminAllOrdersQuery QueryWithUserFilter() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            UserId = 123,
            SortBy = "OrderDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with status filter
        /// </summary>
        public static GetAdminAllOrdersQuery QueryWithStatusFilter() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            Status = "Filled",
            SortBy = "OrderDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with order side filter
        /// </summary>
        public static GetAdminAllOrdersQuery QueryWithOrderSideFilter() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            OrderSide = "Buy",
            SortBy = "OrderDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with date range filter
        /// </summary>
        public static GetAdminAllOrdersQuery QueryWithDateRangeFilter() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            DateFrom = DateTime.UtcNow.AddDays(-30),
            DateTo = DateTime.UtcNow,
            SortBy = "OrderDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with combined filters
        /// </summary>
        public static GetAdminAllOrdersQuery QueryWithCombinedFilters() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            UserId = 123,
            Status = "Filled",
            OrderSide = "Buy",
            DateFrom = DateTime.UtcNow.AddDays(-7),
            DateTo = DateTime.UtcNow,
            SortBy = "OrderDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with custom sorting
        /// </summary>
        public static GetAdminAllOrdersQuery QueryWithCustomSorting() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "Username",
            SortOrder = "asc"
        };

        /// <summary>
        /// Query with large page size
        /// </summary>
        public static GetAdminAllOrdersQuery QueryWithLargePageSize() => new()
        {
            PageNumber = 1,
            PageSize = 50,
            SortBy = "OrderDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Sample list of admin share orders for testing
        /// </summary>
        public static List<AdminShareOrderViewDto> AllOrdersList() => new()
        {
            new()
            {
                OrderId = 1001,
                UserId = 123,
                Username = "trader123",
                UserEmail = "trader123@example.com",
                TradingAccountId = 1,
                TradingAccountName = "Main Trading Account",
                OrderSide = "Buy",
                OrderType = "Market",
                QuantityOrdered = 100,
                QuantityFilled = 100,
                LimitPrice = null,
                AverageFillPrice = 50.25m,
                OrderStatus = "Filled",
                OrderDate = DateTime.UtcNow.AddHours(-2),
                UpdatedAt = DateTime.UtcNow.AddHours(-1),
                TransactionFee = 2.51m
            },
            new()
            {
                OrderId = 1002,
                UserId = 456,
                Username = "investor456",
                UserEmail = "investor456@example.com",
                TradingAccountId = 2,
                TradingAccountName = "Investment Account",
                OrderSide = "Sell",
                OrderType = "Limit",
                QuantityOrdered = 200,
                QuantityFilled = 150,
                LimitPrice = 55.00m,
                AverageFillPrice = 54.75m,
                OrderStatus = "PartiallyFilled",
                OrderDate = DateTime.UtcNow.AddHours(-5),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-30),
                TransactionFee = 8.21m
            },
            new()
            {
                OrderId = 1003,
                UserId = 789,
                Username = "daytrader789",
                UserEmail = "daytrader789@example.com",
                TradingAccountId = 3,
                TradingAccountName = "Day Trading Account",
                OrderSide = "Buy",
                OrderType = "Limit",
                QuantityOrdered = 500,
                QuantityFilled = 0,
                LimitPrice = 48.50m,
                AverageFillPrice = null,
                OrderStatus = "Pending",
                OrderDate = DateTime.UtcNow.AddMinutes(-15),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-15),
                TransactionFee = null
            }
        };

        /// <summary>
        /// Filtered orders list for specific user
        /// </summary>
        public static List<AdminShareOrderViewDto> FilteredOrdersForUser(int userId) => 
            AllOrdersList().Where(o => o.UserId == userId).ToList();

        /// <summary>
        /// Filtered orders list for specific trading account
        /// </summary>
        public static List<AdminShareOrderViewDto> FilteredOrdersForTradingAccount(int tradingAccountId) => 
            AllOrdersList().Where(o => o.TradingAccountId == tradingAccountId).ToList();

        /// <summary>
        /// Filtered orders list for specific status
        /// </summary>
        public static List<AdminShareOrderViewDto> FilteredOrdersByStatus(string status) => 
            AllOrdersList().Where(o => o.OrderStatus == status).ToList();

        /// <summary>
        /// Filtered orders list for specific order side
        /// </summary>
        public static List<AdminShareOrderViewDto> FilteredOrdersByOrderSide(string orderSide) => 
            AllOrdersList().Where(o => o.OrderSide == orderSide).ToList();

        /// <summary>
        /// Empty orders list for testing no results scenario
        /// </summary>
        public static List<AdminShareOrderViewDto> EmptyOrdersList() => new();
    }

    // Note: Due to context limits, remaining Trading classes will be added incrementally:
    // - CreateTradingAccounts, UpdateTradingAccounts, InitialShareOfferings  
    // - GetMyOrders, CancelOrder, GetOrderBook, GetMarketData, GetMyTrades
    // Each containing comprehensive test scenarios and documentation
} 