 # QuantumBandsAI Solution Overview
 
 This document provides an overview of the `QuantumBandsAI` solution, focusing on its core components related to trading account management and API exposure.
 
 ## 1. Project Structure (Identified Core Components)
 
 The primary components identified from the provided files are:
 
 *   **`QuantumBands.Application` Layer**: Contains business logic and application services.
     *   `Services/TradingAccountService.cs`: Implements the core business logic for managing trading accounts, initial share offerings, and retrieving various account-related data.
 *   **`QuantumBands.API` Layer**: Exposes the application functionality via RESTful API endpoints.
     *   `Controllers/TradingAccountsController.cs`: Defines the API endpoints for interacting with trading account services.
 
 ## 2. `TradingAccountService` (Business Logic)
 
 The `TradingAccountService` is a central service responsible for handling all operations related to trading accounts. It interacts with the `IUnitOfWork` for data persistence and other services for specific functionalities.
 
 ### 2.1. Dependencies
 
 The service is injected with the following dependencies:
 *   `IUnitOfWork`: Provides access to various repositories (e.g., `TradingAccounts`, `Users`, `InitialShareOfferings`, `EAOpenPositions`, `EAClosedTrades`, `TradingAccountSnapshots`) for database operations.
 *   `ILogger<TradingAccountService>`: For logging application events and errors.
 *   `IClosedTradeService`: Likely used for retrieving performance KPIs related to closed trades.
 *   `IWalletService`: Used for fetching financial summaries (deposits, withdrawals).
 *   `ChartDataService`: Responsible for generating chart data for performance visualization.
 
 ### 2.2. Key Functionalities
 
 *   **Trading Account Management**:
     *   `CreateTradingAccountAsync`: Creates a new trading account, ensuring uniqueness of account names.
     *   `UpdateTradingAccountAsync`: Updates existing trading account details (name, description, EA name, management fee, active status). Includes validation for unique account names.
     *   `GetPublicTradingAccountsAsync`: Retrieves a paginated list of public trading accounts, with support for filtering by active status and search terms, and sorting by various criteria (share price, fee rate, creation date, name).
     *   `GetTradingAccountDetailsAsync`: Provides comprehensive details for a specific trading account, including open positions, closed trade history, and daily snapshots, with pagination for lists.
 *   **Initial Share Offering Management**:
     *   `CreateInitialShareOfferingAsync`: Creates a new share offering for a trading account, with checks for available shares.
     *   `GetInitialShareOfferingsAsync`: Retrieves paginated initial share offerings for a given trading account, with filtering by status and sorting.
     *   `UpdateInitialShareOfferingAsync`: Updates an existing share offering, with business rules preventing changes to `SharesOffered` or `OfferingPricePerShare` after sales have started, and status transition rules.
     *   `CancelInitialShareOfferingAsync`: Cancels an active share offering.
 *   **Account Overview & Performance**:
     *   `GetAccountOverviewAsync`: Provides a summary of an account's financial status and key performance indicators (KPIs) like win rate, profit factor, growth, and margin info.
     *   `GetChartDataAsync`: Generates data points for various performance charts (e.g., balance, equity, growth).
     *   `GetTradingHistoryAsync`: Fetches paginated trading history with extensive filtering (symbol, type, date, profit, volume) and sorting options, including summary statistics.
     *   `GetOpenPositionsRealtimeAsync`: Retrieves real-time open positions with detailed metrics, margin requirements, and market data.
     *   `GetStatisticsAsync`: Calculates and returns comprehensive trading statistics and risk metrics for a given period. (Currently a placeholder implementation).
 *   **Activity & Export**:
     *   `GetActivityAsync`: Provides a paginated audit trail of account activities (deposits, trades, logins, configurations) with filtering and sorting. (Uses simulated data for some activity types).
     *   `ExportDataAsync`: Exports various types of account data (trading history, statistics, reports) in different formats (CSV, Excel).
 
 ### 2.3. Authorization & User Context
 
 Many methods in `TradingAccountService` utilize `ClaimsPrincipal` to extract the `UserId` and determine if the user is an "Admin". This allows for fine-grained authorization, ensuring users can only access their own account data unless they have administrative privileges.
 
 ## 3. `TradingAccountsController` (API Endpoints)
 
 The `TradingAccountsController` exposes the functionalities of `TradingAccountService` via HTTP GET endpoints.
 
 ### 3.1. Base Route
 
 All endpoints are prefixed with `/api/v1/trading-accounts`.
 
 ### 3.2. Endpoints
 
 *   `GET /`: `GetPublicTradingAccounts` - Retrieves public trading accounts.
 *   `GET /{accountId}`: `GetTradingAccountDetails` - Gets detailed information for a specific account.
 *   `GET /{accountId}/initial-offerings`: `GetInitialShareOfferings` - Lists initial share offerings for an account.
 *   `GET /{accountId}/overview`: `GetAccountOverview` - Provides a financial and performance overview of an account.
 *   `GET /{accountId}/charts`: `GetChartsData` - Fetches data for performance charts.
 *   `GET /{accountId}/trading-history`: `GetTradingHistory` - Retrieves paginated trading history.
 *   `GET /{accountId}/open-positions`: `GetOpenPositions` - Gets real-time open positions.
 *   `GET /{accountId}/statistics`: `GetStatistics` - Provides comprehensive trading statistics.
 *   `GET /{accountId}/activity`: `GetActivity` - Fetches account activity logs.
 *   `GET /{accountId}/export`: `ExportData` - Exports account data in various formats.
 
 ### 3.3. Error Handling
 
 The controller handles various error scenarios, returning appropriate HTTP status codes (e.g., `200 OK`, `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `500 Internal Server Error`) along with descriptive error messages.
 
 ## 4. Data Models (DTOs)
 
 The application extensively uses Data Transfer Objects (DTOs) to structure data for requests and responses, such as `TradingAccountDto`, `TradingAccountDetailDto`, `InitialShareOfferingDto`, `PaginatedList`, `AccountOverviewDto`, `ChartDataDto`, `PaginatedTradingHistoryDto`, `OpenPositionsRealtimeDto`, `TradingStatisticsDto`, `AccountActivityDto`, and `ExportResult`.
 
 ## 5. Database Interaction
 
 The `IUnitOfWork` abstraction is used for all database interactions, promoting a clean separation of concerns and facilitating transactional operations. Entity Framework Core is likely used under the hood, with common patterns like `.Include()` for eager loading related entities, `.Where()` for filtering, `.OrderBy()` for sorting, and `FirstOrDefaultAsync()`, `AddAsync()`, `CompleteAsync()` for data retrieval and persistence.
 
 This overview should provide a solid foundation for understanding your `QuantumBandsAI` project.
 