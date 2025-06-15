# QuantumBands Application Layer Architecture

## Overview

The QuantumBands Application layer implements **Clean Architecture** principles with a **CQRS-inspired** pattern to provide a robust, maintainable, and scalable business logic tier. The layer serves as the orchestration hub between the API controllers and the Domain/Infrastructure layers, encapsulating all business rules and workflows.

## Architectural Principles

### Clean Architecture Implementation
- **Dependency Inversion**: Application layer depends on abstractions, not concrete implementations
- **Single Responsibility**: Each service and handler has a single, well-defined purpose
- **Separation of Concerns**: Clear boundaries between different aspects of business logic
- **Interface Segregation**: Small, focused interfaces for better testability and maintainability

### CQRS Pattern Application
- **Command/Query Separation**: Distinct handling of read and write operations
- **Feature-Based Organization**: Business capabilities organized by domain feature
- **Validation-First Approach**: Comprehensive input validation using FluentValidation
- **DTO Transformation**: Clear data contracts between layers

## Project Structure

```
QuantumBands.Application/
├── Common/
│   └── Models/
│       ├── ChartDataPoint.cs          // Chart data representation
│       └── PaginatedList.cs           // Pagination wrapper
├── Features/                          // Domain-organized features
│   ├── Admin/                         // Administrative capabilities
│   ├── Authentication/                // User authentication/authorization
│   ├── EAIntegration/                // Expert Advisor integration
│   ├── Exchange/                     // Trading exchange operations
│   ├── Portfolio/                    // Portfolio management
│   ├── Roles/                        // Role management
│   ├── TradingAccounts/              // Trading account management
│   ├── Users/                        // User profile management
│   └── Wallets/                      // Financial wallet operations
├── Interfaces/                       // Service contracts
│   ├── [Service Interfaces]
│   └── Repositories/                 // Repository contracts
└── Services/                         // Business logic implementations
    └── [Service Implementations]
```

## Feature-Based Architecture

### Feature Organization Pattern
Each feature follows a consistent structure:
```
Feature/
├── Commands/                         // Write operations
│   ├── [OperationName]/
│   │   ├── [Operation]Request.cs
│   │   └── [Operation]RequestValidator.cs
├── Queries/                          // Read operations
│   ├── [QueryName]Query.cs
│   └── [QueryName]QueryValidator.cs
├── Dtos/                            // Data transfer objects
│   └── [Entity]Dto.cs
└── Handlers/ (when applicable)       // Command/query handlers
```

## Core Features Analysis

### 1. Authentication Feature
**Business Capabilities:**
- User registration with email verification workflow
- JWT-based authentication with refresh token support
- Password management (forgot/reset functionality)
- Security token management with expiration handling

**Architecture Patterns:**
- **Command Pattern**: All authentication operations are commands
- **Email Workflow**: Asynchronous email verification process
- **Token Security**: Secure token generation and validation
- **Domain Events**: User registration and verification events

**Key Components:**
```csharp
// Commands
RegisterUserCommand              // User registration
LoginRequest                     // User authentication  
RefreshTokenRequest             // Token refresh
ForgotPasswordRequest           // Password reset initiation
ResetPasswordRequest            // Password reset completion
VerifyEmailRequest              // Email verification

// DTOs
UserDto                         // Basic user information
UserProfileDto                  // Extended user profile
LoginResponse                   // Authentication response
JwtSettings                     // JWT configuration
```

**Validation Rules:**
- Username: 3-50 characters, alphanumeric with underscores
- Email: RFC-compliant email format, maximum 255 characters
- Password: 8-100 characters with complexity requirements
- Security tokens: Time-bound with secure generation

### 2. Wallet Management Feature
**Business Capabilities:**
- Multi-currency wallet support (currently USD-focused)
- Bank deposit workflow with admin confirmation
- Withdrawal requests with approval process
- Internal transfers between platform users
- Comprehensive transaction audit trail

**Architecture Patterns:**
- **State Machine**: Deposit/withdrawal status management
- **Double-Entry Bookkeeping**: Balance integrity through transaction pairs
- **Approval Workflow**: Admin approval for financial operations
- **Atomic Operations**: Transaction consistency across wallet operations

**Key Components:**
```csharp
// Commands
InitiateBankDepositRequest      // Start deposit process
ConfirmBankDepositRequest       // Admin deposit confirmation
CreateWithdrawalRequest         // Withdrawal request
ExecuteInternalTransferRequest  // Internal money transfer
AdminDirectDepositRequest       // Admin-initiated deposits

// Queries  
GetWalletTransactionsQuery      // Transaction history
GetAdminPendingBankDepositsQuery    // Admin pending deposits
GetAdminPendingWithdrawalsQuery     // Admin pending withdrawals

// DTOs
WalletDto                       // Wallet information
WalletTransactionDto            // Transaction details
WithdrawalRequestDto            // Withdrawal request data
BankDepositInfoResponse         // Deposit instructions
```

**Business Rules:**
- All amounts must be positive decimals
- Currency consistency across related transactions
- Reference code generation for external tracking
- Balance validation before withdrawals
- Admin approval required for deposits/withdrawals

### 3. Exchange Trading Feature
**Business Capabilities:**
- Share order placement (buy/sell, market/limit)
- Advanced order matching engine
- Real-time order book generation
- Market data aggregation across trading accounts
- Trade execution with automatic portfolio updates

**Architecture Patterns:**
- **Matching Engine**: Sophisticated order matching algorithm
- **Event Sourcing**: Trade execution event capture
- **Portfolio Integration**: Automatic share portfolio updates
- **Market Data Aggregation**: Real-time market information compilation

**Key Components:**
```csharp
// Commands
CreateShareOrderRequest         // Place share orders

// Queries
GetMarketDataQuery             // Market data compilation
GetMyShareOrdersQuery          // User's orders
GetMyShareTradesQuery          // User's executed trades
GetOrderBookQuery              // Order book information

// DTOs
ShareOrderDto                  // Order information
MarketDataResponse            // Aggregated market data
OrderBookDto                  // Order book structure
MyShareTradeDto               // User trade information
TradingAccountMarketDataDto   // Per-account market data
```

**Complex Business Logic:**
- **Order Matching Algorithm**:
  1. Buy orders vs Initial Share Offerings (primary market)
  2. Buy orders vs Sell orders (secondary market)
  3. Price-time priority matching
  4. Partial fill support
- **Fee Calculation**: Dynamic fee calculation based on trade value
- **Portfolio Synchronization**: Automatic portfolio updates on trade execution

### 4. Trading Account Management Feature
**Business Capabilities:**
- Trading account (fund) creation and management
- Initial Share Offering (IPO) lifecycle management
- Fund performance tracking and NAV calculations
- Admin-controlled fund operations

**Architecture Patterns:**
- **Factory Pattern**: Trading account creation with proper initialization
- **State Machine**: IPO status management (Active → Completed/Cancelled)
- **Financial Calculations**: NAV and share price computations
- **Admin Authorization**: Role-based access to fund operations

**Key Components:**
```csharp
// Admin Commands
CreateTradingAccountRequest           // Fund creation
UpdateTradingAccountRequest          // Fund updates
CreateInitialShareOfferingRequest   // IPO creation
UpdateInitialShareOfferingRequest   // IPO modifications
CancelInitialShareOfferingRequest   // IPO cancellation

// Queries
GetPublicTradingAccountsQuery       // Public fund listings
GetTradingAccountDetailsQuery       // Detailed fund information
GetInitialOfferingsQuery            // Available IPOs

// DTOs
TradingAccountDto                   // Fund information
TradingAccountDetailDto             // Comprehensive fund details
InitialShareOfferingDto             // IPO information
```

**Financial Calculations:**
- **Net Asset Value (NAV)**: Current fund value calculation
- **Share Price**: NAV ÷ Total Shares Outstanding
- **Performance Metrics**: Return calculations and historical tracking

### 5. EA Integration Feature
**Business Capabilities:**
- Real-time trading data ingestion from Expert Advisors
- Open position synchronization
- Closed trade data integration
- Account equity and balance tracking

**Architecture Patterns:**
- **External System Integration**: Secure API key-based authentication
- **Data Synchronization**: Real-time position and trade data sync
- **Batch Processing**: Efficient handling of bulk trade data
- **Data Reconciliation**: Handling of missing or updated positions

**Key Components:**
```csharp
// Commands
PushLiveDataRequest            // Live trading data
PushClosedTradesRequest        // Historical trade data

// DTOs
EAOpenPositionDtoFromEA        // Open position from EA
EAClosedTradeDtoFromEA         // Closed trade from EA
LiveDataResponse               // Live data response
```

**Integration Patterns:**
- **API Key Authentication**: Secure external system access
- **Bulk Operations**: Efficient database operations for large datasets
- **Data Validation**: Comprehensive validation of external data
- **Error Handling**: Robust handling of integration failures

### 6. Administration Feature
**Business Capabilities:**
- Administrative dashboard with key metrics
- User management (status, roles)
- Financial transaction approvals
- Trading account and IPO management
- Exchange monitoring and oversight

**Architecture Patterns:**
- **Dashboard Aggregation**: Efficient metric calculation and caching
- **Approval Workflows**: Multi-step approval processes
- **Administrative Override**: Admin capabilities with proper authorization
- **Monitoring and Reporting**: Comprehensive platform oversight

**Key Components:**
```csharp
// Dashboard
AdminDashboardSummaryDto       // Dashboard metrics

// User Management
GetAdminUsersQuery             // User listings
UpdateUserStatusRequest        // User activation/deactivation
UpdateUserRoleRequest          // Role assignments

// Financial Operations
ApproveWithdrawalRequest       // Withdrawal approvals
RejectWithdrawalRequest        // Withdrawal rejections
AdminDirectDepositRequest      // Direct deposits

// Exchange Monitoring
GetAdminAllOrdersQuery         // All platform orders
GetAdminAllTradesQuery         // All platform trades
```

## Service Layer Architecture

### Service Interfaces & Implementations

#### Core Business Services
```csharp
// Authentication & User Management
IAuthService → AuthService
IUserService → UserService

// Financial Operations  
IWalletService → WalletService

// Trading Operations
IExchangeService → ExchangeService
ITradingAccountService → TradingAccountService
ISharePortfolioService → SharePortfolioService

// External Integration
IEAIntegrationService → EAIntegrationService

// Administration
IAdminDashboardService → AdminDashboardService

// Infrastructure Services
IEmailService → EmailService (Infrastructure)
IJwtTokenGenerator → JwtTokenGenerator (Infrastructure)
ICachingService → InMemoryCachingService (Infrastructure)
```

### Service Orchestration Patterns

#### Authentication Service Pattern
```csharp
public class AuthService : IAuthService
{
    // Dependencies injected via constructor
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IConfiguration _configuration;

    // Business logic orchestration
    public async Task<(UserDto user, string error)> RegisterUserAsync(RegisterUserCommand command)
    {
        // 1. Validate business rules
        // 2. Create user entity
        // 3. Create associated wallet
        // 4. Generate verification token
        // 5. Send verification email
        // 6. Return user DTO
    }
}
```

#### Exchange Service Pattern
```csharp
public class ExchangeService : IExchangeService
{
    // Complex order matching logic
    public async Task<(ShareOrderDto order, string error)> PlaceOrderAsync(CreateShareOrderRequest request)
    {
        // 1. Validate order parameters
        // 2. Check user wallet balance (for buy orders)
        // 3. Create order entity
        // 4. Attempt immediate matching
        // 5. Update portfolios on successful trades
        // 6. Update wallet balances
        // 7. Return order status
    }

    // Sophisticated matching algorithm
    private async Task<List<ShareTrade>> MatchOrderAsync(ShareOrder newOrder)
    {
        // Complex matching logic with price-time priority
        // Handles partial fills and multiple matches
    }
}
```

### Data Access Patterns

#### Unit of Work Pattern
```csharp
public interface IUnitOfWork : IDisposable
{
    // Entity repositories
    IGenericRepository<User> Users { get; }
    IGenericRepository<Wallet> Wallets { get; }
    IGenericRepository<ShareOrder> ShareOrders { get; }
    // ... other repositories

    // Specialized repositories
    ISystemSettingRepository SystemSettings { get; }
    ITransactionTypeRepository TransactionTypes { get; }
    
    // Transaction management
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

#### Repository Pattern
```csharp
public interface IGenericRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<bool> ExistsAsync(int id);
}
```

## Validation Architecture

### FluentValidation Integration
Every command and query implements comprehensive validation:

```csharp
public class CreateShareOrderRequestValidator : AbstractValidator<CreateShareOrderRequest>
{
    public CreateShareOrderRequestValidator()
    {
        RuleFor(x => x.TradingAccountId)
            .GreaterThan(0)
            .WithMessage("Trading account ID must be greater than 0");

        RuleFor(x => x.QuantityOrdered)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.LimitPrice)
            .GreaterThan(0)
            .When(x => x.OrderType == "Limit")
            .WithMessage("Limit price must be greater than 0 for limit orders");
    }
}
```

### Business Rule Validation
Beyond input validation, services implement business rule validation:
- **Wallet Balance**: Sufficient funds for purchases
- **Trading Account Status**: Active accounts only
- **User Authorization**: Proper permissions for operations
- **Financial Constraints**: Minimum/maximum transaction amounts

## Error Handling Strategy

### Service Return Patterns
Services use tuple return patterns for explicit error handling:
```csharp
public async Task<(UserDto user, string error)> GetUserProfileAsync(int userId)
{
    try
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return (null, "User not found");
            
        return (new UserDto { ... }, null);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving user profile");
        return (null, "An error occurred while retrieving user profile");
    }
}
```

### Exception Handling
- **Global Exception Middleware**: Centralized exception handling at API level
- **Logging Integration**: Structured logging with correlation IDs
- **User-Friendly Messages**: Sanitized error messages for client consumption

## Security Architecture

### Authentication & Authorization
- **Claims-Based Identity**: Consistent user identity extraction
- **Role-Based Authorization**: Admin vs Investor access control
- **JWT Security**: Secure token generation and validation
- **API Key Authentication**: Separate authentication for EA integration

### Financial Security
- **Transaction Integrity**: Database-level constraints and application validation
- **Audit Logging**: Comprehensive audit trail for all financial operations
- **Admin Approval**: Multi-level approval for sensitive operations
- **Rate Limiting**: Protection against abuse at service level

## Performance Considerations

### Caching Strategy
- **In-Memory Caching**: Frequently accessed reference data
- **Cache Invalidation**: Strategic cache invalidation on data updates
- **Performance Metrics**: Dashboard calculations cached for efficiency

### Database Optimization
- **Pagination**: Consistent pagination across all list operations
- **Efficient Queries**: Optimized database queries with proper indexing
- **Bulk Operations**: Efficient handling of large datasets from EA integration

### Scalability Patterns
- **Stateless Services**: All services are stateless for horizontal scaling
- **Async Operations**: Asynchronous processing for I/O-bound operations
- **Background Processing**: Separate background workers for long-running tasks

## Testing Strategy

### Service Testing
- **Unit Tests**: Isolated testing of business logic
- **Integration Tests**: Service integration with database
- **Mock Dependencies**: External dependencies mocked for testing

### Validation Testing
- **FluentValidation Tests**: Comprehensive validation rule testing
- **Business Rule Tests**: Complex business logic verification
- **Edge Case Coverage**: Boundary condition testing

## Future Extensibility

### Domain Events
Ready for domain event implementation:
- **User Registration Events**
- **Trade Execution Events**
- **Portfolio Update Events**
- **Financial Transaction Events**

### Message Bus Integration
Architecture prepared for:
- **Command Bus**: Command routing and handling
- **Event Bus**: Domain event distribution
- **Query Bus**: Query routing and caching

### Microservice Readiness
Current architecture supports decomposition into:
- **User Management Service**
- **Wallet Service**
- **Trading Engine Service**
- **Portfolio Service**
- **Notification Service**

This Application layer architecture provides a solid foundation for a complex financial trading platform with proper separation of concerns, comprehensive business logic implementation, and extensibility for future enhancements.