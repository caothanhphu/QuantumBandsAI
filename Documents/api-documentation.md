# QuantumBands API Documentation

## Overview

The QuantumBands API is a RESTful web service that provides comprehensive functionality for a share-based trading platform. The API enables users to invest in Expert Advisor (EA) managed trading accounts through a share-based system, including full wallet management, order exchange, and administrative functions.

**Base URL**: `/api/v1`  
**Authentication**: JWT Bearer tokens with role-based authorization  
**Content Type**: `application/json`

## Architecture

### Authentication & Authorization
- **JWT Bearer Tokens**: All authenticated endpoints require `Authorization: Bearer <token>` header
- **Role-Based Access**: Two primary roles - `Admin` and `Investor` (default)
- **API Key Authentication**: EA integration endpoints use API key authentication
- **Two-Factor Authentication**: Optional 2FA support for enhanced security

### Response Patterns
- **Success Responses**: HTTP 200/201/204 with appropriate data
- **Error Responses**: Structured error objects with details
- **Pagination**: Standardized pagination for list endpoints
- **Validation**: Comprehensive input validation with detailed error messages

## API Endpoints by Domain

### 1. Authentication (`/api/v1/auth`)

#### User Registration & Email Verification
```http
POST /api/v1/auth/register
Content-Type: application/json

{
  "username": "string (3-50 chars, alphanumeric + underscore)",
  "email": "string (valid email, max 255 chars)",
  "password": "string (8-100 chars, mixed case + number + special)",
  "fullName": "string (optional, max 200 chars)"
}

Response: UserDto (201 Created)
```

```http
POST /api/v1/auth/verify-email
Content-Type: application/json

{
  "email": "string",
  "token": "string"
}

Response: Success message (200 OK)
```

```http
POST /api/v1/auth/resend-verification-email
Content-Type: application/json

{
  "email": "string"
}

Response: Success message (200 OK)
```

#### Authentication
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "usernameOrEmail": "string",
  "password": "string"
}

Response: LoginResponse (200 OK)
{
  "token": "string (JWT access token)",
  "refreshToken": "string",
  "expiration": "datetime",
  "user": { UserDto }
}
```

```http
POST /api/v1/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "string"
}

Response: LoginResponse (200 OK)
```

```http
POST /api/v1/auth/logout
Authorization: Bearer <token>

Response: Success message (200 OK)
```

#### Password Management
```http
POST /api/v1/auth/forgot-password
Content-Type: application/json

{
  "email": "string"
}

Response: Success message (200 OK)
```

```http
POST /api/v1/auth/reset-password
Content-Type: application/json

{
  "email": "string",
  "token": "string",
  "newPassword": "string"
}

Response: Success message (200 OK)
```

### 2. User Management (`/api/v1/users`)
*All endpoints require authentication*

#### Profile Management
```http
GET /api/v1/users/me
Authorization: Bearer <token>

Response: UserProfileDto (200 OK)
{
  "userId": "int",
  "username": "string",
  "email": "string", 
  "fullName": "string",
  "isEmailVerified": "boolean",
  "twoFactorEnabled": "boolean",
  "createdAt": "datetime"
}
```

```http
PUT /api/v1/users/me
Authorization: Bearer <token>
Content-Type: application/json

{
  "fullName": "string (max 200 chars)"
}

Response: UserProfileDto (200 OK)
```

#### Security Operations
```http
POST /api/v1/users/change-password
Authorization: Bearer <token>
Content-Type: application/json

{
  "currentPassword": "string",
  "newPassword": "string"
}

Response: Success message (200 OK)
```

#### Two-Factor Authentication
```http
POST /api/v1/users/2fa/setup
Authorization: Bearer <token>

Response: Setup2FAResponse (200 OK)
{
  "qrCodeDataUrl": "string (QR code image)",
  "secretKey": "string (backup key)"
}
```

```http
POST /api/v1/users/2fa/enable
Authorization: Bearer <token>
Content-Type: application/json

{
  "token": "string (6-digit code)"
}

Response: Success message with recovery codes (200 OK)
```

```http
POST /api/v1/users/2fa/verify
Authorization: Bearer <token>
Content-Type: application/json

{
  "token": "string (6-digit code)"
}

Response: Success message (200 OK)
```

```http
POST /api/v1/users/2fa/disable
Authorization: Bearer <token>
Content-Type: application/json

{
  "currentPassword": "string"
}

Response: Success message (200 OK)
```

### 3. Wallet Management (`/api/v1/wallets`)
*All endpoints require authentication*

#### Wallet Information
```http
GET /api/v1/wallets
Authorization: Bearer <token>

Response: WalletDto (200 OK)
{
  "walletId": "int",
  "balance": "decimal",
  "currencyCode": "string",
  "emailForQR": "string (for QR code generation)"
}
```

```http
GET /api/v1/wallets/transactions
Authorization: Bearer <token>
Query Parameters:
- page: int (default: 1)
- pageSize: int (default: 10, max: 50)
- transactionType: string (optional filter)
- startDate: datetime (optional)
- endDate: datetime (optional)

Response: PaginatedList<WalletTransactionDto> (200 OK)
```

#### Deposit Operations
```http
POST /api/v1/wallets/deposits/bank/initiate
Authorization: Bearer <token>
Content-Type: application/json

{
  "amount": "decimal (> 0)",
  "currencyCode": "string (USD)",
  "userProvidedNotes": "string (optional, max 500 chars)"
}

Response: BankDepositInfoResponse (200 OK)
{
  "transactionId": "long",
  "amount": "decimal",
  "referenceCode": "string",
  "bankingInstructions": "string",
  "expirationTime": "datetime"
}
```

#### Withdrawal Operations
```http
POST /api/v1/wallets/withdrawals
Authorization: Bearer <token>
Content-Type: application/json

{
  "amount": "decimal (> 0)",
  "currencyCode": "string (USD)",
  "withdrawalMethodDetails": "string (max 1000 chars)",
  "userProvidedNotes": "string (optional, max 500 chars)"
}

Response: WithdrawalRequestDto (200 OK)
```

#### Internal Transfers
```http
POST /api/v1/wallets/internal-transfer/verify-recipient
Authorization: Bearer <token>
Content-Type: application/json

{
  "recipientUsernameOrEmail": "string"
}

Response: RecipientInfoResponse (200 OK)
{
  "recipientUserId": "int",
  "recipientUsername": "string",
  "recipientFullName": "string"
}
```

```http
POST /api/v1/wallets/internal-transfer/execute
Authorization: Bearer <token>
Content-Type: application/json

{
  "recipientUserId": "int",
  "amount": "decimal (> 0)",
  "currencyCode": "string (USD)",
  "notes": "string (optional, max 500 chars)"
}

Response: WalletTransactionDto (200 OK)
```

### 4. Trading Accounts (`/api/v1/trading-accounts`)
*Public endpoints unless specified*

#### Public Information
```http
GET /api/v1/trading-accounts
Query Parameters:
- page: int (default: 1)
- pageSize: int (default: 10, max: 50)
- search: string (optional, searches name/description)
- isActive: boolean (optional filter)
- sortBy: string (Name, CreatedAt, CurrentNAV)
- sortOrder: string (Asc, Desc)

Response: PaginatedList<TradingAccountDto> (200 OK)
```

```http
GET /api/v1/trading-accounts/{accountId}
Path Parameters:
- accountId: int
Query Parameters:
- includeClosedTrades: boolean (default: true)
- includeOpenPositions: boolean (default: true)
- includeSnapshots: boolean (default: true)
- tradesPage: int (default: 1)
- tradesPageSize: int (default: 20, max: 50)

Response: TradingAccountDetailDto (200 OK)
{
  "tradingAccountId": "int",
  "accountName": "string",
  "description": "string",
  "eaName": "string",
  "initialCapital": "decimal",
  "currentNetAssetValue": "decimal",
  "currentSharePrice": "decimal",
  "totalSharesIssued": "long",
  "managementFeeRate": "decimal",
  "isActive": "boolean",
  "createdAt": "datetime",
  "closedTrades": "List<EAClosedTradeDto>",
  "openPositions": "List<EAOpenPositionDto>",
  "snapshots": "PaginatedList<TradingAccountSnapshotDto>"
}
```

```http
GET /api/v1/trading-accounts/{accountId}/initial-offerings
Path Parameters:
- accountId: int
Query Parameters:
- page: int (default: 1)
- pageSize: int (default: 10, max: 50)
- status: string (optional filter)

Response: PaginatedList<InitialShareOfferingDto> (200 OK)
```

### 5. Exchange Trading (`/api/v1/exchange`)
*All endpoints require authentication unless specified*

#### Order Management
```http
POST /api/v1/exchange/orders
Authorization: Bearer <token>
Content-Type: application/json

{
  "tradingAccountId": "int",
  "orderSide": "string (Buy/Sell)",
  "orderType": "string (Market/Limit)",
  "quantityOrdered": "long (> 0)",
  "limitPrice": "decimal (required for limit orders)"
}

Response: ShareOrderDto (201 Created)
```

```http
GET /api/v1/exchange/orders/my
Authorization: Bearer <token>
Query Parameters:
- page: int (default: 1)
- pageSize: int (default: 10, max: 50)
- tradingAccountId: int (optional filter)
- orderSide: string (optional filter)
- orderStatus: string (optional filter)
- fromDate: datetime (optional)
- toDate: datetime (optional)

Response: PaginatedList<ShareOrderDto> (200 OK)
```

```http
DELETE /api/v1/exchange/orders/{orderId}
Authorization: Bearer <token>
Path Parameters:
- orderId: long

Response: No Content (204)
```

#### Market Data
```http
GET /api/v1/exchange/order-book/{tradingAccountId}
Path Parameters:
- tradingAccountId: int
Query Parameters:
- depth: int (default: 10, max: 100)

Response: OrderBookDto (200 OK)
{
  "tradingAccountId": "int",
  "buyOrders": "List<OrderBookEntryDto>",
  "sellOrders": "List<OrderBookEntryDto>",
  "lastTradePrice": "decimal",
  "lastTradeTime": "datetime"
}
```

```http
GET /api/v1/exchange/market-data
Query Parameters:
- tradingAccountIds: int[] (optional filter)
- includeRecentTrades: boolean (default: true)
- recentTradesLimit: int (default: 10, max: 50)
- includeActiveOfferings: boolean (default: true)
- activeOfferingsLimit: int (default: 10, max: 50)

Response: MarketDataResponse (200 OK)
{
  "tradingAccounts": "List<TradingAccountMarketDataDto>",
  "recentTrades": "List<SimpleTradeDto>",
  "activeOfferings": "List<ActiveOfferingDto>"
}
```

#### Trade History
```http
GET /api/v1/exchange/trades/my
Authorization: Bearer <token>
Query Parameters:
- page: int (default: 1)
- pageSize: int (default: 10, max: 50)
- tradingAccountId: int (optional filter)
- fromDate: datetime (optional)
- toDate: datetime (optional)

Response: PaginatedList<MyShareTradeDto> (200 OK)
```

### 6. Portfolio (`/api/v1/portfolio`)
*All endpoints require authentication*

```http
GET /api/v1/portfolio/me
Authorization: Bearer <token>

Response: List<SharePortfolioItemDto> (200 OK)
[
  {
    "tradingAccountId": "int",
    "tradingAccountName": "string",
    "quantity": "long",
    "averageBuyPrice": "decimal",
    "currentSharePrice": "decimal",
    "currentValue": "decimal",
    "unrealizedPnL": "decimal",
    "unrealizedPnLPercentage": "decimal",
    "lastUpdatedAt": "datetime"
  }
]
```

### 7. EA Integration (`/api/v1/ea-integration`)
*All endpoints require API key authentication*

```http
POST /api/v1/ea-integration/trading-accounts/{accountId}/live-data
X-API-Key: <api-key>
Path Parameters:
- accountId: int
Content-Type: application/json

{
  "accountEquity": "decimal",
  "accountBalance": "decimal",
  "openPositions": [
    {
      "eaTicketId": "string",
      "symbol": "string",
      "tradeType": "string (Buy/Sell)",
      "volumeLots": "decimal",
      "openPrice": "decimal",
      "openTime": "datetime",
      "currentMarketPrice": "decimal",
      "swap": "decimal",
      "commission": "decimal",
      "floatingPnL": "decimal"
    }
  ]
}

Response: LiveDataResponse (200 OK)
```

```http
POST /api/v1/ea-integration/trading-accounts/{accountId}/closed-trades
X-API-Key: <api-key>
Path Parameters:
- accountId: int
Content-Type: application/json

{
  "closedTrades": [
    {
      "eaTicketId": "string",
      "symbol": "string", 
      "tradeType": "string",
      "volumeLots": "decimal",
      "openPrice": "decimal",
      "openTime": "datetime",
      "closePrice": "decimal",
      "closeTime": "datetime",
      "swap": "decimal",
      "commission": "decimal",
      "realizedPnL": "decimal"
    }
  ]
}

Response: PushClosedTradesResponse (200 OK)
```

### 8. Administration (`/api/v1/admin`)
*All endpoints require Admin role*

#### Dashboard
```http
GET /api/v1/admin/dashboard/summary
Authorization: Bearer <admin-token>

Response: AdminDashboardSummaryDto (200 OK)
{
  "totalUsers": "int",
  "totalActiveFunds": "int",
  "totalPlatformNAV": "decimal",
  "pendingDeposits": "int",
  "pendingWithdrawals": "int",
  "recentTrades": "List<SimpleTradeInfoDto>",
  "userGrowthData": "List<ChartDataPoint>",
  "navGrowthData": "List<ChartDataPoint>"
}
```

#### User Management
```http
GET /api/v1/admin/users
Authorization: Bearer <admin-token>
Query Parameters:
- page: int (default: 1)
- pageSize: int (default: 10, max: 100)
- search: string (optional)
- roleId: int (optional filter)
- isActive: boolean (optional filter)
- sortBy: string (Username, Email, CreatedAt, LastLoginDate)
- sortOrder: string (Asc, Desc)

Response: PaginatedList<AdminUserViewDto> (200 OK)
```

```http
PUT /api/v1/admin/users/{userId}/status
Authorization: Bearer <admin-token>
Path Parameters:
- userId: int
Content-Type: application/json

{
  "isActive": "boolean"
}

Response: AdminUserViewDto (200 OK)
```

```http
PUT /api/v1/admin/users/{userId}/role
Authorization: Bearer <admin-token>
Path Parameters:
- userId: int
Content-Type: application/json

{
  "roleId": "int"
}

Response: AdminUserViewDto (200 OK)
```

#### Trading Account Management
```http
POST /api/v1/admin/trading-accounts
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "accountName": "string (max 100 chars)",
  "description": "string (optional, max 1000 chars)",
  "eaName": "string (optional, max 100 chars)",
  "brokerPlatformIdentifier": "string (optional, max 100 chars)",
  "initialCapital": "decimal (> 0)",
  "totalSharesIssued": "long (> 0)",
  "managementFeeRate": "decimal (0-0.9999)"
}

Response: TradingAccountDto (201 Created)
```

```http
PUT /api/v1/admin/trading-accounts/{accountId}
Authorization: Bearer <admin-token>
Path Parameters:
- accountId: int
Content-Type: application/json

{
  "accountName": "string (max 100 chars)",
  "description": "string (optional, max 1000 chars)",
  "eaName": "string (optional, max 100 chars)",
  "managementFeeRate": "decimal (0-0.9999)",
  "isActive": "boolean"
}

Response: TradingAccountDto (200 OK)
```

#### Initial Share Offering Management
```http
POST /api/v1/admin/trading-accounts/{accountId}/initial-offerings
Authorization: Bearer <admin-token>
Path Parameters:
- accountId: int
Content-Type: application/json

{
  "sharesOffered": "long (> 0)",
  "offeringPricePerShare": "decimal (> 0)",
  "floorPricePerShare": "decimal (optional)",
  "ceilingPricePerShare": "decimal (optional)",
  "offeringStartDate": "datetime",
  "offeringEndDate": "datetime (optional)"
}

Response: InitialShareOfferingDto (201 Created)
```

```http
PUT /api/v1/admin/trading-accounts/{accountId}/initial-offerings/{offeringId}
Authorization: Bearer <admin-token>
Path Parameters:  
- accountId: int
- offeringId: int
Content-Type: application/json

{
  "sharesOffered": "long (> 0)",
  "offeringPricePerShare": "decimal (> 0)",
  "floorPricePerShare": "decimal (optional)",
  "ceilingPricePerShare": "decimal (optional)",
  "offeringEndDate": "datetime (optional)"
}

Response: InitialShareOfferingDto (200 OK)
```

```http
POST /api/v1/admin/trading-accounts/{accountId}/initial-offerings/{offeringId}/cancel
Authorization: Bearer <admin-token>
Path Parameters:
- accountId: int
- offeringId: int
Content-Type: application/json

{
  "cancellationReason": "string (optional, max 500 chars)"
}

Response: InitialShareOfferingDto (200 OK)
```

#### Wallet Administration
```http
GET /api/v1/admin/wallets/deposits/bank/pending-confirmation
Authorization: Bearer <admin-token>
Query Parameters:
- page: int (default: 1)
- pageSize: int (default: 10, max: 50)
- fromDate: datetime (optional)
- toDate: datetime (optional)

Response: PaginatedList<AdminPendingBankDepositDto> (200 OK)
```

```http
POST /api/v1/admin/wallets/deposits/bank/confirm
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "transactionId": "long",
  "actualAmountReceived": "decimal (optional)"
}

Response: WalletTransactionDto (200 OK)
```

```http
POST /api/v1/admin/wallets/deposits/bank/cancel
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "transactionId": "long",
  "cancellationReason": "string (optional, max 500 chars)"
}

Response: WalletTransactionDto (200 OK)
```

```http
GET /api/v1/admin/wallets/withdrawals/pending-approval
Authorization: Bearer <admin-token>
Query Parameters:
- page: int (default: 1)
- pageSize: int (default: 10, max: 50)
- fromDate: datetime (optional)
- toDate: datetime (optional)

Response: PaginatedList<WithdrawalRequestAdminViewDto> (200 OK)
```

```http
POST /api/v1/admin/wallets/withdrawals/approve
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "transactionId": "long",
  "approvalNotes": "string (optional, max 500 chars)"
}

Response: WalletTransactionDto (200 OK)
```

```http
POST /api/v1/admin/wallets/withdrawals/reject
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "transactionId": "long",
  "rejectionReason": "string (max 500 chars)"
}

Response: WalletTransactionDto (200 OK)
```

```http
POST /api/v1/admin/wallets/deposit
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "userId": "int",
  "amount": "decimal (> 0)",
  "currencyCode": "string (USD)",
  "description": "string (optional, max 500 chars)"
}

Response: WalletTransactionDto (200 OK)
```

#### Exchange Monitoring
```http
GET /api/v1/admin/exchange/orders
Authorization: Bearer <admin-token>
Query Parameters:
- page: int (default: 1)
- pageSize: int (default: 10, max: 100)
- tradingAccountId: int (optional filter)
- userId: int (optional filter)
- orderStatus: string (optional filter)
- orderSide: string (optional filter)
- fromDate: datetime (optional)
- toDate: datetime (optional)
- sortBy: string
- sortOrder: string (Asc, Desc)

Response: PaginatedList<AdminShareOrderViewDto> (200 OK)
```

```http
GET /api/v1/admin/exchange/trades
Authorization: Bearer <admin-token>
Query Parameters:
- page: int (default: 1)
- pageSize: int (default: 10, max: 100)
- tradingAccountId: int (optional filter)
- buyerUserId: int (optional filter)
- sellerUserId: int (optional filter)
- fromDate: datetime (optional)
- toDate: datetime (optional)
- sortBy: string
- sortOrder: string (Asc, Desc)

Response: PaginatedList<AdminShareTradeViewDto> (200 OK)
```

## Error Handling

### Standard Error Response Format
```json
{
  "type": "string (error type URL)",
  "title": "string (error title)",
  "status": "int (HTTP status code)",
  "detail": "string (detailed error message)",
  "errors": {
    "fieldName": ["validation error messages"]
  }
}
```

### Common HTTP Status Codes
- **200 OK**: Successful GET requests
- **201 Created**: Successful POST requests that create resources
- **204 No Content**: Successful DELETE requests
- **400 Bad Request**: Validation errors or malformed requests
- **401 Unauthorized**: Missing or invalid authentication
- **403 Forbidden**: Insufficient permissions
- **404 Not Found**: Resource not found
- **409 Conflict**: Business rule conflicts (insufficient funds, etc.)
- **500 Internal Server Error**: Unexpected server errors

## Rate Limiting & Quotas

### API Rate Limits
- **Authenticated Users**: 1000 requests per hour
- **Admin Users**: 5000 requests per hour  
- **EA Integration**: 10000 requests per hour
- **Anonymous**: 100 requests per hour

### Business Quotas
- **Order Placement**: 100 orders per user per day
- **Wallet Transactions**: 50 transactions per user per day
- **2FA Attempts**: 5 failed attempts per hour

## SDK & Integration Examples

### JavaScript/TypeScript
```javascript
const API_BASE = 'https://api.quantumbands.com/api/v1';

// Authentication
const login = async (credentials) => {
  const response = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(credentials)
  });
  return response.json();
};

// Authenticated request
const getPortfolio = async (token) => {
  const response = await fetch(`${API_BASE}/portfolio/me`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  return response.json();
};
```

### Python
```python
import requests

class QuantumBandsAPI:
    def __init__(self, base_url='https://api.quantumbands.com/api/v1'):
        self.base_url = base_url
        self.token = None
    
    def login(self, username_or_email, password):
        response = requests.post(f'{self.base_url}/auth/login', 
            json={'usernameOrEmail': username_or_email, 'password': password})
        if response.ok:
            self.token = response.json()['token']
        return response.json()
    
    def get_portfolio(self):
        headers = {'Authorization': f'Bearer {self.token}'}
        response = requests.get(f'{self.base_url}/portfolio/me', headers=headers)
        return response.json()
```

## Security Considerations

### Authentication Security
- JWT tokens expire after 24 hours
- Refresh tokens expire after 30 days
- Secure HTTP-only cookies for web applications
- API key rotation for EA integration

### Data Protection
- All sensitive data encrypted at rest
- TLS 1.3 for data in transit
- PII masking in logs
- GDPR compliant data handling

### Financial Security
- Two-factor authentication support
- Transaction approval workflows
- Audit logging for all financial operations
- Rate limiting on financial endpoints

This API documentation provides comprehensive coverage of all available endpoints with detailed request/response specifications, authentication requirements, and integration examples for building client applications on the QuantumBands platform.