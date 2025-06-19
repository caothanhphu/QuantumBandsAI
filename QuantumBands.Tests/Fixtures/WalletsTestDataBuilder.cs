using System;
using System.Collections.Generic;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Wallets.Commands.AdminActions;
using QuantumBands.Application.Features.Wallets.Commands.AdminDeposit;
using QuantumBands.Application.Features.Wallets.Commands.BankDeposit;
using QuantumBands.Application.Features.Wallets.Commands.CreateWithdrawal;
using QuantumBands.Application.Features.Wallets.Commands.InternalTransfer;
using QuantumBands.Application.Features.Wallets.Dtos;
using QuantumBands.Application.Features.Wallets.Queries.GetTransactions;
using QuantumBands.Domain.Entities;

namespace QuantumBands.Tests.Fixtures;

/// <summary>
/// WalletsTestDataBuilder - Test data builder for Wallets domain
/// 
/// This class provides comprehensive test data for wallet-related functionality including:
/// - Wallet entity and DTO builders 
/// - Bank deposit operations (initiate, confirm, cancel)
/// - Withdrawal operations (create, approve, reject)
/// - Internal transfer operations (verify recipient, execute transfer)
/// - Transaction querying and pagination
/// - Admin wallet management operations
/// 
/// Extracted from TestDataBuilder.cs to improve code organization and maintainability.
/// Contains 10 static classes covering all wallet-related test scenarios.
/// 
/// Usage:
/// - WalletsWalletsTestDataBuilder.Wallets.ValidWallet(userId) - Entity builders
/// - WalletsWalletsTestDataBuilder.BankDeposit.ValidRequest() - Bank deposit operations
/// - WalletsWalletsTestDataBuilder.CreateWithdrawal.ValidRequest() - Withdrawal operations
/// - WalletsWalletsTestDataBuilder.ExecuteInternalTransfer.ValidRequest() - Transfer operations
/// 
/// Domain coverage:
/// - Core wallet functionality and balance management
/// - Multi-step deposit workflows with admin confirmation
/// - Withdrawal request and approval workflows
/// - Internal user-to-user transfer functionality
/// - Transaction history and filtering capabilities
/// - Admin oversight and management operations
/// </summary>
public static class WalletsTestDataBuilder
{
    public static class Wallets
    {
        public static Wallet ValidWallet(int userId) => new()
        {
            WalletId = 1,
            UserId = userId,
            Balance = 0.00m,
            CurrencyCode = "USD",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // SCRUM-43: Test data for bank deposit initiation endpoint testing
    public static class BankDeposit
    {
        public static InitiateBankDepositRequest ValidInitiateBankDepositRequest() => new()
        {
            AmountUSD = 100.00m
        };

        public static InitiateBankDepositRequest SmallAmountDepositRequest() => new()
        {
            AmountUSD = 0.01m
        };

        public static InitiateBankDepositRequest LargeAmountDepositRequest() => new()
        {
            AmountUSD = 10000.00m
        };

        public static InitiateBankDepositRequest ZeroAmountDepositRequest() => new()
        {
            AmountUSD = 0.00m
        };

        public static InitiateBankDepositRequest NegativeAmountDepositRequest() => new()
        {
            AmountUSD = -50.00m
        };

        public static InitiateBankDepositRequest VeryLargeAmountDepositRequest() => new()
        {
            AmountUSD = 999999.99m
        };

        public static InitiateBankDepositRequest DecimalPrecisionDepositRequest() => new()
        {
            AmountUSD = 123.456789m
        };

        public static InitiateBankDepositRequest MinimumValidDepositRequest() => new()
        {
            AmountUSD = 0.01m
        };

        public static BankDepositInfoResponse ValidBankDepositInfoResponse() => new()
        {
            TransactionId = 12345,
            RequestedAmountUSD = 100.00m,
            AmountVND = 2400000.00m,
            ExchangeRate = 24000.00m,
            BankName = "Test Bank Vietnam",
            AccountHolder = "FINIX TRADING COMPANY LIMITED",
            AccountNumber = "1234567890",
            ReferenceCode = "FINIXDEP202501001234"
        };

        public static BankDepositInfoResponse LargeAmountBankDepositInfoResponse() => new()
        {
            TransactionId = 12346,
            RequestedAmountUSD = 999999.99m,
            AmountVND = 23999999760000.00m,
            ExchangeRate = 24000.00m,
            BankName = "Test Bank Vietnam",
            AccountHolder = "FINIX TRADING COMPANY LIMITED",
            AccountNumber = "1234567890",
            ReferenceCode = "FINIXDEP202501001235"
        };

        public static BankDepositInfoResponse SmallAmountBankDepositInfoResponse() => new()
        {
            TransactionId = 12347,
            RequestedAmountUSD = 0.01m,
            AmountVND = 240.00m,
            ExchangeRate = 24000.00m,
            BankName = "Test Bank Vietnam",
            AccountHolder = "FINIX TRADING COMPANY LIMITED",
            AccountNumber = "1234567890",
            ReferenceCode = "FINIXDEP202501001236"
        };

        public static BankDepositInfoResponse CustomAmountBankDepositInfoResponse(decimal amountUSD, string referenceCode) => new()
        {
            TransactionId = 12348,
            RequestedAmountUSD = amountUSD,
            AmountVND = amountUSD * 24000.00m,
            ExchangeRate = 24000.00m,
            BankName = "Test Bank Vietnam",
            AccountHolder = "FINIX TRADING COMPANY LIMITED",
            AccountNumber = "1234567890",
            ReferenceCode = referenceCode
        };

        public static BankDepositInfoResponse ResponseWithMissingBankInfo() => new()
        {
            TransactionId = 12349,
            RequestedAmountUSD = 100.00m,
            AmountVND = 2400000.00m,
            ExchangeRate = 24000.00m,
            BankName = "N/A",
            AccountHolder = "N/A",
            AccountNumber = "N/A",
            ReferenceCode = "FINIXDEP202501001237"
        };

        public static BankDepositInfoResponse ResponseWithDifferentExchangeRate() => new()
        {
            TransactionId = 12350,
            RequestedAmountUSD = 100.00m,
            AmountVND = 2500000.00m,
            ExchangeRate = 25000.00m,
            BankName = "Test Bank Vietnam",
            AccountHolder = "FINIX TRADING COMPANY LIMITED",
            AccountNumber = "1234567890",
            ReferenceCode = "FINIXDEP202501001238"
        };

        public static BankDepositInfoResponse ResponseWithLongReferenceCode() => new()
        {
            TransactionId = 12351,
            RequestedAmountUSD = 100.00m,
            AmountVND = 2400000.00m,
            ExchangeRate = 24000.00m,
            BankName = "Test Bank Vietnam",
            AccountHolder = "FINIX TRADING COMPANY LIMITED",
            AccountNumber = "1234567890",
            ReferenceCode = "FINIXDEP2025010012345678901234567890"
        };

        public static BankDepositInfoResponse ResponseWithSpecialCharactersInBankInfo() => new()
        {
            TransactionId = 12352,
            RequestedAmountUSD = 100.00m,
            AmountVND = 2400000.00m,
            ExchangeRate = 24000.00m,
            BankName = "Ngân Hàng TMCP Việt Nam",
            AccountHolder = "CÔNG TY TNHH FINIX TRADING",
            AccountNumber = "1234-5678-90",
            ReferenceCode = "FINIXDEP202501001239"
        };

        public static BankDepositInfoResponse ResponseWithRoundedVNDAmount() => new()
        {
            TransactionId = 12353,
            RequestedAmountUSD = 123.456789m,
            AmountVND = 2962963.00m, // Rounded to whole VND
            ExchangeRate = 24000.00m,
            BankName = "Test Bank Vietnam",
            AccountHolder = "FINIX TRADING COMPANY LIMITED",
            AccountNumber = "1234567890",
            ReferenceCode = "FINIXDEP202501001240"
        };
    }

    // SCRUM-49: Test data for GetMyWallet endpoint testing
    public static class GetMyWallet
    {
        // Valid wallet response scenarios for different user types
        public static WalletDto ValidUserWallet() => new()
        {
            WalletId = 1,
            UserId = 1,
            Balance = 1000.50m,
            CurrencyCode = "USD",
            EmailForQrCode = "testuser@example.com",
            UpdatedAt = DateTime.UtcNow
        };

        public static WalletDto ValidBusinessUserWallet() => new()
        {
            WalletId = 2,
            UserId = 2,
            Balance = 25000.75m,
            CurrencyCode = "USD",
            EmailForQrCode = "business.user@company.com",
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        };

        public static WalletDto ValidAdminWallet() => new()
        {
            WalletId = 3,
            UserId = 3,
            Balance = 50000.00m,
            CurrencyCode = "USD",
            EmailForQrCode = "admin@quantumbands.ai",
            UpdatedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        // Edge case scenarios for comprehensive testing
        public static WalletDto ZeroBalanceWallet() => new()
        {
            WalletId = 4,
            UserId = 4,
            Balance = 0.00m,
            CurrencyCode = "USD",
            EmailForQrCode = "newuser@example.com",
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        public static WalletDto SmallBalanceWallet() => new()
        {
            WalletId = 5,
            UserId = 5,
            Balance = 0.01m,
            CurrencyCode = "USD",
            EmailForQrCode = "smallbalance@test.com",
            UpdatedAt = DateTime.UtcNow.AddHours(-6)
        };

        public static WalletDto LargeBalanceWallet() => new()
        {
            WalletId = 6,
            UserId = 6,
            Balance = 999999.99m,
            CurrencyCode = "USD",
            EmailForQrCode = "largebalance@example.org",
            UpdatedAt = DateTime.UtcNow.AddDays(-7)
        };

        public static WalletDto WalletWithPreciseBalance() => new()
        {
            WalletId = 7,
            UserId = 7,
            Balance = 123.456789m, // High precision balance
            CurrencyCode = "USD",
            EmailForQrCode = "precision@test.com",
            UpdatedAt = DateTime.UtcNow.AddMinutes(-15)
        };

        public static WalletDto WalletWithSpecialCharacterEmail() => new()
        {
            WalletId = 8,
            UserId = 8,
            Balance = 500.25m,
            CurrencyCode = "USD",
            EmailForQrCode = "special+user@test-domain.co.uk",
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };

        public static WalletDto WalletWithDifferentCurrency() => new()
        {
            WalletId = 9,
            UserId = 9,
            Balance = 1500.00m,
            CurrencyCode = "EUR",
            EmailForQrCode = "euro.user@europe.com",
            UpdatedAt = DateTime.UtcNow.AddHours(-4)
        };

        public static WalletDto WalletWithQrCodePrefix() => new()
        {
            WalletId = 10,
            UserId = 10,
            Balance = 750.33m,
            CurrencyCode = "USD",
            EmailForQrCode = "mailto:qrcode@example.com", // With QR code prefix
            UpdatedAt = DateTime.UtcNow.AddMinutes(-45)
        };

        public static WalletDto WalletWithLongEmail() => new()
        {
            WalletId = 11,
            UserId = 11,
            Balance = 2000.00m,
            CurrencyCode = "USD",
            EmailForQrCode = "very.long.email.address.for.testing.purposes@extremely-long-domain-name.example.org",
            UpdatedAt = DateTime.UtcNow.AddDays(-3)
        };

        public static WalletDto WalletWithMinimalData() => new()
        {
            WalletId = 12,
            UserId = 12,
            Balance = 10.00m,
            CurrencyCode = "USD",
            EmailForQrCode = "min@t.co",
            UpdatedAt = DateTime.UtcNow.AddHours(-12)
        };

        public static WalletDto WalletForVipUser() => new()
        {
            WalletId = 13,
            UserId = 13,
            Balance = 100000.00m,
            CurrencyCode = "USD",
            EmailForQrCode = "vip.member@quantumbands.ai",
            UpdatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        // Historical data scenarios
        public static WalletDto WalletWithOldTimestamp() => new()
        {
            WalletId = 14,
            UserId = 14,
            Balance = 800.75m,
            CurrencyCode = "USD",
            EmailForQrCode = "old.user@legacy.com",
            UpdatedAt = DateTime.UtcNow.AddDays(-365) // One year old
        };

        public static WalletDto WalletWithRecentActivity() => new()
        {
            WalletId = 15,
            UserId = 15,
            Balance = 300.50m,
            CurrencyCode = "USD",
            EmailForQrCode = "active.user@current.com",
            UpdatedAt = DateTime.UtcNow.AddMinutes(-1) // Very recent
        };

        // Utility method for creating custom wallets
        public static WalletDto CustomWallet(int walletId, int userId, decimal balance, string currencyCode, string email) => new()
        {
            WalletId = walletId,
            UserId = userId,
            Balance = balance,
            CurrencyCode = currencyCode,
            EmailForQrCode = email,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static class GetTransactions
    {
        /// <summary>
        /// Valid query with default pagination
        /// </summary>
        public static GetWalletTransactionsQuery ValidQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "TransactionDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with maximum allowed page size
        /// </summary>
        public static GetWalletTransactionsQuery QueryWithMaxPageSize() => new()
        {
            PageNumber = 1,
            PageSize = 50, // According to ticket: max 50
            SortBy = "TransactionDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with page size exceeding maximum
        /// </summary>
        public static GetWalletTransactionsQuery QueryWithExcessivePageSize() => new()
        {
            PageNumber = 1,
            PageSize = 100, // Exceeds max 50
            SortBy = "TransactionDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with zero page size
        /// </summary>
        public static GetWalletTransactionsQuery QueryWithZeroPageSize() => new()
        {
            PageNumber = 1,
            PageSize = 0,
            SortBy = "TransactionDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with negative page number
        /// </summary>
        public static GetWalletTransactionsQuery QueryWithNegativePageNumber() => new()
        {
            PageNumber = -1,
            PageSize = 10,
            SortBy = "TransactionDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with transaction type filter
        /// </summary>
        public static GetWalletTransactionsQuery QueryWithTransactionTypeFilter() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            TransactionType = "Deposit",
            SortBy = "TransactionDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with date range filter
        /// </summary>
        public static GetWalletTransactionsQuery QueryWithDateRangeFilter() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            SortBy = "TransactionDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with all filters combined
        /// </summary>
        public static GetWalletTransactionsQuery QueryWithCombinedFilters() => new()
        {
            PageNumber = 1,
            PageSize = 20,
            TransactionType = "Withdrawal",
            Status = "Completed",
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            SortBy = "Amount",
            SortOrder = "asc"
        };

        /// <summary>
        /// Valid paginated result with transactions
        /// </summary>
        public static PaginatedList<WalletTransactionDto> ValidPaginatedTransactions() => new(
            new List<WalletTransactionDto>
            {
                ValidDepositTransaction(),
                ValidWithdrawalTransaction(),
                ValidTransferTransaction()
            },
            15, // totalCount
            1,  // pageNumber
            10  // pageSize
        );

        /// <summary>
        /// Empty paginated result
        /// </summary>
        public static PaginatedList<WalletTransactionDto> EmptyPaginatedTransactions() => new(
            new List<WalletTransactionDto>(),
            0,  // totalCount
            1,  // pageNumber
            10  // pageSize
        );

        /// <summary>
        /// Large paginated result with max page size
        /// </summary>
        public static PaginatedList<WalletTransactionDto> LargePaginatedTransactions() => new(
            GenerateTransactionList(50),
            250, // totalCount
            1,   // pageNumber
            50   // pageSize
        );

        /// <summary>
        /// Single page result
        /// </summary>
        public static PaginatedList<WalletTransactionDto> SinglePageTransactions() => new(
            new List<WalletTransactionDto>
            {
                ValidDepositTransaction(),
                ValidWithdrawalTransaction()
            },
            2,  // totalCount
            1,  // pageNumber
            10  // pageSize
        );

        /// <summary>
        /// Valid deposit transaction DTO
        /// </summary>
        public static WalletTransactionDto ValidDepositTransaction() => new()
        {
            TransactionId = 1001,
            TransactionTypeName = "Bank Deposit",
            Amount = 1000.50m,
            CurrencyCode = "USD",
            BalanceAfter = 2500.75m,
            ReferenceId = "FINIXDEP202401001",
            PaymentMethod = "Bank Transfer",
            Description = "Bank deposit via reference code",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow.AddDays(-5)
        };

        /// <summary>
        /// Valid withdrawal transaction DTO
        /// </summary>
        public static WalletTransactionDto ValidWithdrawalTransaction() => new()
        {
            TransactionId = 1002,
            TransactionTypeName = "Withdrawal",
            Amount = -500.00m,
            CurrencyCode = "USD",
            BalanceAfter = 2000.75m,
            ReferenceId = "FINIXWTH202401002",
            PaymentMethod = "Bank Transfer",
            Description = "Withdrawal to bank account",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow.AddDays(-3)
        };

        /// <summary>
        /// Valid internal transfer transaction DTO
        /// </summary>
        public static WalletTransactionDto ValidTransferTransaction() => new()
        {
            TransactionId = 1003,
            TransactionTypeName = "Internal Transfer",
            Amount = -250.25m,
            CurrencyCode = "USD",
            BalanceAfter = 1750.50m,
            ReferenceId = "FINIXTRF202401003",
            PaymentMethod = "Internal",
            Description = "Transfer to user@example.com",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow.AddDays(-1)
        };

        /// <summary>
        /// Pending transaction DTO
        /// </summary>
        public static WalletTransactionDto PendingTransaction() => new()
        {
            TransactionId = 1004,
            TransactionTypeName = "Bank Deposit",
            Amount = 2000.00m,
            CurrencyCode = "USD",
            BalanceAfter = 1750.50m, // Balance not updated yet
            ReferenceId = "FINIXDEP202401004",
            PaymentMethod = "Bank Transfer",
            Description = "Pending bank deposit confirmation",
            Status = "PendingAdminConfirmation",
            TransactionDate = DateTime.UtcNow
        };

        /// <summary>
        /// Small amount transaction DTO
        /// </summary>
        public static WalletTransactionDto SmallAmountTransaction() => new()
        {
            TransactionId = 1005,
            TransactionTypeName = "Micro Deposit",
            Amount = 0.01m,
            CurrencyCode = "USD",
            BalanceAfter = 1750.51m,
            ReferenceId = "FINIXMIC202401005",
            PaymentMethod = "System",
            Description = "Interest payment",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow.AddHours(-2)
        };

        /// <summary>
        /// Generate a list of transactions for testing
        /// </summary>
        private static List<WalletTransactionDto> GenerateTransactionList(int count)
        {
            var transactions = new List<WalletTransactionDto>();
            for (int i = 1; i <= count; i++)
            {
                transactions.Add(new WalletTransactionDto
                {
                    TransactionId = 2000 + i,
                    TransactionTypeName = i % 2 == 0 ? "Deposit" : "Withdrawal",
                    Amount = i % 2 == 0 ? 100.00m + i : -(50.00m + i),
                    CurrencyCode = "USD",
                    BalanceAfter = 1000.00m + (i * 10),
                    ReferenceId = $"FINIXTEST{DateTime.UtcNow:yyyyMM}{i:D3}",
                    PaymentMethod = "Bank Transfer",
                    Description = $"Test transaction #{i}",
                    Status = "Completed",
                    TransactionDate = DateTime.UtcNow.AddHours(-i)
                });
            }
            return transactions;
        }
    }

    public static class CreateWithdrawal
    {
        /// <summary>
        /// Valid withdrawal request
        /// </summary>
        public static CreateWithdrawalRequest ValidRequest() => new()
        {
            Amount = 500.00m,
            CurrencyCode = "USD",
            WithdrawalMethodDetails = "Bank: VCB, Account: 0012300456, Name: Test User, Branch: HN",
            Notes = "Withdrawal for personal use"
        };

        /// <summary>
        /// Withdrawal request with minimum amount
        /// </summary>
        public static CreateWithdrawalRequest MinimumAmountRequest() => new()
        {
            Amount = 0.01m,
            CurrencyCode = "USD", 
            WithdrawalMethodDetails = "Bank: VCB, Account: 0012300456, Name: Test User, Branch: HN",
            Notes = "Test minimum withdrawal"
        };

        /// <summary>
        /// Withdrawal request with large amount
        /// </summary>
        public static CreateWithdrawalRequest LargeAmountRequest() => new()
        {
            Amount = 50000.00m,
            CurrencyCode = "USD",
            WithdrawalMethodDetails = "Bank: VCB, Account: 0012300456, Name: Test User, Branch: HN",
            Notes = "Large withdrawal for business"
        };

        /// <summary>
        /// Withdrawal request with zero amount (invalid)
        /// </summary>
        public static CreateWithdrawalRequest ZeroAmountRequest() => new()
        {
            Amount = 0.00m,
            CurrencyCode = "USD",
            WithdrawalMethodDetails = "Bank: VCB, Account: 0012300456, Name: Test User, Branch: HN",
            Notes = "Invalid zero withdrawal"
        };

        /// <summary>
        /// Withdrawal request with negative amount (invalid)
        /// </summary>
        public static CreateWithdrawalRequest NegativeAmountRequest() => new()
        {
            Amount = -100.00m,
            CurrencyCode = "USD",
            WithdrawalMethodDetails = "Bank: VCB, Account: 0012300456, Name: Test User, Branch: HN",
            Notes = "Invalid negative withdrawal"
        };

        /// <summary>
        /// Withdrawal request with invalid currency
        /// </summary>
        public static CreateWithdrawalRequest InvalidCurrencyRequest() => new()
        {
            Amount = 500.00m,
            CurrencyCode = "EUR", // Not supported
            WithdrawalMethodDetails = "Bank: VCB, Account: 0012300456, Name: Test User, Branch: HN",
            Notes = "Withdrawal with unsupported currency"
        };

        /// <summary>
        /// Withdrawal request with empty withdrawal method details
        /// </summary>
        public static CreateWithdrawalRequest EmptyWithdrawalMethodRequest() => new()
        {
            Amount = 500.00m,
            CurrencyCode = "USD",
            WithdrawalMethodDetails = "",
            Notes = "Missing withdrawal details"
        };

        /// <summary>
        /// Withdrawal request with too long withdrawal method details
        /// </summary>
        public static CreateWithdrawalRequest TooLongWithdrawalMethodRequest() => new()
        {
            Amount = 500.00m,
            CurrencyCode = "USD",
            WithdrawalMethodDetails = new string('A', 1001), // Exceeds 1000 characters
            Notes = "Withdrawal with too long details"
        };

        /// <summary>
        /// Withdrawal request with too long notes
        /// </summary>
        public static CreateWithdrawalRequest TooLongNotesRequest() => new()
        {
            Amount = 500.00m,
            CurrencyCode = "USD",
            WithdrawalMethodDetails = "Bank: VCB, Account: 0012300456, Name: Test User, Branch: HN",
            Notes = new string('N', 501) // Exceeds 500 characters
        };

        /// <summary>
        /// Withdrawal request without notes
        /// </summary>
        public static CreateWithdrawalRequest RequestWithoutNotes() => new()
        {
            Amount = 500.00m,
            CurrencyCode = "USD",
            WithdrawalMethodDetails = "Bank: VCB, Account: 0012300456, Name: Test User, Branch: HN",
            Notes = null
        };

        /// <summary>
        /// Withdrawal request with amount exceeding balance
        /// </summary>
        public static CreateWithdrawalRequest AmountExceedingBalanceRequest() => new()
        {
            Amount = 100000.00m, // Very large amount
            CurrencyCode = "USD",
            WithdrawalMethodDetails = "Bank: VCB, Account: 0012300456, Name: Test User, Branch: HN",
            Notes = "Amount exceeding wallet balance"
        };

        /// <summary>
        /// Valid withdrawal request response DTO
        /// </summary>
        public static WithdrawalRequestDto ValidResponse() => new()
        {
            WithdrawalRequestId = 3001,
            UserId = 1,
            Amount = 500.00m,
            CurrencyCode = "USD",
            Status = "PendingAdminApproval",
            WithdrawalMethodDetails = "Bank: VCB, Account: 0012300456, Name: Test User, Branch: HN",
            Notes = "Withdrawal for personal use",
            RequestedAt = DateTime.UtcNow
        };

        /// <summary>
        /// Withdrawal request response for large amount
        /// </summary>
        public static WithdrawalRequestDto LargeAmountResponse() => new()
        {
            WithdrawalRequestId = 3002,
            UserId = 1,
            Amount = 50000.00m,
            CurrencyCode = "USD",
            Status = "PendingAdminApproval",
            WithdrawalMethodDetails = "Bank: VCB, Account: 0012300456, Name: Test User, Branch: HN",
            Notes = "Large withdrawal for business",
            RequestedAt = DateTime.UtcNow
        };

        /// <summary>
        /// Withdrawal request response without notes
        /// </summary>
        public static WithdrawalRequestDto ResponseWithoutNotes() => new()
        {
            WithdrawalRequestId = 3003,
            UserId = 1,
            Amount = 500.00m,
            CurrencyCode = "USD",
            Status = "PendingAdminApproval",
            WithdrawalMethodDetails = "Bank: VCB, Account: 0012300456, Name: Test User, Branch: HN",
            Notes = null,
            RequestedAt = DateTime.UtcNow
        };

        /// <summary>
        /// Custom withdrawal request
        /// </summary>
        public static CreateWithdrawalRequest CustomRequest(decimal amount, string currencyCode, string withdrawalDetails, string? notes = null) => new()
        {
            Amount = amount,
            CurrencyCode = currencyCode,
            WithdrawalMethodDetails = withdrawalDetails,
            Notes = notes
        };

        /// <summary>
        /// Custom withdrawal response
        /// </summary>
        public static WithdrawalRequestDto CustomResponse(long requestId, int userId, decimal amount, string status = "PendingAdminApproval") => new()
        {
            WithdrawalRequestId = requestId,
            UserId = userId,
            Amount = amount,
            CurrencyCode = "USD",
            Status = status,
            WithdrawalMethodDetails = "Bank: VCB, Account: 0012300456, Name: Test User, Branch: HN",
            Notes = "Custom withdrawal request",
            RequestedAt = DateTime.UtcNow
        };
    }

    public static class VerifyRecipient
    {
        /// <summary>
        /// Valid recipient verification request
        /// </summary>
        public static VerifyRecipientRequest ValidRequest() => new()
        {
            RecipientEmail = "recipient@example.com"
        };

        /// <summary>
        /// Valid recipient verification request with different email
        /// </summary>
        public static VerifyRecipientRequest ValidAlternativeRequest() => new()
        {
            RecipientEmail = "user_b@example.com"
        };

        /// <summary>
        /// Request with empty email
        /// </summary>
        public static VerifyRecipientRequest EmptyEmailRequest() => new()
        {
            RecipientEmail = ""
        };

        /// <summary>
        /// Request with invalid email format
        /// </summary>
        public static VerifyRecipientRequest InvalidEmailFormatRequest() => new()
        {
            RecipientEmail = "invalid-email-format"
        };

        /// <summary>
        /// Request with non-existent email
        /// </summary>
        public static VerifyRecipientRequest NonExistentEmailRequest() => new()
        {
            RecipientEmail = "nonexistent@example.com"
        };

        /// <summary>
        /// Request with self email (for testing self-transfer prevention)
        /// </summary>
        public static VerifyRecipientRequest SelfEmailRequest() => new()
        {
            RecipientEmail = "testuser@example.com"
        };

        /// <summary>
        /// Request with inactive user email
        /// </summary>
        public static VerifyRecipientRequest InactiveUserEmailRequest() => new()
        {
            RecipientEmail = "inactive@example.com"
        };

        /// <summary>
        /// Request with too long email
        /// </summary>
        public static VerifyRecipientRequest TooLongEmailRequest() => new()
        {
            RecipientEmail = new string('a', 250) + "@example.com" // Over 255 chars
        };

        /// <summary>
        /// Valid recipient info response
        /// </summary>
        public static RecipientInfoResponse ValidResponse() => new()
        {
            RecipientUserId = 2,
            RecipientUsername = "recipient_user",
            RecipientFullName = "Recipient Full Name"
        };

        /// <summary>
        /// Alternative valid recipient info response
        /// </summary>
        public static RecipientInfoResponse AlternativeValidResponse() => new()
        {
            RecipientUserId = 3,
            RecipientUsername = "user_b",
            RecipientFullName = "User B Full Name"
        };

        /// <summary>
        /// Recipient info response without full name
        /// </summary>
        public static RecipientInfoResponse ResponseWithoutFullName() => new()
        {
            RecipientUserId = 4,
            RecipientUsername = "minimal_user",
            RecipientFullName = null
        };
    }

    public static class ExecuteInternalTransfer
    {
        /// <summary>
        /// Valid internal transfer request
        /// </summary>
        public static ExecuteInternalTransferRequest ValidRequest() => new()
        {
            RecipientUserId = 2,
            Amount = 100.00m,
            CurrencyCode = "USD",
            Description = "Transfer for lunch payment"
        };

        /// <summary>
        /// Large amount transfer request
        /// </summary>
        public static ExecuteInternalTransferRequest LargeAmountRequest() => new()
        {
            RecipientUserId = 2,
            Amount = 5000.00m,
            CurrencyCode = "USD",
            Description = "Large transfer for business payment"
        };

        /// <summary>
        /// Minimum amount transfer request
        /// </summary>
        public static ExecuteInternalTransferRequest MinimumAmountRequest() => new()
        {
            RecipientUserId = 2,
            Amount = 0.01m,
            CurrencyCode = "USD",
            Description = "Minimum transfer test"
        };

        /// <summary>
        /// Transfer without description
        /// </summary>
        public static ExecuteInternalTransferRequest RequestWithoutDescription() => new()
        {
            RecipientUserId = 2,
            Amount = 100.00m,
            CurrencyCode = "USD",
            Description = null
        };

        /// <summary>
        /// Transfer request with zero amount (invalid)
        /// </summary>
        public static ExecuteInternalTransferRequest ZeroAmountRequest() => new()
        {
            RecipientUserId = 2,
            Amount = 0.00m,
            CurrencyCode = "USD",
            Description = "Invalid zero transfer"
        };

        /// <summary>
        /// Transfer request with negative amount (invalid)
        /// </summary>
        public static ExecuteInternalTransferRequest NegativeAmountRequest() => new()
        {
            RecipientUserId = 2,
            Amount = -50.00m,
            CurrencyCode = "USD",
            Description = "Invalid negative transfer"
        };

        /// <summary>
        /// Transfer request with invalid recipient ID
        /// </summary>
        public static ExecuteInternalTransferRequest InvalidRecipientIdRequest() => new()
        {
            RecipientUserId = -1,
            Amount = 100.00m,
            CurrencyCode = "USD",
            Description = "Transfer to invalid recipient"
        };

        /// <summary>
        /// Transfer request with zero recipient ID
        /// </summary>
        public static ExecuteInternalTransferRequest ZeroRecipientIdRequest() => new()
        {
            RecipientUserId = 0,
            Amount = 100.00m,
            CurrencyCode = "USD",
            Description = "Transfer to zero recipient ID"
        };

        /// <summary>
        /// Transfer request with invalid currency
        /// </summary>
        public static ExecuteInternalTransferRequest InvalidCurrencyRequest() => new()
        {
            RecipientUserId = 2,
            Amount = 100.00m,
            CurrencyCode = "EUR", // Not supported
            Description = "Transfer with unsupported currency"
        };

        /// <summary>
        /// Transfer request with too long description
        /// </summary>
        public static ExecuteInternalTransferRequest TooLongDescriptionRequest() => new()
        {
            RecipientUserId = 2,
            Amount = 100.00m,
            CurrencyCode = "USD",
            Description = new string('D', 501) // Exceeds 500 characters
        };

        /// <summary>
        /// Self-transfer request (invalid)
        /// </summary>
        public static ExecuteInternalTransferRequest SelfTransferRequest() => new()
        {
            RecipientUserId = 1, // Same as sender
            Amount = 100.00m,
            CurrencyCode = "USD",
            Description = "Invalid self-transfer"
        };

        /// <summary>
        /// Transfer request with amount exceeding balance
        /// </summary>
        public static ExecuteInternalTransferRequest AmountExceedingBalanceRequest() => new()
        {
            RecipientUserId = 2,
            Amount = 100000.00m, // Very large amount
            CurrencyCode = "USD",
            Description = "Amount exceeding sender balance"
        };

        /// <summary>
        /// Transfer to inactive user
        /// </summary>
        public static ExecuteInternalTransferRequest TransferToInactiveUserRequest() => new()
        {
            RecipientUserId = 999, // Inactive user
            Amount = 100.00m,
            CurrencyCode = "USD",
            Description = "Transfer to inactive user"
        };

        /// <summary>
        /// Transfer to non-existent user
        /// </summary>
        public static ExecuteInternalTransferRequest TransferToNonExistentUserRequest() => new()
        {
            RecipientUserId = 99999, // Non-existent user
            Amount = 100.00m,
            CurrencyCode = "USD",
            Description = "Transfer to non-existent user"
        };

        /// <summary>
        /// Valid sender transaction response DTO
        /// </summary>
        public static WalletTransactionDto ValidSenderTransactionResponse() => new()
        {
            TransactionId = 4001,
            TransactionTypeName = "Internal Transfer",
            Amount = -100.00m, // Negative for sender
            CurrencyCode = "USD",
            BalanceAfter = 1400.00m,
            ReferenceId = "FINIXTRF202401001",
            PaymentMethod = "Internal",
            Description = "Transfer to recipient@example.com",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow
        };

        /// <summary>
        /// Large amount sender transaction response DTO
        /// </summary>
        public static WalletTransactionDto LargeAmountSenderTransactionResponse() => new()
        {
            TransactionId = 4002,
            TransactionTypeName = "Internal Transfer",
            Amount = -5000.00m, // Negative for sender
            CurrencyCode = "USD",
            BalanceAfter = 45000.00m,
            ReferenceId = "FINIXTRF202401002",
            PaymentMethod = "Internal",
            Description = "Large business transfer to recipient@example.com",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow
        };

        /// <summary>
        /// Minimum amount sender transaction response DTO
        /// </summary>
        public static WalletTransactionDto MinimumAmountSenderTransactionResponse() => new()
        {
            TransactionId = 4003,
            TransactionTypeName = "Internal Transfer",
            Amount = -0.01m, // Negative for sender
            CurrencyCode = "USD",
            BalanceAfter = 999.99m,
            ReferenceId = "FINIXTRF202401003",
            PaymentMethod = "Internal",
            Description = "Minimum transfer test to recipient@example.com",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow
        };

        /// <summary>
        /// Transaction response without description
        /// </summary>
        public static WalletTransactionDto TransactionResponseWithoutDescription() => new()
        {
            TransactionId = 4004,
            TransactionTypeName = "Internal Transfer",
            Amount = -100.00m, // Negative for sender
            CurrencyCode = "USD",
            BalanceAfter = 1400.00m,
            ReferenceId = "FINIXTRF202401004",
            PaymentMethod = "Internal",
            Description = null,
            Status = "Completed",
            TransactionDate = DateTime.UtcNow
        };

        /// <summary>
        /// Custom internal transfer request
        /// </summary>
        public static ExecuteInternalTransferRequest CustomRequest(int recipientUserId, decimal amount, string currencyCode, string? description = null) => new()
        {
            RecipientUserId = recipientUserId,
            Amount = amount,
            CurrencyCode = currencyCode,
            Description = description
        };

        /// <summary>
        /// Custom sender transaction response
        /// </summary>
        public static WalletTransactionDto CustomSenderTransactionResponse(long transactionId, decimal amount, decimal balanceAfter, int recipientUserId, string? description = null) => new()
        {
            TransactionId = transactionId,
            TransactionTypeName = "Internal Transfer",
            Amount = -amount, // Negative for sender
            CurrencyCode = "USD",
            BalanceAfter = balanceAfter,
            ReferenceId = $"FINIXTRF{DateTime.UtcNow:yyyyMMdd}{transactionId:D3}",
            PaymentMethod = "Internal",
            Description = description ?? $"Transfer to user {recipientUserId}",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow
        };
    }

    // SCRUM-76: Test data for GET /admin/wallets/deposits/bank/pending-confirmation endpoint testing
    public static class AdminPendingBankDeposits
    {
        /// <summary>
        /// Valid query for getting pending bank deposits
        /// </summary>
        public static GetAdminPendingBankDepositsQuery ValidQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "TransactionDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with date range filtering
        /// </summary>
        public static GetAdminPendingBankDepositsQuery QueryWithDateRange() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "TransactionDate",
            SortOrder = "desc",
            DateFrom = DateTime.UtcNow.AddDays(-30),
            DateTo = DateTime.UtcNow
        };

        /// <summary>
        /// Query with user filtering
        /// </summary>
        public static GetAdminPendingBankDepositsQuery QueryWithUserFilter() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "TransactionDate",
            SortOrder = "desc",
            UserId = 1,
            UsernameOrEmail = "testuser"
        };

        /// <summary>
        /// Query with amount range filtering
        /// </summary>
        public static GetAdminPendingBankDepositsQuery QueryWithAmountFilter() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "AmountUSD",
            SortOrder = "desc",
            MinAmountUSD = 100.00m,
            MaxAmountUSD = 5000.00m
        };

        /// <summary>
        /// Query with reference code filtering
        /// </summary>
        public static GetAdminPendingBankDepositsQuery QueryWithReferenceFilter() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "TransactionDate",
            SortOrder = "desc",
            ReferenceCode = "DEP123"
        };

        /// <summary>
        /// Valid pending bank deposit response
        /// </summary>
        public static List<AdminPendingBankDepositDto> ValidPendingDepositsResponse() => new()
        {
            new()
            {
                TransactionId = 1001,
                UserId = 1,
                Username = "testuser123",
                UserEmail = "test@example.com",
                AmountUSD = 1000.00m,
                CurrencyCode = "USD",
                AmountVND = 24000000m,
                ExchangeRate = 24000m,
                ReferenceCode = "DEP001",
                PaymentMethod = "Bank Transfer",
                Status = "Pending",
                TransactionDate = DateTime.UtcNow.AddHours(-2),
                UpdatedAt = DateTime.UtcNow.AddHours(-2),
                Description = "Bank deposit via ACH transfer"
            },
            new()
            {
                TransactionId = 1002,
                UserId = 2,
                Username = "investor456",
                UserEmail = "investor@example.com",
                AmountUSD = 2500.00m,
                CurrencyCode = "USD",
                AmountVND = 60000000m,
                ExchangeRate = 24000m,
                ReferenceCode = "DEP002",
                PaymentMethod = "Wire Transfer",
                Status = "Pending",
                TransactionDate = DateTime.UtcNow.AddHours(-5),
                UpdatedAt = DateTime.UtcNow.AddHours(-5),
                Description = "International wire transfer"
            }
        };

        /// <summary>
        /// Empty pending deposits response
        /// </summary>
        public static List<AdminPendingBankDepositDto> EmptyPendingDepositsResponse() => new();

        /// <summary>
        /// Single pending deposit response
        /// </summary>
        public static List<AdminPendingBankDepositDto> SinglePendingDepositResponse() => new()
        {
            new()
            {
                TransactionId = 1003,
                UserId = 3,
                Username = "newuser789",
                UserEmail = "newuser@example.com",
                AmountUSD = 500.00m,
                CurrencyCode = "USD",
                AmountVND = 12000000m,
                ExchangeRate = 24000m,
                ReferenceCode = "DEP003",
                PaymentMethod = "Bank Transfer",
                Status = "Pending",
                TransactionDate = DateTime.UtcNow.AddMinutes(-30),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-30),
                Description = "First time deposit"
            }
        };

        /// <summary>
        /// Pending deposits with different currencies and amounts
        /// </summary>
        public static List<AdminPendingBankDepositDto> PendingDepositsWithVariedAmounts() => new()
        {
            new()
            {
                TransactionId = 1004,
                UserId = 4,
                Username = "bigspender",
                UserEmail = "bigspender@example.com",
                AmountUSD = 10000.00m,
                CurrencyCode = "USD",
                AmountVND = 240000000m,
                ExchangeRate = 24000m,
                ReferenceCode = "DEP004",
                PaymentMethod = "Wire Transfer",
                Status = "Pending",
                TransactionDate = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                Description = "Large investment deposit"
            },
            new()
            {
                TransactionId = 1005,
                UserId = 5,
                Username = "smallinvestor",
                UserEmail = "small@example.com",
                AmountUSD = 50.00m,
                CurrencyCode = "USD",
                AmountVND = 1200000m,
                ExchangeRate = 24000m,
                ReferenceCode = "DEP005",
                PaymentMethod = "Bank Transfer",
                Status = "Pending",
                TransactionDate = DateTime.UtcNow.AddMinutes(-10),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-10),
                Description = "Small test deposit"
            }
        };

        /// <summary>
        /// Custom pending deposit item
        /// </summary>
        public static AdminPendingBankDepositDto CustomPendingDeposit(
            long transactionId,
            int userId,
            string username,
            string email,
            decimal amountUSD,
            string referenceCode) => new()
        {
            TransactionId = transactionId,
            UserId = userId,
            Username = username,
            UserEmail = email,
            AmountUSD = amountUSD,
            CurrencyCode = "USD",
            AmountVND = amountUSD * 24000,
            ExchangeRate = 24000m,
            ReferenceCode = referenceCode,
            PaymentMethod = "Bank Transfer",
            Status = "Pending",
            TransactionDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Description = "Custom test deposit"
        };
    }

    // SCRUM-77: Test data for POST /admin/wallets/deposits/bank/cancel endpoint testing
    /// <summary>
    /// Test data builder for CancelBankDeposit functionality
    /// Provides comprehensive test data for unit testing the bank deposit cancellation feature.
    /// 
    /// This class supports testing of:
    /// - Valid cancellation requests (happy path scenarios)
    /// - Validation error scenarios (empty notes, too long notes, invalid transaction IDs)
    /// - Business logic scenarios (non-existent transactions, already processed deposits)
    /// - Response DTOs for successful cancellations
    /// 
    /// Usage:
    /// - WalletsTestDataBuilder.CancelBankDeposit.ValidRequest() - Returns valid cancellation request
    /// - WalletsTestDataBuilder.CancelBankDeposit.ValidCancelledTransactionDto() - Returns expected response
    /// - Various error scenario methods for comprehensive testing coverage
    /// </summary>
    public static class CancelBankDeposit
    {
        /// <summary>
        /// Valid cancel bank deposit request for happy path testing
        /// Contains valid transaction ID and admin notes within acceptable limits
        /// </summary>
        public static CancelBankDepositRequest ValidRequest() => new()
        {
            TransactionId = 1001,
            AdminNotes = "User requested cancellation due to error in amount"
        };

        /// <summary>
        /// Cancel request with empty admin notes (invalid)
        /// </summary>
        public static CancelBankDepositRequest RequestWithEmptyNotes() => new()
        {
            TransactionId = 1001,
            AdminNotes = ""
        };

        /// <summary>
        /// Cancel request with admin notes too long (invalid)
        /// </summary>
        public static CancelBankDepositRequest RequestWithTooLongAdminNotes() => new()
        {
            TransactionId = 1001,
            AdminNotes = new string('A', 501) // 501 characters - exceeds limit
        };

        /// <summary>
        /// Cancel request with empty admin notes (invalid)
        /// </summary>
        public static CancelBankDepositRequest RequestWithEmptyAdminNotes() => new()
        {
            TransactionId = 1001,
            AdminNotes = ""
        };

        /// <summary>
        /// Cancel request with invalid transaction ID
        /// </summary>
        public static CancelBankDepositRequest RequestWithInvalidTransactionId() => new()
        {
            TransactionId = -1,
            AdminNotes = "Attempting to cancel with invalid transaction ID"
        };

        /// <summary>
        /// Cancel request for non-existent transaction
        /// </summary>
        public static CancelBankDepositRequest RequestForNonExistentTransaction() => new()
        {
            TransactionId = 99999,
            AdminNotes = "Attempting to cancel non-existent transaction"
        };

        /// <summary>
        /// Cancel request for already confirmed deposit
        /// </summary>
        public static CancelBankDepositRequest RequestForAlreadyConfirmedDeposit() => new()
        {
            TransactionId = 1002,
            AdminNotes = "Attempting to cancel already confirmed deposit"
        };

        /// <summary>
        /// Cancel request for already cancelled deposit
        /// </summary>
        public static CancelBankDepositRequest RequestForAlreadyCancelledDeposit() => new()
        {
            TransactionId = 1003,
            AdminNotes = "Attempting to cancel already cancelled deposit"
        };

        /// <summary>
        /// Valid cancelled transaction DTO response
        /// </summary>
        public static WalletTransactionDto ValidCancelledTransactionDto() => new()
        {
            TransactionId = 1001,
            TransactionTypeName = "Bank Deposit",
            Amount = 1000.00m,
            CurrencyCode = "USD",
            BalanceAfter = 0.00m, // Balance unchanged due to cancellation
            ReferenceId = "FINIXDEP202401001",
            PaymentMethod = "Bank Transfer",
            Description = "Cancelled: User requested cancellation due to error in amount",
            Status = "Cancelled",
            TransactionDate = DateTime.UtcNow.AddHours(-2)
        };

        /// <summary>
        /// Custom cancel bank deposit request
        /// </summary>
        public static CancelBankDepositRequest CustomRequest(long transactionId, string adminNotes) => new()
        {
            TransactionId = transactionId,
            AdminNotes = adminNotes
        };
    }

    // SCRUM-78: Test data for POST /admin/wallets/deposits/direct endpoint testing
    /// <summary>
    /// Test data builder for AdminDirectDeposit functionality
    /// Provides comprehensive test data for unit testing the admin direct deposit feature.
    /// 
    /// This class supports testing of:
    /// - Valid direct deposit requests (happy path scenarios)
    /// - Validation error scenarios (invalid user IDs, negative amounts, empty notes)
    /// - Business logic scenarios (non-existent users, inactive users, excessive amounts)
    /// - Response DTOs for successful deposits
    /// 
    /// Usage:
    /// - WalletsTestDataBuilder.AdminDirectDeposit.ValidRequest() - Returns valid direct deposit request
    /// - WalletsTestDataBuilder.AdminDirectDeposit.ValidTransactionDto() - Returns expected response
    /// - Various error scenario methods for comprehensive testing coverage
    /// </summary>
    public static class AdminDirectDeposit
    {
        /// <summary>
        /// Valid admin direct deposit request for happy path testing
        /// Contains valid user ID, amount, currency and admin notes within acceptable limits
        /// </summary>
        public static AdminDirectDepositRequest ValidRequest() => new()
        {
            UserId = 1,
            Amount = 1000.00m,
            CurrencyCode = "USD",
            Description = "Direct deposit for promotional bonus"
        };

        /// <summary>
        /// Large amount direct deposit request
        /// </summary>
        public static AdminDirectDepositRequest LargeAmountRequest() => new()
        {
            UserId = 1,
            Amount = 50000.00m,
            CurrencyCode = "USD",
            Description = "Large direct deposit for institutional investor"
        };

        /// <summary>
        /// Small amount direct deposit request
        /// </summary>
        public static AdminDirectDepositRequest SmallAmountRequest() => new()
        {
            UserId = 1,
            Amount = 0.01m,
            CurrencyCode = "USD",
            Description = "Micro adjustment direct deposit"
        };

        /// <summary>
        /// Direct deposit request with zero amount (invalid)
        /// </summary>
        public static AdminDirectDepositRequest ZeroAmountRequest() => new()
        {
            UserId = 1,
            Amount = 0.00m,
            CurrencyCode = "USD",
            Description = "Invalid zero amount deposit"
        };

        /// <summary>
        /// Direct deposit request with negative amount (invalid)
        /// </summary>
        public static AdminDirectDepositRequest NegativeAmountRequest() => new()
        {
            UserId = 1,
            Amount = -500.00m,
            CurrencyCode = "USD",
            Description = "Invalid negative amount deposit"
        };

        /// <summary>
        /// Direct deposit request with invalid user ID (zero)
        /// </summary>
        public static AdminDirectDepositRequest InvalidUserIdZeroRequest() => new()
        {
            UserId = 0,
            Amount = 1000.00m,
            CurrencyCode = "USD",
            Description = "Deposit for invalid user ID zero"
        };

        /// <summary>
        /// Direct deposit request with invalid user ID (negative)
        /// </summary>
        public static AdminDirectDepositRequest InvalidUserIdNegativeRequest() => new()
        {
            UserId = -1,
            Amount = 1000.00m,
            CurrencyCode = "USD",
            Description = "Deposit for invalid negative user ID"
        };

        /// <summary>
        /// Direct deposit request for non-existent user
        /// </summary>
        public static AdminDirectDepositRequest NonExistentUserRequest() => new()
        {
            UserId = 99999,
            Amount = 1000.00m,
            CurrencyCode = "USD",
            Description = "Deposit for non-existent user"
        };

        /// <summary>
        /// Direct deposit request with invalid currency code
        /// </summary>
        public static AdminDirectDepositRequest InvalidCurrencyRequest() => new()
        {
            UserId = 1,
            Amount = 1000.00m,
            CurrencyCode = "EUR", // Not supported
            Description = "Deposit with unsupported currency"
        };

        /// <summary>
        /// Direct deposit request with empty admin notes (invalid)
        /// </summary>
        public static AdminDirectDepositRequest EmptyAdminNotesRequest() => new()
        {
            UserId = 1,
            Amount = 1000.00m,
            CurrencyCode = "USD",
            Description = ""
        };

        /// <summary>
        /// Direct deposit request with too long admin notes (invalid)
        /// </summary>
        public static AdminDirectDepositRequest TooLongAdminNotesRequest() => new()
        {
            UserId = 1,
            Amount = 1000.00m,
            CurrencyCode = "USD",
            Description = new string('A', 501) // 501 characters - exceeds limit
        };

        /// <summary>
        /// Direct deposit request for inactive user
        /// </summary>
        public static AdminDirectDepositRequest InactiveUserRequest() => new()
        {
            UserId = 999, // Assume this is inactive user
            Amount = 1000.00m,
            CurrencyCode = "USD",
            Description = "Deposit for inactive user account"
        };

        /// <summary>
        /// Valid direct deposit transaction DTO response
        /// </summary>
        public static WalletTransactionDto ValidTransactionDto() => new()
        {
            TransactionId = 5001,
            TransactionTypeName = "Admin Direct Deposit",
            Amount = 1000.00m,
            CurrencyCode = "USD",
            BalanceAfter = 2500.00m,
            ReferenceId = "FINIXADM202401001",
            PaymentMethod = "Admin Direct",
            Description = "Admin: Direct deposit for promotional bonus",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow
        };

        /// <summary>
        /// Large amount direct deposit transaction DTO response
        /// </summary>
        public static WalletTransactionDto LargeAmountTransactionDto() => new()
        {
            TransactionId = 5002,
            TransactionTypeName = "Admin Direct Deposit",
            Amount = 50000.00m,
            CurrencyCode = "USD",
            BalanceAfter = 75000.00m,
            ReferenceId = "FINIXADM202401002",
            PaymentMethod = "Admin Direct",
            Description = "Admin: Large direct deposit for institutional investor",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow
        };

        /// <summary>
        /// Small amount direct deposit transaction DTO response
        /// </summary>
        public static WalletTransactionDto SmallAmountTransactionDto() => new()
        {
            TransactionId = 5003,
            TransactionTypeName = "Admin Direct Deposit",
            Amount = 0.01m,
            CurrencyCode = "USD",
            BalanceAfter = 1000.01m,
            ReferenceId = "FINIXADM202401003",
            PaymentMethod = "Admin Direct",
            Description = "Admin: Micro adjustment direct deposit",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow
        };

        /// <summary>
        /// Custom admin direct deposit request
        /// </summary>
        public static AdminDirectDepositRequest CustomRequest(int userId, decimal amount, string currencyCode, string adminNotes) => new()
        {
            UserId = userId,
            Amount = amount,
            CurrencyCode = currencyCode,
            Description = adminNotes
        };

        /// <summary>
        /// Custom admin direct deposit transaction DTO
        /// </summary>
        public static WalletTransactionDto CustomTransactionDto(long transactionId, decimal amount, decimal balanceAfter, string adminNotes) => new()
        {
            TransactionId = transactionId,
            TransactionTypeName = "Admin Direct Deposit",
            Amount = amount,
            CurrencyCode = "USD",
            BalanceAfter = balanceAfter,
            ReferenceId = $"FINIXADM{DateTime.UtcNow:yyyyMMdd}{transactionId:D3}",
            PaymentMethod = "Admin Direct",
            Description = $"Admin: {adminNotes}",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow
        };
    }

    // SCRUM-79 & SCRUM-80: Test data for admin withdrawal management endpoint testing
    /// <summary>
    /// Test data builder for Admin Withdrawal Management functionality
    /// Provides comprehensive test data for unit testing admin withdrawal approval and rejection features.
    /// 
    /// This class supports testing of:
    /// - Valid withdrawal approval requests (happy path scenarios)
    /// - Valid withdrawal rejection requests (happy path scenarios)
    /// - Validation error scenarios (invalid withdrawal IDs, empty admin notes)
    /// - Business logic scenarios (non-existent withdrawals, already processed withdrawals)
    /// - Response DTOs for approved and rejected withdrawals
    /// 
    /// Usage:
    /// - WalletsTestDataBuilder.AdminWithdrawals.ApprovalRequest() - Returns valid approval request
    /// - WalletsTestDataBuilder.AdminWithdrawals.RejectionRequest() - Returns valid rejection request
    /// - WalletsTestDataBuilder.AdminWithdrawals.ApprovedTransactionDto() - Returns expected approval response
    /// - Various error scenario methods for comprehensive testing coverage
    /// </summary>
    public static class AdminWithdrawals
    {
        /// <summary>
        /// Valid approve withdrawal request for happy path testing
        /// Contains valid withdrawal request ID and admin notes within acceptable limits
        /// </summary>
        public static ApproveWithdrawalRequest ValidApprovalRequest() => new()
        {
            TransactionId = 3001,
            AdminNotes = "Withdrawal approved after verification of bank details"
        };

        /// <summary>
        /// Valid reject withdrawal request for happy path testing
        /// Contains valid withdrawal request ID and admin notes within acceptable limits
        /// </summary>
        public static RejectWithdrawalRequest ValidRejectionRequest() => new()
        {
            TransactionId = 3002,
            AdminNotes = "Withdrawal rejected due to insufficient documentation"
        };

        /// <summary>
        /// Approve withdrawal request with empty admin notes (invalid)
        /// </summary>
        public static ApproveWithdrawalRequest ApprovalRequestWithEmptyNotes() => new()
        {
            TransactionId = 3001,
            AdminNotes = ""
        };

        /// <summary>
        /// Reject withdrawal request with empty admin notes (invalid)
        /// </summary>
        public static RejectWithdrawalRequest RejectionRequestWithEmptyNotes() => new()
        {
            TransactionId = 3002,
            AdminNotes = ""
        };

        /// <summary>
        /// Approve withdrawal request with too long admin notes (invalid)
        /// </summary>
        public static ApproveWithdrawalRequest ApprovalRequestWithTooLongNotes() => new()
        {
            TransactionId = 3001,
            AdminNotes = new string('A', 501) // 501 characters - exceeds limit
        };

        /// <summary>
        /// Reject withdrawal request with too long admin notes (invalid)
        /// </summary>
        public static RejectWithdrawalRequest RejectionRequestWithTooLongNotes() => new()
        {
            TransactionId = 3002,
            AdminNotes = new string('R', 501) // 501 characters - exceeds limit
        };

        /// <summary>
        /// Approve withdrawal request with invalid withdrawal request ID (zero)
        /// </summary>
        public static ApproveWithdrawalRequest ApprovalRequestWithZeroId() => new()
        {
            TransactionId = 0,
            AdminNotes = "Attempting to approve withdrawal with zero ID"
        };

        /// <summary>
        /// Reject withdrawal request with invalid withdrawal request ID (negative)
        /// </summary>
        public static RejectWithdrawalRequest RejectionRequestWithNegativeId() => new()
        {
            TransactionId = -1,
            AdminNotes = "Attempting to reject withdrawal with negative ID"
        };

        /// <summary>
        /// Approve withdrawal request for non-existent withdrawal
        /// </summary>
        public static ApproveWithdrawalRequest ApprovalRequestForNonExistentWithdrawal() => new()
        {
            TransactionId = 99999,
            AdminNotes = "Attempting to approve non-existent withdrawal"
        };

        /// <summary>
        /// Reject withdrawal request for non-existent withdrawal
        /// </summary>
        public static RejectWithdrawalRequest RejectionRequestForNonExistentWithdrawal() => new()
        {
            TransactionId = 99999,
            AdminNotes = "Attempting to reject non-existent withdrawal"
        };

        /// <summary>
        /// Approve withdrawal request for already processed withdrawal
        /// </summary>
        public static ApproveWithdrawalRequest ApprovalRequestForAlreadyProcessedWithdrawal() => new()
        {
            TransactionId = 3003,
            AdminNotes = "Attempting to approve already processed withdrawal"
        };

        /// <summary>
        /// Reject withdrawal request for already processed withdrawal
        /// </summary>
        public static RejectWithdrawalRequest RejectionRequestForAlreadyProcessedWithdrawal() => new()
        {
            TransactionId = 3004,
            AdminNotes = "Attempting to reject already processed withdrawal"
        };

        /// <summary>
        /// Valid approved withdrawal transaction DTO response
        /// </summary>
        public static WalletTransactionDto ValidApprovedTransactionDto() => new()
        {
            TransactionId = 6001,
            TransactionTypeName = "Withdrawal",
            Amount = -500.00m,
            CurrencyCode = "USD",
            BalanceAfter = 1500.00m,
            ReferenceId = "FINIXWTH202401001",
            PaymentMethod = "Bank Transfer",
            Description = "Approved: Withdrawal approved after verification of bank details",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow
        };

        /// <summary>
        /// Valid rejected withdrawal transaction DTO response
        /// </summary>
        public static WalletTransactionDto ValidRejectedTransactionDto() => new()
        {
            TransactionId = 6002,
            TransactionTypeName = "Withdrawal",
            Amount = -750.00m,
            CurrencyCode = "USD",
            BalanceAfter = 2000.00m, // Balance unchanged due to rejection
            ReferenceId = "FINIXWTH202401002",
            PaymentMethod = "Bank Transfer",
            Description = "Rejected: Withdrawal rejected due to insufficient documentation",
            Status = "Rejected",
            TransactionDate = DateTime.UtcNow
        };

        /// <summary>
        /// Large amount approved withdrawal transaction DTO response
        /// </summary>
        public static WalletTransactionDto LargeAmountApprovedTransactionDto() => new()
        {
            TransactionId = 6003,
            TransactionTypeName = "Withdrawal",
            Amount = -25000.00m,
            CurrencyCode = "USD",
            BalanceAfter = 25000.00m,
            ReferenceId = "FINIXWTH202401003",
            PaymentMethod = "Wire Transfer",
            Description = "Approved: Large withdrawal approved for business payment",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow
        };

        /// <summary>
        /// Small amount rejected withdrawal transaction DTO response
        /// </summary>
        public static WalletTransactionDto SmallAmountRejectedTransactionDto() => new()
        {
            TransactionId = 6004,
            TransactionTypeName = "Withdrawal",
            Amount = -50.00m,
            CurrencyCode = "USD",
            BalanceAfter = 1000.00m, // Balance unchanged due to rejection
            ReferenceId = "FINIXWTH202401004",
            PaymentMethod = "Bank Transfer",
            Description = "Rejected: Insufficient minimum withdrawal amount",
            Status = "Rejected",
            TransactionDate = DateTime.UtcNow
        };

        /// <summary>
        /// Custom approve withdrawal request
        /// </summary>
        public static ApproveWithdrawalRequest CustomApprovalRequest(long TransactionId, string adminNotes) => new()
        {
            TransactionId = TransactionId,
            AdminNotes = adminNotes
        };

        /// <summary>
        /// Custom reject withdrawal request
        /// </summary>
        public static RejectWithdrawalRequest CustomRejectionRequest(long TransactionId, string adminNotes) => new()
        {
            TransactionId = TransactionId,
            AdminNotes = adminNotes
        };

        /// <summary>
        /// Custom approved withdrawal transaction DTO
        /// </summary>
        public static WalletTransactionDto CustomApprovedTransactionDto(
            long transactionId,
            decimal amount,
            decimal balanceAfter,
            string adminNotes) => new()
        {
            TransactionId = transactionId,
            TransactionTypeName = "Withdrawal",
            Amount = -amount,
            CurrencyCode = "USD",
            BalanceAfter = balanceAfter,
            ReferenceId = $"FINIXWTH{DateTime.UtcNow:yyyyMMdd}{transactionId:D3}",
            PaymentMethod = "Bank Transfer",
            Description = $"Approved: {adminNotes}",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow
        };

        /// <summary>
        /// Custom rejected withdrawal transaction DTO
        /// </summary>
        public static WalletTransactionDto CustomRejectedTransactionDto(
            long transactionId,
            decimal originalAmount,
            decimal unchangedBalance,
            string adminNotes) => new()
        {
            TransactionId = transactionId,
            TransactionTypeName = "Withdrawal",
            Amount = -originalAmount,
            CurrencyCode = "USD",
            BalanceAfter = unchangedBalance, // Balance unchanged due to rejection
            ReferenceId = $"FINIXWTH{DateTime.UtcNow:yyyyMMdd}{transactionId:D3}",
            PaymentMethod = "Bank Transfer",
            Description = $"Rejected: {adminNotes}",
            Status = "Rejected",
            TransactionDate = DateTime.UtcNow
        };
    }
} 