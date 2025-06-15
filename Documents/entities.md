# QuantumBands Domain Entities Structure

## Domain Overview

The QuantumBands system is a trading platform that enables share-based investment in Expert Advisor (EA) trading accounts. The domain model supports:

- **User Management** - Authentication, roles, and profiles
- **Trading Account Management** - EA-managed trading accounts with share-based investment
- **Share Trading Exchange** - Order book, trading, and settlement
- **Wallet & Transactions** - User balance management and financial transactions
- **Profit Distribution** - Automated profit sharing based on shareholdings
- **Admin Functions** - Platform administration and monitoring

## Core Entity Categories

### 1. User & Security Entities
- **User** - Platform users with authentication and profile data
- **UserRole** - Role-based access control
- **Wallet** - User financial balance management
- **WalletTransaction** - All financial transaction records

### 2. Trading Account Entities
- **TradingAccount** - EA-managed trading accounts available for investment
- **TradingAccountSnapshot** - Daily snapshots for performance tracking
- **EAClosedTrade** - Completed trades from EA systems
- **EAOpenPosition** - Current open positions from EA systems

### 3. Share Trading Entities
- **InitialShareOffering** - IPO-style offerings for new trading accounts
- **ShareOrder** - Buy/sell orders in the exchange
- **ShareTrade** - Executed trades and settlements
- **SharePortfolio** - User shareholdings per trading account

### 4. Reference & Configuration Entities
- **ShareOrderStatus** - Order status reference data
- **ShareOrderSide** - Buy/Sell side reference
- **ShareOrderType** - Market/Limit order types
- **TransactionType** - Wallet transaction categories
- **SystemSetting** - Platform configuration

### 5. Financial Distribution
- **ProfitDistributionLog** - Records of profit distributions to shareholders

## Key Entity Relationships

### User-Centric Relationships
```
User (1) ←→ (1) Wallet
User (1) ←→ (*) ShareOrder
User (1) ←→ (*) SharePortfolio  
User (1) ←→ (*) ShareTrade (as Buyer/Seller)
User (1) ←→ (*) TradingAccount (as Creator)
User (1) ←→ (*) InitialShareOffering (as Admin)
```

### Trading Account Relationships
```
TradingAccount (1) ←→ (*) ShareOrder
TradingAccount (1) ←→ (*) ShareTrade
TradingAccount (1) ←→ (*) SharePortfolio
TradingAccount (1) ←→ (*) EAClosedTrade
TradingAccount (1) ←→ (*) EAOpenPosition
TradingAccount (1) ←→ (*) InitialShareOffering
TradingAccount (1) ←→ (*) TradingAccountSnapshot
```

### Transaction Flow
```
ShareOrder (*) ←→ (*) ShareTrade (Buy/Sell Orders)
ShareTrade (1) ←→ (0..1) InitialShareOffering
WalletTransaction (0..1) ←→ (1) ProfitDistributionLog
```

## Detailed Entity Descriptions

### User
**Purpose**: Core user entity with authentication and profile management

**Key Properties**:
- `UserId` (PK) - Unique user identifier
- `Username` - Unique login name
- `Email` - Unique email address
- `PasswordHash` - Secured password storage
- `RoleId` (FK) - Links to UserRole
- `IsActive` - Account status
- `IsEmailVerified` - Email verification status
- `TwoFactorEnabled` - 2FA security setting

**Security Features**:
- Email verification tokens with expiry
- Password reset tokens with expiry
- Refresh tokens for JWT authentication
- Two-factor authentication secret keys

### TradingAccount
**Purpose**: Represents EA-managed trading accounts that users can invest in

**Key Properties**:
- `TradingAccountId` (PK) - Unique account identifier
- `AccountName` - Unique display name
- `EAName` - Expert Advisor identifier
- `BrokerPlatformIdentifier` - External broker reference
- `InitialCapital` - Starting capital amount
- `TotalSharesIssued` - Total shares available for investment
- `CurrentNetAssetValue` - Current account value
- `CurrentSharePrice` - Current price per share
- `ManagementFeeRate` - Fee percentage charged

### ShareOrder
**Purpose**: Buy/sell orders in the share exchange system

**Key Properties**:
- `OrderId` (PK) - Unique order identifier
- `UserId` (FK) - Order creator
- `TradingAccountId` (FK) - Target trading account
- `OrderSideId` (FK) - Buy or Sell
- `OrderTypeId` (FK) - Market or Limit order
- `OrderStatusId` (FK) - Current order status
- `QuantityOrdered` - Number of shares requested
- `QuantityFilled` - Number of shares executed
- `LimitPrice` - Price limit for limit orders
- `AverageFillPrice` - Execution price average

### ShareTrade
**Purpose**: Records completed share transactions

**Key Properties**:
- `TradeId` (PK) - Unique trade identifier
- `TradingAccountId` (FK) - Trading account being traded
- `BuyOrderId` (FK) - Buy order reference
- `SellOrderId` (FK) - Sell order reference (nullable)
- `InitialShareOfferingId` (FK) - IPO reference (nullable)
- `BuyerUserId` (FK) - Purchasing user
- `SellerUserId` (FK) - Selling user
- `QuantityTraded` - Number of shares traded
- `TradePrice` - Execution price per share
- `BuyerFeeAmount` - Fee charged to buyer
- `SellerFeeAmount` - Fee charged to seller

### Wallet & WalletTransaction
**Purpose**: User balance management and transaction tracking

**Wallet Properties**:
- `WalletId` (PK) - Unique wallet identifier
- `UserId` (FK) - Wallet owner (unique constraint)
- `Balance` - Current balance
- `CurrencyCode` - Currency (USD, etc.)

**WalletTransaction Properties**:
- `TransactionId` (PK) - Unique transaction identifier
- `WalletId` (FK) - Associated wallet
- `TransactionTypeId` (FK) - Transaction category
- `Amount` - Transaction amount
- `BalanceBefore` - Balance prior to transaction
- `BalanceAfter` - Balance after transaction
- `Status` - Transaction status
- `PaymentMethod` - Deposit/withdrawal method
- `ExternalTransactionId` - External system reference

### InitialShareOffering
**Purpose**: IPO-style offerings for new trading account investments

**Key Properties**:
- `OfferingId` (PK) - Unique offering identifier
- `TradingAccountId` (FK) - Account being offered
- `AdminUserId` (FK) - Administrator managing offering
- `SharesOffered` - Total shares available
- `SharesSold` - Shares sold to date
- `OfferingPricePerShare` - IPO price
- `FloorPricePerShare` - Minimum price (optional)
- `CeilingPricePerShare` - Maximum price (optional)
- `OfferingStartDate` - Start date
- `OfferingEndDate` - End date (optional)
- `Status` - Offering status (Active, Completed, Cancelled, etc.)

### SharePortfolio
**Purpose**: Tracks user shareholdings per trading account

**Key Properties**:
- `PortfolioId` (PK) - Unique portfolio identifier
- `UserId` (FK) - Portfolio owner
- `TradingAccountId` (FK) - Trading account invested in
- `Quantity` - Current shares held
- `AverageBuyPrice` - Average purchase price
- `LastUpdatedAt` - Last update timestamp

**Unique Constraint**: One portfolio record per user per trading account

### EA Trading Data

#### EAClosedTrade
**Purpose**: Historical trading data from Expert Advisor systems

**Key Properties**:
- `ClosedTradeId` (PK) - Unique closed trade identifier
- `TradingAccountId` (FK) - Associated trading account
- `EATicketId` - EA system ticket number
- `Symbol` - Trading instrument (EURUSD, GBPUSD, etc.)
- `TradeType` - Buy or Sell
- `VolumeLots` - Trade size in lots
- `OpenPrice` & `ClosePrice` - Entry and exit prices
- `OpenTime` & `CloseTime` - Trade duration
- `RealizedPandL` - Profit/Loss realized
- `Commission` & `Swap` - Trading costs

#### EAOpenPosition
**Purpose**: Current open positions from Expert Advisor systems

**Key Properties**:
- `OpenPositionId` (PK) - Unique position identifier
- `TradingAccountId` (FK) - Associated trading account
- `EATicketId` - EA system ticket number
- `Symbol` - Trading instrument
- `TradeType` - Buy or Sell
- `VolumeLots` - Position size
- `OpenPrice` - Entry price
- `CurrentMarketPrice` - Current market price
- `FloatingPandL` - Unrealized profit/loss

### Profit Distribution System

#### ProfitDistributionLog
**Purpose**: Records profit distributions to shareholders

**Key Properties**:
- `DistributionLogId` (PK) - Unique distribution record
- `TradingAccountSnapshotId` (FK) - Snapshot used for calculation
- `TradingAccountId` (FK) - Source trading account
- `UserId` (FK) - Recipient user
- `DistributionDate` - Date of distribution
- `SharesHeldAtDistribution` - Shares held at distribution time
- `ProfitPerShareDistributed` - Profit per share amount
- `TotalAmountDistributed` - Total amount to user
- `WalletTransactionId` (FK) - Associated wallet transaction

#### TradingAccountSnapshot
**Purpose**: Daily snapshots for performance tracking and profit calculations

**Key Properties**:
- `SnapshotId` (PK) - Unique snapshot identifier
- `TradingAccountId` (FK) - Associated trading account
- `SnapshotDate` - Date of snapshot
- `NetAssetValue` - Account value at snapshot
- `TotalSharesOutstanding` - Shares issued at snapshot
- `SharePriceAtSnapshot` - Calculated share price
- `DailyProfitLoss` - Daily P&L change
- `CumulativeProfitLoss` - Total P&L to date

## Entity Relationship Diagram

```
┌─────────────┐    ┌──────────────┐    ┌─────────────────┐
│    User     │────│   UserRole   │    │   SystemSetting │
│   (Core)    │    │(Reference)   │    │  (Configuration)│
└─────────────┘    └──────────────┘    └─────────────────┘
       │
       ├─────────────────────┐
       │                     │
       ▼                     ▼
┌─────────────┐         ┌─────────────┐
│   Wallet    │         │SharePortfolio│
│(Financial)  │         │(Investment) │
└─────────────┘         └─────────────┘
       │                      │
       ▼                      │
┌─────────────────┐           │
│WalletTransaction│           │
│   (Financial)   │           │
└─────────────────┘           │
       │                      │
       ▼                      │
┌──────────────────┐          │
│ProfitDistribution│          │
│    Log           │          │
│  (Financial)     │          │
└──────────────────┘          │
                               │
┌─────────────────────────────┴──┐
│         TradingAccount         │
│           (Core)               │
└────────────────────────────────┘
              │
    ┌─────────┼─────────┐
    │         │         │
    ▼         ▼         ▼
┌─────────┐ ┌──────┐ ┌─────────────┐
│ShareOrder│ │Share │ │   Initial   │
│(Exchange)│ │Trade │ │    Share    │
└─────────┘ │(Exec)│ │  Offering   │
    │       └──────┘ │   (IPO)     │
    ▼                └─────────────┘
┌─────────────┐
│ShareOrder   │
│   Status    │
│(Reference)  │
└─────────────┘

    ┌─────────────────────────────┐
    │     EA Trading Data         │
    ├─────────────────────────────┤
    │      EAClosedTrade          │
    │      EAOpenPosition         │
    │  TradingAccountSnapshot     │
    └─────────────────────────────┘
```

## Business Rules & Constraints

### User Management
- Username and email must be unique across the platform
- Email verification required for account activation
- Two-factor authentication optional but recommended
- Each user has exactly one wallet

### Trading Accounts
- Account names must be unique
- Only active accounts can participate in trading
- Management fees calculated based on defined rates
- Share price calculated as NAV / Total Shares Outstanding

### Share Trading
- Orders can only be placed for active trading accounts
- Partial fills supported through quantity tracking
- Transaction fees calculated per trade
- Both primary (IPO) and secondary market trading supported

### Financial Management
- All wallet transactions must maintain balance integrity
- Profit distributions based on shareholding percentages
- Transaction history maintained for audit purposes
- Multiple currency support with currency-specific balances

### Data Integrity
- Soft deletes preferred over hard deletes for audit trails
- All timestamps stored in UTC
- Foreign key constraints enforced at database level
- Unique constraints prevent duplicate critical data

This entity structure provides a robust foundation for a share-based trading platform with comprehensive user management, financial tracking, and automated profit distribution capabilities.