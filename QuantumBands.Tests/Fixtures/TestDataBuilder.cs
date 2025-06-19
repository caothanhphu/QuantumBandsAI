using QuantumBands.Application.Features.Authentication.Commands.RegisterUser;
using QuantumBands.Application.Features.Authentication.Commands.Login;
using QuantumBands.Application.Features.Authentication.Commands.VerifyEmail;
using QuantumBands.Application.Features.Authentication.Commands.ForgotPassword;
using QuantumBands.Application.Features.Authentication.Commands.ResetPassword;
using QuantumBands.Application.Features.Authentication.Commands.ResendVerificationEmail;
using QuantumBands.Application.Features.Users.Commands.UpdateProfile;
using QuantumBands.Application.Features.Users.Commands.ChangePassword;
using QuantumBands.Application.Features.Authentication;
using QuantumBands.Application.Features.Wallets.Commands.BankDeposit;
using QuantumBands.Application.Features.Wallets.Commands.CreateWithdrawal;
using QuantumBands.Application.Features.Wallets.Commands.InternalTransfer;
using QuantumBands.Application.Features.Wallets.Commands.AdminActions;
using QuantumBands.Application.Features.Wallets.Dtos;
using QuantumBands.Application.Features.Exchange.Commands.CreateOrder;
using QuantumBands.Application.Features.Exchange.Dtos;
using QuantumBands.Application.Features.Exchange.Queries;
using QuantumBands.Application.Features.Users.Commands.Setup2FA;
using QuantumBands.Application.Features.Users.Commands.Enable2FA;
using QuantumBands.Application.Features.Users.Commands.Verify2FA;
using QuantumBands.Application.Features.Users.Commands.Disable2FA;
using QuantumBands.Application.Features.Wallets.Queries.GetTransactions;
using QuantumBands.Application.Features.TradingAccounts.Queries;
using QuantumBands.Application.Features.Admin.TradingAccounts.Dtos;
using QuantumBands.Application.Features.Admin.TradingAccounts.Commands;
using QuantumBands.Application.Features.TradingAccounts.Dtos;
using QuantumBands.Application.Features.Portfolio.Dtos;
using QuantumBands.Application.Common.Models;
using QuantumBands.Domain.Entities;

namespace QuantumBands.Tests.Fixtures;

public static class TestDataBuilder
{
















    // SCRUM-36: Test data for forgot password endpoint testing


    // SCRUM-37: Test data for reset password endpoint testing


    // SCRUM-35: Test data for refresh token endpoint testing















    // SCRUM-44: Test data for Exchange PlaceOrder endpoint testing
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
            OrderId = 12345,
            UserId = 1,
            TradingAccountId = 1,
            TradingAccountName = "Test Company Ltd",
            OrderSide = "Buy",
            OrderType = "Market",
            QuantityOrdered = 100,
            QuantityFilled = 0,
            LimitPrice = null,
            AverageFillPrice = null,
            OrderStatus = "Open",
            OrderDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TransactionFee = 5.00m
        };

        public static ShareOrderDto ValidLimitSellOrderResponse() => new()
        {
            OrderId = 12346,
            UserId = 1,
            TradingAccountId = 1,
            TradingAccountName = "Test Company Ltd",
            OrderSide = "Sell",
            OrderType = "Limit",
            QuantityOrdered = 50,
            QuantityFilled = 0,
            LimitPrice = 55.00m,
            AverageFillPrice = null,
            OrderStatus = "Open",
            OrderDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TransactionFee = 2.50m
        };

        public static ShareOrderDto PartiallyFilledOrderResponse() => new()
        {
            OrderId = 12347,
            UserId = 1,
            TradingAccountId = 1,
            TradingAccountName = "Test Company Ltd",
            OrderSide = "Buy",
            OrderType = "Market",
            QuantityOrdered = 100,
            QuantityFilled = 30,
            LimitPrice = null,
            AverageFillPrice = 52.50m,
            OrderStatus = "PartiallyFilled",
            OrderDate = DateTime.UtcNow.AddMinutes(-5),
            UpdatedAt = DateTime.UtcNow,
            TransactionFee = 7.50m
        };

        public static ShareOrderDto FilledOrderResponse() => new()
        {
            OrderId = 12348,
            UserId = 1,
            TradingAccountId = 1,
            TradingAccountName = "Test Company Ltd",
            OrderSide = "Sell",
            OrderType = "Limit",
            QuantityOrdered = 50,
            QuantityFilled = 50,
            LimitPrice = 55.00m,
            AverageFillPrice = 55.25m,
            OrderStatus = "Filled",
            OrderDate = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-1),
            TransactionFee = 5.25m
        };

        public static ShareOrderDto OrderWithHighFeeResponse() => new()
        {
            OrderId = 12349,
            UserId = 1,
            TradingAccountId = 1,
            TradingAccountName = "Test Company Ltd",
            OrderSide = "Buy",
            OrderType = "Market",
            QuantityOrdered = 999999,
            QuantityFilled = 0,
            LimitPrice = null,
            AverageFillPrice = null,
            OrderStatus = "Open",
            OrderDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TransactionFee = 49999.95m
        };

        public static ShareOrderDto OrderFromDifferentUserResponse() => new()
        {
            OrderId = 12350,
            UserId = 999, // Different user
            TradingAccountId = 2,
            TradingAccountName = "Another Company Ltd",
            OrderSide = "Buy",
            OrderType = "Limit",
            QuantityOrdered = 100,
            QuantityFilled = 0,
            LimitPrice = 45.00m,
            AverageFillPrice = null,
            OrderStatus = "Open",
            OrderDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TransactionFee = 4.50m
        };
    }

    // SCRUM-45: Test data for Setup2FA endpoint testing
    public static class Setup2FA
    {
        // Valid response scenarios for different 2FA setup cases
        public static Setup2FAResponse ValidSetup2FAResponse() => new()
        {
            SharedKey = "JBSWY3DPEHPK3PXP", // Standard Base32 test key
            AuthenticatorUri = "otpauth://totp/QuantumBands:testuser%40example.com?secret=JBSWY3DPEHPK3PXP&issuer=QuantumBands"
        };

        public static Setup2FAResponse ValidSetup2FAResponseWithLongKey() => new()
        {
            SharedKey = "MFRGG2LTEBQW4ZDJNZTXIZLSEB2GKYLM", // 160-bit key (20 bytes)
            AuthenticatorUri = "otpauth://totp/QuantumBands:user%40test.com?secret=MFRGG2LTEBQW4ZDJNZTXIZLSEB2GKYLM&issuer=QuantumBands"
        };

        public static Setup2FAResponse ValidSetup2FAResponseWithSpecialChars() => new()
        {
            SharedKey = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
            AuthenticatorUri = "otpauth://totp/QuantumBands:test%2Buser%40company.co.uk?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&issuer=QuantumBands"
        };

        public static Setup2FAResponse ValidSetup2FAResponseWithDifferentIssuer() => new()
        {
            SharedKey = "JBSWY3DPEHPK3PXP",
            AuthenticatorUri = "otpauth://totp/TestIssuer:testuser%40example.com?secret=JBSWY3DPEHPK3PXP&issuer=TestIssuer"
        };

        public static Setup2FAResponse ValidSetup2FAResponseWithMinimalKey() => new()
        {
            SharedKey = "GEZDGNBV", // Minimal 8-character Base32 key
            AuthenticatorUri = "otpauth://totp/QuantumBands:min%40test.com?secret=GEZDGNBV&issuer=QuantumBands"
        };

        public static Setup2FAResponse ValidSetup2FAResponseWithMaxLengthKey() => new()
        {
            SharedKey = "MFRGG2LTEBQW4ZDJNZTXIZLSEB2GKYLMNFSGS3DMNFTWYZLOORSW45DFNZ2GS5LPMRQXEZLUORSW2YLSMUQD2YLQMU", // Very long key
            AuthenticatorUri = "otpauth://totp/QuantumBands:long%40example.org?secret=MFRGG2LTEBQW4ZDJNZTXIZLSEB2GKYLMNFSGS3DMNFTWYZLOORSW45DFNZ2GS5LPMRQXEZLUORSW2YLSMUQD2YLQMU&issuer=QuantumBands"
        };

        // Test response scenarios for different user cases
        public static Setup2FAResponse Setup2FAResponseForBusinessUser() => new()
        {
            SharedKey = "KRSXG5BAIJ2W4ZDPOJSW63TFOIQHIZLB",
            AuthenticatorUri = "otpauth://totp/QuantumBands:business.user%40company.com?secret=KRSXG5BAIJ2W4ZDPOJSW63TFOIQHIZLB&issuer=QuantumBands"
        };

        public static Setup2FAResponse Setup2FAResponseForTestUser() => new()
        {
            SharedKey = "ORUX2ZLNOBZGS3THMV4HIZLBMNSXE2LM",
            AuthenticatorUri = "otpauth://totp/QuantumBands:test.automation%40qa.dev?secret=ORUX2ZLNOBZGS3THMV4HIZLBMNSXE2LM&issuer=QuantumBands"
        };

        public static Setup2FAResponse Setup2FAResponseForAdminUser() => new()
        {
            SharedKey = "MJSXG5DFON2HEZLCMFUW4ZDJMJSXG5DF",
            AuthenticatorUri = "otpauth://totp/QuantumBands:admin%40quantumbands.ai?secret=MJSXG5DFON2HEZLCMFUW4ZDJMJSXG5DF&issuer=QuantumBands"
        };

        // Edge case scenarios for testing robustness
        public static Setup2FAResponse Setup2FAResponseWithEncodedEmail() => new()
        {
            SharedKey = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
            AuthenticatorUri = "otpauth://totp/QuantumBands:user%40domain.com?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&issuer=QuantumBands"
        };

        public static Setup2FAResponse Setup2FAResponseWithComplexEmail() => new()
        {
            SharedKey = "MFZGKYLOMFWG6ZDJNZ2W24DTMFZGKYLM",
            AuthenticatorUri = "otpauth://totp/QuantumBands:complex.email%2Btest%40subdomain.example.org?secret=MFZGKYLOMFWG6ZDJNZ2W24DTMFZGKYLM&issuer=QuantumBands"
        };

        public static Setup2FAResponse Setup2FAResponseWithTimestamp() => new()
        {
            SharedKey = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
            AuthenticatorUri = $"otpauth://totp/QuantumBands:time%40test.com?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&issuer=QuantumBands&period=30"
        };

        public static Setup2FAResponse Setup2FAResponseWithCustomParameters() => new()
        {
            SharedKey = "KRSXG5BAIJ2W4ZDPOJSW63TFOIQHIZLB",
            AuthenticatorUri = "otpauth://totp/QuantumBands:custom%40example.net?secret=KRSXG5BAIJ2W4ZDPOJSW63TFOIQHIZLB&issuer=QuantumBands&digits=6&algorithm=SHA1&period=30"
        };

        // Error scenarios for negative testing
        public static Setup2FAResponse ResponseWithEmptySharedKey() => new()
        {
            SharedKey = "",
            AuthenticatorUri = "otpauth://totp/QuantumBands:empty%40test.com?secret=&issuer=QuantumBands"
        };

        public static Setup2FAResponse ResponseWithInvalidBase32Key() => new()
        {
            SharedKey = "INVALID_BASE32_KEY!", // Contains invalid Base32 characters
            AuthenticatorUri = "otpauth://totp/QuantumBands:invalid%40test.com?secret=INVALID_BASE32_KEY!&issuer=QuantumBands"
        };

        public static Setup2FAResponse ResponseWithMissingSecretInUri() => new()
        {
            SharedKey = "JBSWY3DPEHPK3PXP",
            AuthenticatorUri = "otpauth://totp/QuantumBands:missing%40test.com?issuer=QuantumBands" // Missing secret parameter
        };

        public static Setup2FAResponse ResponseWithMalformedUri() => new()
        {
            SharedKey = "JBSWY3DPEHPK3PXP",
            AuthenticatorUri = "invalid-uri-format" // Not a valid otpauth URI
        };

        public static Setup2FAResponse ResponseWithVeryLongEmail() => new()
        {
            SharedKey = "JBSWY3DPEHPK3PXP",
            AuthenticatorUri = $"otpauth://totp/QuantumBands:{new string('a', 200)}%40longdomain.com?secret=JBSWY3DPEHPK3PXP&issuer=QuantumBands"
        };

        // Utility method for creating custom responses
        public static Setup2FAResponse CustomSetup2FAResponse(string sharedKey, string email, string issuer = "QuantumBands") => new()
        {
            SharedKey = sharedKey,
            AuthenticatorUri = $"otpauth://totp/{issuer}:{Uri.EscapeDataString(email)}?secret={sharedKey}&issuer={issuer}"
        };
    }

    // SCRUM-46: Test data for Enable2FA endpoint testing
    public static class Enable2FA
    {
        // Valid request scenarios for different 2FA enable cases
        public static Enable2FARequest ValidEnable2FARequest() => new()
        {
            VerificationCode = "123456"
        };

        public static Enable2FARequest ValidEnable2FARequestWithDifferentCode() => new()
        {
            VerificationCode = "789012"
        };

        public static Enable2FARequest ValidEnable2FARequestWithTimeSyncedCode() => new()
        {
            VerificationCode = "654321"
        };

        public static Enable2FARequest ValidEnable2FARequestForBusinessUser() => new()
        {
            VerificationCode = "987654"
        };

        public static Enable2FARequest ValidEnable2FARequestForTestUser() => new()
        {
            VerificationCode = "135790"
        };

        // Edge case scenarios for comprehensive testing
        public static Enable2FARequest ValidEnable2FARequestWithLeadingZeros() => new()
        {
            VerificationCode = "000123"
        };

        public static Enable2FARequest ValidEnable2FARequestWithAllSameDigits() => new()
        {
            VerificationCode = "888888"
        };

        public static Enable2FARequest ValidEnable2FARequestMaxDigits() => new()
        {
            VerificationCode = "999999"
        };

        public static Enable2FARequest ValidEnable2FARequestMinDigits() => new()
        {
            VerificationCode = "000000"
        };

        // Invalid request scenarios for validation testing
        public static Enable2FARequest RequestWithEmptyCode() => new()
        {
            VerificationCode = ""
        };

        public static Enable2FARequest RequestWithShortCode() => new()
        {
            VerificationCode = "12345" // Only 5 digits
        };

        public static Enable2FARequest RequestWithLongCode() => new()
        {
            VerificationCode = "1234567" // 7 digits
        };

        public static Enable2FARequest RequestWithNonNumericCode() => new()
        {
            VerificationCode = "ABC123"
        };

        public static Enable2FARequest RequestWithSpecialCharacters() => new()
        {
            VerificationCode = "12@456"
        };

        public static Enable2FARequest RequestWithSpaces() => new()
        {
            VerificationCode = "12 34 56"
        };

        public static Enable2FARequest RequestWithDashes() => new()
        {
            VerificationCode = "123-456"
        };

        public static Enable2FARequest RequestWithPlusSign() => new()
        {
            VerificationCode = "+123456"
        };

        // Utility method for creating custom requests
        public static Enable2FARequest CustomEnable2FARequest(string verificationCode) => new()
        {
            VerificationCode = verificationCode
        };
    }

    // SCRUM-47: Test data for Verify2FA endpoint testing
    public static class Verify2FA
    {
        // Valid request scenarios for different 2FA verification cases
        public static Verify2FARequest ValidVerify2FARequest() => new()
        {
            VerificationCode = "123456"
        };

        public static Verify2FARequest ValidVerify2FARequestWithDifferentCode() => new()
        {
            VerificationCode = "789012"
        };

        public static Verify2FARequest ValidVerify2FARequestWithTimeSyncedCode() => new()
        {
            VerificationCode = "654321"
        };

        public static Verify2FARequest ValidVerify2FARequestForBusinessUser() => new()
        {
            VerificationCode = "987654"
        };

        public static Verify2FARequest ValidVerify2FARequestForSensitiveAction() => new()
        {
            VerificationCode = "456789"
        };

        // Edge case scenarios for comprehensive testing
        public static Verify2FARequest ValidVerify2FARequestWithLeadingZeros() => new()
        {
            VerificationCode = "000123"
        };

        public static Verify2FARequest ValidVerify2FARequestWithAllSameDigits() => new()
        {
            VerificationCode = "777777"
        };

        public static Verify2FARequest ValidVerify2FARequestMaxDigits() => new()
        {
            VerificationCode = "999999"
        };

        public static Verify2FARequest ValidVerify2FARequestMinDigits() => new()
        {
            VerificationCode = "000000"
        };

        public static Verify2FARequest ValidVerify2FARequestForLoginFlow() => new()
        {
            VerificationCode = "111222"
        };

        // Invalid request scenarios for validation testing
        public static Verify2FARequest RequestWithEmptyCode() => new()
        {
            VerificationCode = ""
        };

        public static Verify2FARequest RequestWithShortCode() => new()
        {
            VerificationCode = "12345" // Only 5 digits
        };

        public static Verify2FARequest RequestWithLongCode() => new()
        {
            VerificationCode = "1234567" // 7 digits
        };

        public static Verify2FARequest RequestWithNonNumericCode() => new()
        {
            VerificationCode = "ABC123"
        };

        public static Verify2FARequest RequestWithSpecialCharacters() => new()
        {
            VerificationCode = "12@456"
        };

        public static Verify2FARequest RequestWithSpaces() => new()
        {
            VerificationCode = "12 34 56"
        };

        public static Verify2FARequest RequestWithDashes() => new()
        {
            VerificationCode = "123-456"
        };

        public static Verify2FARequest RequestWithPlusSign() => new()
        {
            VerificationCode = "+123456"
        };

        public static Verify2FARequest RequestWithExpiredCode() => new()
        {
            VerificationCode = "999888" // Simulates an expired code
        };

        public static Verify2FARequest RequestWithReusedCode() => new()
        {
            VerificationCode = "555444" // Simulates a previously used code
        };

        // Rate limiting test scenarios
        public static Verify2FARequest RequestForRateLimitTest() => new()
        {
            VerificationCode = "111111"
        };

        public static Verify2FARequest RequestForAccountLockoutTest() => new()
        {
            VerificationCode = "333222"
        };

        // Utility method for creating custom requests
        public static Verify2FARequest CustomVerify2FARequest(string verificationCode) => new()
        {
            VerificationCode = verificationCode
        };
    }

    // SCRUM-48: Test data for Disable2FA endpoint testing
    public static class Disable2FA
    {
        // Valid request scenarios for different 2FA disable cases
        public static Disable2FARequest ValidDisable2FARequest() => new()
        {
            VerificationCode = "123456"
        };

        public static Disable2FARequest ValidDisable2FARequestWithDifferentCode() => new()
        {
            VerificationCode = "789012"
        };

        public static Disable2FARequest ValidDisable2FARequestWithTimeSyncedCode() => new()
        {
            VerificationCode = "654321"
        };

        public static Disable2FARequest ValidDisable2FARequestForBusinessUser() => new()
        {
            VerificationCode = "987654"
        };

        public static Disable2FARequest ValidDisable2FARequestForSecurityReview() => new()
        {
            VerificationCode = "456789"
        };

        // Edge case scenarios for comprehensive testing
        public static Disable2FARequest ValidDisable2FARequestWithLeadingZeros() => new()
        {
            VerificationCode = "000123"
        };

        public static Disable2FARequest ValidDisable2FARequestWithAllSameDigits() => new()
        {
            VerificationCode = "777777"
        };

        public static Disable2FARequest ValidDisable2FARequestMaxDigits() => new()
        {
            VerificationCode = "999999"
        };

        public static Disable2FARequest ValidDisable2FARequestMinDigits() => new()
        {
            VerificationCode = "000000"
        };

        public static Disable2FARequest ValidDisable2FARequestForEmergencyDisable() => new()
        {
            VerificationCode = "111222"
        };

        // Invalid request scenarios for validation testing
        public static Disable2FARequest RequestWithEmptyCode() => new()
        {
            VerificationCode = ""
        };

        public static Disable2FARequest RequestWithShortCode() => new()
        {
            VerificationCode = "12345" // Only 5 digits
        };

        public static Disable2FARequest RequestWithLongCode() => new()
        {
            VerificationCode = "1234567" // 7 digits
        };

        public static Disable2FARequest RequestWithNonNumericCode() => new()
        {
            VerificationCode = "ABC123"
        };

        public static Disable2FARequest RequestWithSpecialCharacters() => new()
        {
            VerificationCode = "12@456"
        };

        public static Disable2FARequest RequestWithSpaces() => new()
        {
            VerificationCode = "12 34 56"
        };

        public static Disable2FARequest RequestWithDashes() => new()
        {
            VerificationCode = "123-456"
        };

        public static Disable2FARequest RequestWithPlusSign() => new()
        {
            VerificationCode = "+123456"
        };

        public static Disable2FARequest RequestWithExpiredCode() => new()
        {
            VerificationCode = "999888" // Simulates an expired code
        };

        public static Disable2FARequest RequestWithInvalidCode() => new()
        {
            VerificationCode = "555444" // Simulates an invalid verification code
        };

        // Rate limiting and security test scenarios
        public static Disable2FARequest RequestForRateLimitTest() => new()
        {
            VerificationCode = "111111"
        };

        public static Disable2FARequest RequestForSecurityAuditTest() => new()
        {
            VerificationCode = "333222"
        };

        public static Disable2FARequest RequestForDataCleanupTest() => new()
        {
            VerificationCode = "444555"
        };

        public static Disable2FARequest RequestForRecoveryCodesInvalidation() => new()
        {
            VerificationCode = "666777"
        };

        // Utility method for creating custom requests
        public static Disable2FARequest CustomDisable2FARequest(string verificationCode) => new()
        {
            VerificationCode = verificationCode
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
        /// Valid wallet transaction DTO response for sender
        /// </summary>
        public static WalletTransactionDto ValidSenderTransactionResponse() => new()
        {
            TransactionId = 4001,
            TransactionTypeName = "InternalTransferSent",
            Amount = 100.00m,
            CurrencyCode = "USD",
            BalanceAfter = 900.00m, // Assuming balance was 1000 before
            ReferenceId = "TRANSFER_TO_USER_2",
            PaymentMethod = "InternalTransfer",
            Description = "Sent to recipient@example.com (User ID: 2). Notes: Transfer for lunch payment",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        /// <summary>
        /// Large amount sender transaction response
        /// </summary>
        public static WalletTransactionDto LargeAmountSenderTransactionResponse() => new()
        {
            TransactionId = 4002,
            TransactionTypeName = "InternalTransferSent",
            Amount = 5000.00m,
            CurrencyCode = "USD",
            BalanceAfter = 5000.00m, // Assuming balance was 10000 before
            ReferenceId = "TRANSFER_TO_USER_2",
            PaymentMethod = "InternalTransfer",
            Description = "Sent to recipient@example.com (User ID: 2). Notes: Large transfer for business payment",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        /// <summary>
        /// Minimum amount sender transaction response
        /// </summary>
        public static WalletTransactionDto MinimumAmountSenderTransactionResponse() => new()
        {
            TransactionId = 4003,
            TransactionTypeName = "InternalTransferSent",
            Amount = 0.01m,
            CurrencyCode = "USD",
            BalanceAfter = 999.99m, // Assuming balance was 1000 before
            ReferenceId = "TRANSFER_TO_USER_2",
            PaymentMethod = "InternalTransfer",
            Description = "Sent to recipient@example.com (User ID: 2). Notes: Minimum transfer test",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        /// <summary>
        /// Transaction response without description
        /// </summary>
        public static WalletTransactionDto TransactionResponseWithoutDescription() => new()
        {
            TransactionId = 4004,
            TransactionTypeName = "InternalTransferSent",
            Amount = 100.00m,
            CurrencyCode = "USD",
            BalanceAfter = 900.00m,
            ReferenceId = "TRANSFER_TO_USER_2",
            PaymentMethod = "InternalTransfer",
            Description = "Sent to recipient@example.com (User ID: 2). Notes: N/A",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        /// <summary>
        /// Custom transfer request
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
            TransactionTypeName = "InternalTransferSent",
            Amount = amount,
            CurrencyCode = "USD",
            BalanceAfter = balanceAfter,
            ReferenceId = $"TRANSFER_TO_USER_{recipientUserId}",
            PaymentMethod = "InternalTransfer",
            Description = $"Sent to recipient (User ID: {recipientUserId}). Notes: {description ?? "N/A"}",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static class TradingAccounts
    {
        /// <summary>
        /// Valid GetPublicTradingAccountsQuery with default parameters
        /// </summary>
        public static GetPublicTradingAccountsQuery ValidDefaultQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "CreatedAt",
            SortOrder = "Desc",
            IsActive = null,
            SearchTerm = null
        };

        /// <summary>
        /// Query with pagination - page 2
        /// </summary>
        public static GetPublicTradingAccountsQuery SecondPageQuery() => new()
        {
            PageNumber = 2,
            PageSize = 10,
            SortBy = "AccountName",
            SortOrder = "Asc",
            IsActive = true,
            SearchTerm = null
        };

        /// <summary>
        /// Query with search term
        /// </summary>
        public static GetPublicTradingAccountsQuery SearchQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "AccountName",
            SortOrder = "Asc",
            IsActive = null,
            SearchTerm = "AI Fund"
        };

        /// <summary>
        /// Query with active filter
        /// </summary>
        public static GetPublicTradingAccountsQuery ActiveOnlyQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "CreatedAt",
            SortOrder = "Desc",
            IsActive = true,
            SearchTerm = null
        };

        /// <summary>
        /// Query with maximum page size
        /// </summary>
        public static GetPublicTradingAccountsQuery MaxPageSizeQuery() => new()
        {
            PageNumber = 1,
            PageSize = 50, // Maximum allowed
            SortBy = "CreatedAt",
            SortOrder = "Desc",
            IsActive = null,
            SearchTerm = null
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
                    AccountName = "AI Growth Fund",
                    Description = "Artificial Intelligence focused growth fund",
                    EaName = "QuantumBands AI v1.0",
                    BrokerPlatformIdentifier = "QB-AI-001",
                    InitialCapital = 1000000.00m,
                    TotalSharesIssued = 100000,
                    CurrentNetAssetValue = 1150000.00m,
                    CurrentSharePrice = 11.50m,
                    ManagementFeeRate = 0.02m,
                    IsActive = true,
                    CreatedByUserId = 1,
                    CreatorUsername = "admin",
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new()
                {
                    TradingAccountId = 2,
                    AccountName = "Tech Innovation Fund",
                    Description = "Technology and innovation focused investment fund",
                    EaName = "QuantumBands AI v2.0",
                    BrokerPlatformIdentifier = "QB-TECH-002",
                    InitialCapital = 2000000.00m,
                    TotalSharesIssued = 150000,
                    CurrentNetAssetValue = 2300000.00m,
                    CurrentSharePrice = 15.33m,
                    ManagementFeeRate = 0.025m,
                    IsActive = true,
                    CreatedByUserId = 1,
                    CreatorUsername = "admin",
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                    UpdatedAt = DateTime.UtcNow.AddHours(-2)
                }
            },
            count: 2,
            pageNumber: 1,
            pageSize: 10
        );

        /// <summary>
        /// Empty paginated response
        /// </summary>
        public static PaginatedList<TradingAccountDto> EmptyResponse() => new(
            new List<TradingAccountDto>(),
            count: 0,
            pageNumber: 1,
            pageSize: 10
        );

        // SCRUM-55: Test data for GetTradingAccountDetails endpoint

        /// <summary>
        /// Valid GetTradingAccountDetailsQuery with default parameters
        /// </summary>
        public static GetTradingAccountDetailsQuery ValidDetailsQuery() => new()
        {
            ClosedTradesPageNumber = 1,
            ClosedTradesPageSize = 10,
            SnapshotsPageNumber = 1,
            SnapshotsPageSize = 10,
            OpenPositionsLimit = 20
        };

        /// <summary>
        /// Query with custom pagination parameters
        /// </summary>
        public static GetTradingAccountDetailsQuery CustomPaginationQuery() => new()
        {
            ClosedTradesPageNumber = 2,
            ClosedTradesPageSize = 5,
            SnapshotsPageNumber = 1,
            SnapshotsPageSize = 7,
            OpenPositionsLimit = 10
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
        /// Valid TradingAccountDetailDto response with complete data
        /// </summary>
        public static TradingAccountDetailDto ValidDetailResponse() => new()
        {
            TradingAccountId = 1,
            AccountName = "AI Growth Fund",
            Description = "Artificial Intelligence focused growth fund",
            EaName = "QuantumBands AI v1.0",
            BrokerPlatformIdentifier = "QB-AI-001",
            InitialCapital = 1000000.00m,
            TotalSharesIssued = 100000,
            CurrentNetAssetValue = 1150000.00m,
            CurrentSharePrice = 11.50m,
            ManagementFeeRate = 0.02m,
            IsActive = true,
            CreatedByUserId = 1,
            CreatorUsername = "admin",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            OpenPositions = ValidOpenPositions(),
            ClosedTradesHistory = ValidClosedTradesHistory(),
            DailySnapshotsInfo = ValidSnapshotsHistory()
        };

        /// <summary>
        /// Valid list of open positions
        /// </summary>
        public static List<EAOpenPositionDto> ValidOpenPositions() => new()
        {
            new()
            {
                OpenPositionId = 1,
                EaTicketId = "TKT001",
                Symbol = "EURUSD",
                TradeType = "BUY",
                VolumeLots = 0.10m,
                OpenPrice = 1.0850m,
                OpenTime = DateTime.UtcNow.AddHours(-2),
                CurrentMarketPrice = 1.0875m,
                Swap = -0.25m,
                Commission = 0.50m,
                FloatingPAndL = 25.00m,
                LastUpdateTime = DateTime.UtcNow.AddMinutes(-5)
            },
            new()
            {
                OpenPositionId = 2,
                EaTicketId = "TKT002",
                Symbol = "GBPUSD",
                TradeType = "SELL",
                VolumeLots = 0.05m,
                OpenPrice = 1.2650m,
                OpenTime = DateTime.UtcNow.AddHours(-1),
                CurrentMarketPrice = 1.2640m,
                Swap = -0.15m,
                Commission = 0.25m,
                FloatingPAndL = 5.00m,
                LastUpdateTime = DateTime.UtcNow.AddMinutes(-2)
            }
        };

        /// <summary>
        /// Valid paginated closed trades history
        /// </summary>
        public static PaginatedList<EAClosedTradeDto> ValidClosedTradesHistory() => new(
            new List<EAClosedTradeDto>
            {
                new()
                {
                    ClosedTradeId = 1,
                    EaTicketId = "TKT_CLOSED_001",
                    Symbol = "EURUSD",
                    TradeType = "BUY",
                    VolumeLots = 0.10m,
                    OpenPrice = 1.0800m,
                    OpenTime = DateTime.UtcNow.AddDays(-2),
                    ClosePrice = 1.0850m,
                    CloseTime = DateTime.UtcNow.AddDays(-1),
                    Swap = -0.50m,
                    Commission = 1.00m,
                    RealizedPAndL = 49.50m,
                    RecordedAt = DateTime.UtcNow.AddDays(-1)
                },
                new()
                {
                    ClosedTradeId = 2,
                    EaTicketId = "TKT_CLOSED_002",
                    Symbol = "GBPUSD",
                    TradeType = "SELL",
                    VolumeLots = 0.05m,
                    OpenPrice = 1.2700m,
                    OpenTime = DateTime.UtcNow.AddDays(-3),
                    ClosePrice = 1.2650m,
                    CloseTime = DateTime.UtcNow.AddDays(-2),
                    Swap = -0.30m,
                    Commission = 0.50m,
                    RealizedPAndL = 24.20m,
                    RecordedAt = DateTime.UtcNow.AddDays(-2)
                }
            },
            count: 15,
            pageNumber: 1,
            pageSize: 10
        );

        /// <summary>
        /// Valid paginated snapshots history
        /// </summary>
        public static PaginatedList<TradingAccountSnapshotDto> ValidSnapshotsHistory() => new(
            new List<TradingAccountSnapshotDto>
            {
                new()
                {
                    SnapshotId = 1,
                    SnapshotDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1)),
                    OpeningNAV = 1140000.00m,
                    RealizedPAndLForTheDay = 8000.00m,
                    UnrealizedPAndLForTheDay = 2000.00m,
                    ManagementFeeDeducted = 62.33m,
                    ProfitDistributed = 0.00m,
                    ClosingNAV = 1150000.00m,
                    ClosingSharePrice = 11.50m,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new()
                {
                    SnapshotId = 2,
                    SnapshotDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-2)),
                    OpeningNAV = 1125000.00m,
                    RealizedPAndLForTheDay = 12000.00m,
                    UnrealizedPAndLForTheDay = 3000.00m,
                    ManagementFeeDeducted = 61.64m,
                    ProfitDistributed = 0.00m,
                    ClosingNAV = 1140000.00m,
                    ClosingSharePrice = 11.40m,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            },
            count: 30,
            pageNumber: 1,
            pageSize: 10
        );

        /// <summary>
        /// Empty detail response with no additional data
        /// </summary>
        public static TradingAccountDetailDto EmptyDetailResponse() => new()
        {
            TradingAccountId = 1,
            AccountName = "Empty Fund",
            Description = "Test fund with no activity",
            EaName = "Test EA",
            BrokerPlatformIdentifier = "TEST-001",
            InitialCapital = 10000.00m,
            TotalSharesIssued = 1000,
            CurrentNetAssetValue = 10000.00m,
            CurrentSharePrice = 10.00m,
            ManagementFeeRate = 0.02m,
            IsActive = true,
            CreatedByUserId = 1,
            CreatorUsername = "testuser",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow,
            OpenPositions = new List<EAOpenPositionDto>(),
            ClosedTradesHistory = new PaginatedList<EAClosedTradeDto>(
                new List<EAClosedTradeDto>(),
                count: 0,
                pageNumber: 1,
                pageSize: 10
            ),
            DailySnapshotsInfo = new PaginatedList<TradingAccountSnapshotDto>(
                new List<TradingAccountSnapshotDto>(),
                count: 0,
                pageNumber: 1,
                pageSize: 10
            )
        };

        // SCRUM-56: Test data for GetInitialShareOfferings endpoint

        /// <summary>
        /// Valid GetInitialOfferingsQuery with default parameters
        /// </summary>
        public static GetInitialOfferingsQuery ValidOfferingsQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "OfferingStartDate",
            SortOrder = "desc",
            Status = null
        };

        /// <summary>
        /// Query with status filter for Active offerings
        /// </summary>
        public static GetInitialOfferingsQuery ActiveOfferingsQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "OfferingStartDate",
            SortOrder = "desc",
            Status = "Active"
        };

        /// <summary>
        /// Query with status filter for Completed offerings
        /// </summary>
        public static GetInitialOfferingsQuery CompletedOfferingsQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "OfferingPricePerShare",
            SortOrder = "asc",
            Status = "Completed"
        };

        /// <summary>
        /// Query with custom pagination and sorting
        /// </summary>
        public static GetInitialOfferingsQuery CustomOfferingsQuery() => new()
        {
            PageNumber = 2,
            PageSize = 5,
            SortBy = "SharesOffered",
            SortOrder = "desc",
            Status = "Active"
        };

        /// <summary>
        /// Query with maximum page size
        /// </summary>
        public static GetInitialOfferingsQuery MaxPageSizeOfferingsQuery() => new()
        {
            PageNumber = 1,
            PageSize = 50,
            SortBy = "OfferingStartDate",
            SortOrder = "desc",
            Status = null
        };

        /// <summary>
        /// Valid paginated list of InitialShareOfferingDto
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
                    SharesOffered = 10000,
                    SharesSold = 7500,
                    OfferingPricePerShare = 12.50m,
                    FloorPricePerShare = 10.00m,
                    CeilingPricePerShare = 15.00m,
                    OfferingStartDate = DateTime.UtcNow.AddDays(-10),
                    OfferingEndDate = DateTime.UtcNow.AddDays(20),
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new()
                {
                    OfferingId = 2,
                    TradingAccountId = 1,
                    AdminUserId = 1,
                    AdminUsername = "admin",
                    SharesOffered = 5000,
                    SharesSold = 5000,
                    OfferingPricePerShare = 10.75m,
                    FloorPricePerShare = 9.50m,
                    CeilingPricePerShare = 12.00m,
                    OfferingStartDate = DateTime.UtcNow.AddDays(-60),
                    OfferingEndDate = DateTime.UtcNow.AddDays(-30),
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow.AddDays(-65),
                    UpdatedAt = DateTime.UtcNow.AddDays(-30)
                }
            },
            count: 8,
            pageNumber: 1,
            pageSize: 10
        );

        /// <summary>
        /// Active offerings only response
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
                    SharesOffered = 10000,
                    SharesSold = 7500,
                    OfferingPricePerShare = 12.50m,
                    FloorPricePerShare = 10.00m,
                    CeilingPricePerShare = 15.00m,
                    OfferingStartDate = DateTime.UtcNow.AddDays(-10),
                    OfferingEndDate = DateTime.UtcNow.AddDays(20),
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                }
            },
            count: 3,
            pageNumber: 1,
            pageSize: 10
        );        /// <summary>
        /// Empty offerings response
        /// </summary>
        public static PaginatedList<InitialShareOfferingDto> EmptyOfferingsResponse() => new(
            new List<InitialShareOfferingDto>(),
            count: 0,
            pageNumber: 1,
            pageSize: 10        );
    }

    // SCRUM-70: Test data for POST /admin/trading-accounts endpoint testing
    public static class CreateTradingAccounts
    {
        /// <summary>
        /// Valid request for creating trading account with all required fields
        /// </summary>
        public static CreateTradingAccountRequest ValidRequest() => new()
        {
            AccountName = "Test Trading Account",
            Description = "A test trading account for unit tests",
            EaName = "TestEA_v1.0",
            BrokerPlatformIdentifier = "MetaTrader5_Demo",
            InitialCapital = 100000.00m,
            TotalSharesIssued = 10000,
            ManagementFeeRate = 0.02m // 2%
        };

        /// <summary>
        /// Valid minimal request with only required fields
        /// </summary>
        public static CreateTradingAccountRequest ValidMinimalRequest() => new()
        {
            AccountName = "Minimal Trading Account",
            InitialCapital = 50000.00m,
            TotalSharesIssued = 5000,
            ManagementFeeRate = 0.01m // 1%
        };

        /// <summary>
        /// Request with account name that is too long (> 100 chars)
        /// </summary>
        public static CreateTradingAccountRequest AccountNameTooLongRequest() => new()
        {
            AccountName = new string('A', 101), // 101 characters
            InitialCapital = 100000.00m,
            TotalSharesIssued = 10000,
            ManagementFeeRate = 0.02m
        };

        /// <summary>
        /// Request with description that is too long (> 1000 chars)
        /// </summary>
        public static CreateTradingAccountRequest DescriptionTooLongRequest() => new()
        {
            AccountName = "Test Account",
            Description = new string('D', 1001), // 1001 characters
            InitialCapital = 100000.00m,
            TotalSharesIssued = 10000,
            ManagementFeeRate = 0.02m
        };

        /// <summary>
        /// Request with zero initial capital
        /// </summary>
        public static CreateTradingAccountRequest ZeroInitialCapitalRequest() => new()
        {
            AccountName = "Test Account",
            InitialCapital = 0m,
            TotalSharesIssued = 10000,
            ManagementFeeRate = 0.02m
        };

        /// <summary>
        /// Request with negative initial capital
        /// </summary>
        public static CreateTradingAccountRequest NegativeInitialCapitalRequest() => new()
        {
            AccountName = "Test Account",
            InitialCapital = -1000.00m,
            TotalSharesIssued = 10000,
            ManagementFeeRate = 0.02m
        };

        /// <summary>
        /// Request with zero shares issued
        /// </summary>
        public static CreateTradingAccountRequest ZeroSharesIssuedRequest() => new()
        {
            AccountName = "Test Account",
            InitialCapital = 100000.00m,
            TotalSharesIssued = 0,
            ManagementFeeRate = 0.02m
        };

        /// <summary>
        /// Request with negative shares issued
        /// </summary>
        public static CreateTradingAccountRequest NegativeSharesIssuedRequest() => new()
        {
            AccountName = "Test Account",
            InitialCapital = 100000.00m,
            TotalSharesIssued = -1000,
            ManagementFeeRate = 0.02m
        };

        /// <summary>
        /// Request with management fee rate that is too high (> 0.9999)
        /// </summary>
        public static CreateTradingAccountRequest ExcessiveManagementFeeRequest() => new()
        {
            AccountName = "Test Account",
            InitialCapital = 100000.00m,
            TotalSharesIssued = 10000,
            ManagementFeeRate = 1.0m // 100% - exceeds maximum of 99.99%
        };

        /// <summary>
        /// Request with negative management fee rate
        /// </summary>
        public static CreateTradingAccountRequest NegativeManagementFeeRequest() => new()
        {
            AccountName = "Test Account",
            InitialCapital = 100000.00m,
            TotalSharesIssued = 10000,
            ManagementFeeRate = -0.01m
        };

        /// <summary>
        /// Request with duplicate account name
        /// </summary>
        public static CreateTradingAccountRequest DuplicateAccountNameRequest() => new()
        {
            AccountName = "Existing Account Name",
            InitialCapital = 100000.00m,
            TotalSharesIssued = 10000,
            ManagementFeeRate = 0.02m
        };        /// <summary>
        /// Successful response DTO
        /// </summary>
        public static TradingAccountDto SuccessfulResponse() => new()
        {
            TradingAccountId = 1,
            AccountName = "Test Trading Account",
            Description = "A test trading account for unit tests",
            EaName = "TestEA_v1.0",
            BrokerPlatformIdentifier = "MetaTrader5_Demo",
            InitialCapital = 100000.00m,
            TotalSharesIssued = 10000,
            CurrentNetAssetValue = 100000.00m,
            CurrentSharePrice = 10.00m, // InitialCapital / TotalShares
            ManagementFeeRate = 0.02m,
            IsActive = true,
            CreatedByUserId = 1,
            CreatorUsername = "admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow        };
    }

    // SCRUM-71: Test data for PUT /admin/trading-accounts/{accountId} endpoint testing
    public static class UpdateTradingAccounts
    {
        /// <summary>
        /// Valid request for updating trading account with all optional fields
        /// </summary>
        public static UpdateTradingAccountRequest ValidCompleteRequest() => new()
        {
            Description = "Updated trading account description",
            EaName = "UpdatedEA_v2.0",
            ManagementFeeRate = 0.025m, // 2.5%
            IsActive = true
        };

        /// <summary>
        /// Valid request for updating only description
        /// </summary>
        public static UpdateTradingAccountRequest ValidDescriptionOnlyRequest() => new()
        {
            Description = "Only description updated"
        };

        /// <summary>
        /// Valid request for updating only EA name
        /// </summary>
        public static UpdateTradingAccountRequest ValidEaNameOnlyRequest() => new()
        {
            EaName = "NewEA_v3.0"
        };

        /// <summary>
        /// Valid request for updating only management fee rate
        /// </summary>
        public static UpdateTradingAccountRequest ValidManagementFeeOnlyRequest() => new()
        {
            ManagementFeeRate = 0.03m // 3%
        };

        /// <summary>
        /// Valid request for updating only active status
        /// </summary>
        public static UpdateTradingAccountRequest ValidActiveStatusOnlyRequest() => new()
        {
            IsActive = false
        };

        /// <summary>
        /// Request with description that is too long (> 1000 chars)
        /// </summary>
        public static UpdateTradingAccountRequest DescriptionTooLongRequest() => new()
        {
            Description = new string('D', 1001) // 1001 characters
        };

        /// <summary>
        /// Request with EA name that is too long (> 100 chars)
        /// </summary>
        public static UpdateTradingAccountRequest EaNameTooLongRequest() => new()
        {
            EaName = new string('E', 101) // 101 characters
        };

        /// <summary>
        /// Request with management fee rate that is too high (> 0.9999)
        /// </summary>
        public static UpdateTradingAccountRequest ExcessiveManagementFeeRequest() => new()
        {
            ManagementFeeRate = 1.0m // 100% - exceeds maximum of 99.99%
        };

        /// <summary>
        /// Request with negative management fee rate
        /// </summary>
        public static UpdateTradingAccountRequest NegativeManagementFeeRequest() => new()
        {
            ManagementFeeRate = -0.01m
        };        /// <summary>
        /// Empty request (all fields null)
        /// </summary>
        public static UpdateTradingAccountRequest EmptyRequest() => new()
        {
            // All fields are null
        };

        /// <summary>
        /// Valid request for updating only account name
        /// </summary>
        public static UpdateTradingAccountRequest ValidAccountNameRequest() => new()
        {
            AccountName = "New Account Name"
        };

        /// <summary>
        /// Request with account name that is too long (> 100 chars)
        /// </summary>
        public static UpdateTradingAccountRequest AccountNameTooLongRequest() => new()
        {
            AccountName = new string('A', 101) // 101 characters
        };

        /// <summary>
        /// Request with empty account name (validation should fail)
        /// </summary>
        public static UpdateTradingAccountRequest EmptyAccountNameRequest() => new()
        {
            AccountName = ""
        };

        /// <summary>
        /// Successful response DTO
        /// </summary>
        public static TradingAccountDto SuccessfulResponse() => new()
        {
            TradingAccountId = 1,
            AccountName = "Updated Trading Account",
            Description = "Updated trading account description",
            EaName = "UpdatedEA_v2.0",
            BrokerPlatformIdentifier = "MetaTrader5_Demo",
            InitialCapital = 100000.00m,
            TotalSharesIssued = 10000,
            CurrentNetAssetValue = 105000.00m,
            CurrentSharePrice = 10.50m,
            ManagementFeeRate = 0.025m,
            IsActive = true,
            CreatedByUserId = 1,
            CreatorUsername = "admin",
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            UpdatedAt = DateTime.UtcNow
        };
    }

    // SCRUM-73: Test data for POST /admin/trading-accounts/{accountId}/initial-offerings endpoint testing
    public static class InitialShareOfferings
    {
        /// <summary>
        /// Valid request for creating initial share offering
        /// </summary>
        public static CreateInitialShareOfferingRequest ValidRequest() => new(
            SharesOffered: 10000,
            OfferingPricePerShare: 12.50m,
            FloorPricePerShare: 10.00m,
            CeilingPricePerShare: 15.00m,
            OfferingEndDate: DateTime.UtcNow.AddDays(30)
        );

        /// <summary>
        /// Valid update offering request for SCRUM-74 tests
        /// </summary>
        public static UpdateInitialShareOfferingRequest ValidUpdateOfferingRequest() => new()
        {
            SharesOffered = 5000,
            OfferingPricePerShare = 25.00m,
            FloorPricePerShare = 20.00m,
            CeilingPricePerShare = 30.00m,
            OfferingEndDate = DateTime.UtcNow.AddDays(30),
            Status = "Active"
        };

        /// <summary>
        /// Valid initial offering DTO for SCRUM-74 tests
        /// </summary>
        public static InitialShareOfferingDto ValidInitialOfferingDto() => new()
        {
            OfferingId = 1,
            TradingAccountId = 1,
            AdminUserId = 1,
            AdminUsername = "admin",
            SharesOffered = 5000,
            SharesSold = 0,
            OfferingPricePerShare = 25.00m,
            FloorPricePerShare = 20.00m,
            CeilingPricePerShare = 30.00m,
            OfferingStartDate = DateTime.UtcNow.AddDays(-1),
            OfferingEndDate = DateTime.UtcNow.AddDays(30),
            Status = "Active",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        /// <summary>
        /// Valid request without optional fields
        /// </summary>
        public static CreateInitialShareOfferingRequest ValidMinimalRequest() => new(
            SharesOffered: 5000,
            OfferingPricePerShare: 10.00m,
            FloorPricePerShare: null,
            CeilingPricePerShare: null,
            OfferingEndDate: null
        );

        /// <summary>
        /// Request with invalid shares offered (zero)
        /// </summary>
        public static CreateInitialShareOfferingRequest InvalidSharesZeroRequest() => new(
            SharesOffered: 0,
            OfferingPricePerShare: 12.50m,
            FloorPricePerShare: 10.00m,
            CeilingPricePerShare: 15.00m,
            OfferingEndDate: DateTime.UtcNow.AddDays(30)
        );

        /// <summary>
        /// Request with invalid shares offered (negative)
        /// </summary>
        public static CreateInitialShareOfferingRequest InvalidSharesNegativeRequest() => new(
            SharesOffered: -1000,
            OfferingPricePerShare: 12.50m,
            FloorPricePerShare: 10.00m,
            CeilingPricePerShare: 15.00m,
            OfferingEndDate: DateTime.UtcNow.AddDays(30)
        );

        /// <summary>
        /// Request with invalid offering price (zero)
        /// </summary>
        public static CreateInitialShareOfferingRequest InvalidOfferingPriceZeroRequest() => new(
            SharesOffered: 10000,
            OfferingPricePerShare: 0,
            FloorPricePerShare: 10.00m,
            CeilingPricePerShare: 15.00m,
            OfferingEndDate: DateTime.UtcNow.AddDays(30)
        );

        /// <summary>
        /// Request with invalid offering price (negative)
        /// </summary>
        public static CreateInitialShareOfferingRequest InvalidOfferingPriceNegativeRequest() => new(
            SharesOffered: 10000,
            OfferingPricePerShare: -5.00m,
            FloorPricePerShare: 10.00m,
            CeilingPricePerShare: 15.00m,
            OfferingEndDate: DateTime.UtcNow.AddDays(30)
        );

        /// <summary>
        /// Request with floor price greater than offering price
        /// </summary>
        public static CreateInitialShareOfferingRequest InvalidFloorPriceRequest() => new(
            SharesOffered: 10000,
            OfferingPricePerShare: 12.50m,
            FloorPricePerShare: 15.00m, // Floor > Offering
            CeilingPricePerShare: 20.00m,
            OfferingEndDate: DateTime.UtcNow.AddDays(30)
        );

        /// <summary>
        /// Request with ceiling price less than offering price
        /// </summary>
        public static CreateInitialShareOfferingRequest InvalidCeilingPriceRequest() => new(
            SharesOffered: 10000,
            OfferingPricePerShare: 12.50m,
            FloorPricePerShare: 10.00m,
            CeilingPricePerShare: 11.00m, // Ceiling < Offering
            OfferingEndDate: DateTime.UtcNow.AddDays(30)
        );

        /// <summary>
        /// Request with end date in the past
        /// </summary>
        public static CreateInitialShareOfferingRequest InvalidEndDateRequest() => new(
            SharesOffered: 10000,
            OfferingPricePerShare: 12.50m,
            FloorPricePerShare: 10.00m,
            CeilingPricePerShare: 15.00m,
            OfferingEndDate: DateTime.UtcNow.AddDays(-5) // Past date
        );

        /// <summary>
        /// Successful response DTO
        /// </summary>
        public static InitialShareOfferingDto SuccessfulResponse() => new()
        {
            OfferingId = 1,
            TradingAccountId = 1,
            AdminUserId = 1,
            AdminUsername = "admin",
            SharesOffered = 10000,
            SharesSold = 0,
            OfferingPricePerShare = 12.50m,
            FloorPricePerShare = 10.00m,
            CeilingPricePerShare = 15.00m,
            OfferingStartDate = DateTime.UtcNow,
            OfferingEndDate = DateTime.UtcNow.AddDays(30),
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // SCRUM-75: Test data for cancel initial offering endpoint testing
        /// <summary>
        /// Valid cancel request with admin notes
        /// </summary>
        public static CancelInitialShareOfferingRequest ValidCancelRequest() => new()
        {
            AdminNotes = "Market conditions changed, cancelling offering"
        };

        /// <summary>
        /// Valid cancel request without admin notes
        /// </summary>
        public static CancelInitialShareOfferingRequest ValidCancelRequestWithoutNotes() => new();

        /// <summary>
        /// Cancel request with admin notes exceeding maximum length (500 chars)
        /// </summary>
        public static CancelInitialShareOfferingRequest InvalidCancelRequestTooLongNotes() => new()
        {
            AdminNotes = new string('A', 501) // 501 characters - exceeds limit
        };

        /// <summary>
        /// Active offering that can be cancelled
        /// </summary>
        public static InitialShareOfferingDto ActiveOfferingForCancellation() => new()
        {
            OfferingId = 1,
            TradingAccountId = 1,
            AdminUserId = 1,
            AdminUsername = "admin",
            SharesOffered = 10000,
            SharesSold = 0,
            OfferingPricePerShare = 25.00m,
            FloorPricePerShare = 20.00m,
            CeilingPricePerShare = 30.00m,
            OfferingStartDate = DateTime.UtcNow.AddDays(-1),
            OfferingEndDate = DateTime.UtcNow.AddDays(30),
            Status = "Active",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        /// <summary>
        /// Active offering with sales that can be cancelled
        /// </summary>
        public static InitialShareOfferingDto ActiveOfferingWithSalesForCancellation() => new()
        {
            OfferingId = 2,
            TradingAccountId = 1,
            AdminUserId = 1,
            AdminUsername = "admin",
            SharesOffered = 10000,
            SharesSold = 2500,
            OfferingPricePerShare = 25.00m,
            FloorPricePerShare = 20.00m,
            CeilingPricePerShare = 30.00m,
            OfferingStartDate = DateTime.UtcNow.AddDays(-1),
            OfferingEndDate = DateTime.UtcNow.AddDays(30),
            Status = "Active",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        /// <summary>
        /// Completed offering that cannot be cancelled
        /// </summary>
        public static InitialShareOfferingDto CompletedOfferingForCancellation() => new()
        {
            OfferingId = 3,
            TradingAccountId = 1,
            AdminUserId = 1,
            AdminUsername = "admin",
            SharesOffered = 10000,
            SharesSold = 10000,
            OfferingPricePerShare = 25.00m,
            FloorPricePerShare = 20.00m,
            CeilingPricePerShare = 30.00m,
            OfferingStartDate = DateTime.UtcNow.AddDays(-5),
            OfferingEndDate = DateTime.UtcNow.AddDays(-1),
            Status = "Completed",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        /// <summary>
        /// Already cancelled offering that cannot be cancelled again
        /// </summary>
        public static InitialShareOfferingDto CancelledOfferingForCancellation() => new()
        {
            OfferingId = 4,
            TradingAccountId = 1,
            AdminUserId = 1,
            AdminUsername = "admin",
            SharesOffered = 10000,
            SharesSold = 0,
            OfferingPricePerShare = 25.00m,
            FloorPricePerShare = 20.00m,
            CeilingPricePerShare = 30.00m,
            OfferingStartDate = DateTime.UtcNow.AddDays(-3),
            OfferingEndDate = DateTime.UtcNow.AddDays(30),
            Status = "Cancelled",
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };

        /// <summary>
        /// Successfully cancelled offering response
        /// </summary>
        public static InitialShareOfferingDto CancelledOfferingResponse() => new()
        {
            OfferingId = 1,
            TradingAccountId = 1,
            AdminUserId = 1,
            AdminUsername = "admin",
            SharesOffered = 10000,
            SharesSold = 0,
            OfferingPricePerShare = 25.00m,
            FloorPricePerShare = 20.00m,
            CeilingPricePerShare = 30.00m,
            OfferingStartDate = DateTime.UtcNow.AddDays(-1),
            OfferingEndDate = DateTime.UtcNow.AddDays(30),
            Status = "Cancelled",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
    }

    // SCRUM-57: Test data for GET /exchange/orders/my endpoint testing
    public static class GetMyOrders
    {
        /// <summary>
        /// Valid query with all parameters
        /// </summary>
        public static GetMyShareOrdersQuery ValidFullQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            TradingAccountId = 1,
            Status = "Active,PartiallyFilled",
            OrderSide = "Buy",
            OrderType = "Limit",
            DateFrom = DateTime.UtcNow.AddDays(-30),
            DateTo = DateTime.UtcNow,
            SortBy = "OrderDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Valid query with minimal parameters
        /// </summary>
        public static GetMyShareOrdersQuery ValidMinimalQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10
        };

        /// <summary>
        /// Query with invalid pagination
        /// </summary>
        public static GetMyShareOrdersQuery InvalidPaginationQuery() => new()
        {
            PageNumber = -1,
            PageSize = 0
        };

        /// <summary>
        /// Query with large page size
        /// </summary>
        public static GetMyShareOrdersQuery LargePageSizeQuery() => new()
        {
            PageNumber = 1,
            PageSize = 200 // Exceeds max of 100
        };

        /// <summary>
        /// Successful response with orders
        /// </summary>
        public static PaginatedList<ShareOrderDto> SuccessfulOrdersResponse() => new(
            new List<ShareOrderDto>
            {
                new()
                {
                    OrderId = 1001,
                    UserId = 1,
                    TradingAccountId = 1,
                    TradingAccountName = "Tech Solutions Inc.",
                    OrderSide = "Buy",
                    OrderType = "Limit",
                    QuantityOrdered = 1000,
                    QuantityFilled = 750,
                    LimitPrice = 25.50m,
                    AverageFillPrice = 25.25m,
                    OrderStatus = "PartiallyFilled",
                    OrderDate = DateTime.UtcNow.AddHours(-6),
                    UpdatedAt = DateTime.UtcNow.AddMinutes(-30),
                    TransactionFee = 12.65m
                },
                new()
                {
                    OrderId = 1002,
                    UserId = 1,
                    TradingAccountId = 2,
                    TradingAccountName = "Green Energy Corp.",
                    OrderSide = "Sell",
                    OrderType = "Market",
                    QuantityOrdered = 500,
                    QuantityFilled = 500,
                    LimitPrice = null,
                    AverageFillPrice = 18.75m,
                    OrderStatus = "Filled",
                    OrderDate = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1).AddMinutes(5),
                    TransactionFee = 4.69m
                }
            },
            15,
            1,
            10
        );

        /// <summary>
        /// Filtered orders response (buy orders only)
        /// </summary>
        public static PaginatedList<ShareOrderDto> BuyOrdersResponse() => new(
            new List<ShareOrderDto>
            {
                new()
                {
                    OrderId = 1003,
                    UserId = 1,
                    TradingAccountId = 1,
                    TradingAccountName = "Tech Solutions Inc.",
                    OrderSide = "Buy",
                    OrderType = "Limit",
                    QuantityOrdered = 2000,
                    QuantityFilled = 0,
                    LimitPrice = 22.00m,
                    AverageFillPrice = null,
                    OrderStatus = "Active",
                    OrderDate = DateTime.UtcNow.AddHours(-2),
                    UpdatedAt = DateTime.UtcNow.AddHours(-2),
                    TransactionFee = null
                }
            },
            8,
            1,
            10
        );

        /// <summary>
        /// Empty orders response
        /// </summary>
        public static PaginatedList<ShareOrderDto> EmptyOrdersResponse() => new(
            new List<ShareOrderDto>(),
            0,
            1,
            10
        );
    }

    // SCRUM-58: Test data for DELETE /exchange/orders/{orderId} endpoint testing
    public static class CancelOrder
    {
        /// <summary>
        /// Valid order ID for cancellation
        /// </summary>
        public static long ValidOrderId() => 1001;

        /// <summary>
        /// Invalid order ID (zero or negative)
        /// </summary>
        public static long InvalidOrderId() => -1;

        /// <summary>
        /// Non-existent order ID
        /// </summary>
        public static long NonExistentOrderId() => 9999;

        /// <summary>
        /// Order ID belonging to another user
        /// </summary>
        public static long OtherUserOrderId() => 2001;

        /// <summary>
        /// Already cancelled order ID
        /// </summary>
        public static long CancelledOrderId() => 1002;

        /// <summary>
        /// Already executed order ID
        /// </summary>
        public static long ExecutedOrderId() => 1003;

        /// <summary>
        /// Successfully cancelled order DTO
        /// </summary>
        public static ShareOrderDto CancelledOrderDto() => new()
        {
            OrderId = ValidOrderId(),
            UserId = 1,
            TradingAccountId = 1,
            TradingAccountName = "Tech Solutions Inc.",
            OrderSide = "Buy",
            OrderType = "Limit",
            QuantityOrdered = 1000,
            QuantityFilled = 0,
            LimitPrice = 25.50m,
            AverageFillPrice = null,
            OrderStatus = "Cancelled",
            OrderDate = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow,
            TransactionFee = null
        };

        /// <summary>
        /// Partially filled order that can be cancelled
        /// </summary>
        public static ShareOrderDto PartiallyFilledOrderDto() => new()
        {
            OrderId = 1004,
            UserId = 1,
            TradingAccountId = 1,
            TradingAccountName = "Tech Solutions Inc.",
            OrderSide = "Buy",
            OrderType = "Limit",
            QuantityOrdered = 1000,
            QuantityFilled = 300,
            LimitPrice = 25.50m,
            AverageFillPrice = 25.25m,
            OrderStatus = "PartiallyFilled",
            OrderDate = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-30),
            TransactionFee = 7.58m
        };

        /// <summary>
        /// Active order ready for cancellation
        /// </summary>
        public static ShareOrderDto ActiveOrderDto() => new()
        {
            OrderId = ValidOrderId(),
            UserId = 1,
            TradingAccountId = 1,
            TradingAccountName = "Tech Solutions Inc.",
            OrderSide = "Buy",
            OrderType = "Limit",
            QuantityOrdered = 1000,
            QuantityFilled = 0,
            LimitPrice = 25.50m,
            AverageFillPrice = null,
            OrderStatus = "Active",
            OrderDate = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-2),
            TransactionFee = null
        };
    }

    // SCRUM-59: Test data for GET /exchange/order-book/{tradingAccountId} endpoint testing
    public static class GetOrderBook
    {
        /// <summary>
        /// Valid trading account ID
        /// </summary>
        public static int ValidTradingAccountId() => 1;

        /// <summary>
        /// Invalid trading account ID (zero or negative)
        /// </summary>
        public static int InvalidTradingAccountId() => -1;

        /// <summary>
        /// Non-existent trading account ID
        /// </summary>
        public static int NonExistentTradingAccountId() => 9999;

        /// <summary>
        /// Valid query with default depth
        /// </summary>
        public static GetOrderBookQuery ValidQuery() => new()
        {
            Depth = 10
        };

        /// <summary>
        /// Query with custom depth
        /// </summary>
        public static GetOrderBookQuery CustomDepthQuery(int depth) => new()
        {
            Depth = depth
        };

        /// <summary>
        /// Query with maximum depth
        /// </summary>
        public static GetOrderBookQuery MaxDepthQuery() => new()
        {
            Depth = 20
        };

        /// <summary>
        /// Query with invalid depth (too high)
        /// </summary>
        public static GetOrderBookQuery InvalidDepthQuery() => new()
        {
            Depth = 100 // Exceeds max of 20
        };

        /// <summary>
        /// Full order book with bids and asks
        /// </summary>
        public static OrderBookDto ValidOrderBookDto() => new()
        {
            TradingAccountId = ValidTradingAccountId(),
            TradingAccountName = "Tech Solutions Inc.",
            LastTradePrice = 25.50m,
            Timestamp = DateTime.UtcNow,
            Bids = new List<OrderBookEntryDto>
            {
                new() { Price = 25.45m, TotalQuantity = 1000 },
                new() { Price = 25.40m, TotalQuantity = 1500 },
                new() { Price = 25.35m, TotalQuantity = 2000 },
                new() { Price = 25.30m, TotalQuantity = 800 },
                new() { Price = 25.25m, TotalQuantity = 1200 }
            },
            Asks = new List<OrderBookEntryDto>
            {
                new() { Price = 25.55m, TotalQuantity = 900 },
                new() { Price = 25.60m, TotalQuantity = 1100 },
                new() { Price = 25.65m, TotalQuantity = 1300 },
                new() { Price = 25.70m, TotalQuantity = 700 },
                new() { Price = 25.75m, TotalQuantity = 1600 }
            }
        };

        /// <summary>
        /// Empty order book with no bids or asks
        /// </summary>
        public static OrderBookDto EmptyOrderBookDto() => new()
        {
            TradingAccountId = ValidTradingAccountId(),
            TradingAccountName = "Tech Solutions Inc.",
            LastTradePrice = null,
            Timestamp = DateTime.UtcNow,
            Bids = new List<OrderBookEntryDto>(),
            Asks = new List<OrderBookEntryDto>()
        };

        /// <summary>
        /// Order book with only bids (no asks)
        /// </summary>
        public static OrderBookDto BidsOnlyOrderBookDto() => new()
        {
            TradingAccountId = ValidTradingAccountId(),
            TradingAccountName = "Tech Solutions Inc.",
            LastTradePrice = 25.00m,
            Timestamp = DateTime.UtcNow,
            Bids = new List<OrderBookEntryDto>
            {
                new() { Price = 24.95m, TotalQuantity = 500 },
                new() { Price = 24.90m, TotalQuantity = 750 }
            },
            Asks = new List<OrderBookEntryDto>()
        };

        /// <summary>
        /// Order book with only asks (no bids)
        /// </summary>
        public static OrderBookDto AsksOnlyOrderBookDto() => new()
        {
            TradingAccountId = ValidTradingAccountId(),
            TradingAccountName = "Tech Solutions Inc.",
            LastTradePrice = 26.00m,
            Timestamp = DateTime.UtcNow,
            Bids = new List<OrderBookEntryDto>(),
            Asks = new List<OrderBookEntryDto>
            {
                new() { Price = 26.05m, TotalQuantity = 300 },
                new() { Price = 26.10m, TotalQuantity = 600 }
            }
        };

        /// <summary>
        /// Order book with limited depth (fewer entries)
        /// </summary>
        public static OrderBookDto LimitedDepthOrderBookDto() => new()
        {
            TradingAccountId = ValidTradingAccountId(),
            TradingAccountName = "Tech Solutions Inc.",
            LastTradePrice = 25.50m,
            Timestamp = DateTime.UtcNow,
            Bids = new List<OrderBookEntryDto>
            {
                new() { Price = 25.45m, TotalQuantity = 1000 },
                new() { Price = 25.40m, TotalQuantity = 1500 }
            },
            Asks = new List<OrderBookEntryDto>
            {
                new() { Price = 25.55m, TotalQuantity = 900 },
                new() { Price = 25.60m, TotalQuantity = 1100 }
            }
        };
    }

    public static class GetMarketData
    {
        public static GetMarketDataQuery ValidQuery() => new()
        {
            TradingAccountIds = "1,2,3",
            RecentTradesLimit = 5,
            ActiveOfferingsLimit = 3
        };

        public static GetMarketDataQuery QueryWithSingleTradingAccount() => new()
        {
            TradingAccountIds = "1",
            RecentTradesLimit = 10,
            ActiveOfferingsLimit = 5
        };

        public static GetMarketDataQuery QueryWithoutTradingAccountIds() => new()
        {
            RecentTradesLimit = 3,
            ActiveOfferingsLimit = 2
        };

        public static GetMarketDataQuery QueryWithInvalidRecentTradesLimit() => new()
        {
            TradingAccountIds = "1,2",
            RecentTradesLimit = 25, // Over max limit of 20
            ActiveOfferingsLimit = 5
        };

        public static GetMarketDataQuery QueryWithInvalidActiveOfferingsLimit() => new()
        {
            TradingAccountIds = "1,2",
            RecentTradesLimit = 5,
            ActiveOfferingsLimit = 15 // Over max limit of 10
        };

        public static MarketDataResponse ValidMarketDataResponse() => new()
        {
            Items = new List<TradingAccountMarketDataDto>
            {
                ValidTradingAccountMarketData(),
                TradingAccountMarketDataWithRecentTrades(),
                TradingAccountMarketDataWithActiveOfferings()
            },
            GeneratedAt = DateTime.UtcNow
        };

        public static MarketDataResponse EmptyMarketDataResponse() => new()
        {
            Items = new List<TradingAccountMarketDataDto>(),
            GeneratedAt = DateTime.UtcNow
        };

        public static TradingAccountMarketDataDto ValidTradingAccountMarketData() => new()
        {
            TradingAccountId = 1,
            TradingAccountName = "Tesla Inc.",
            LastTradePrice = 250.50m,
            BestBids = new List<OrderBookEntryDto>
            {
                new() { Price = 250.00m, TotalQuantity = 1000 },
                new() { Price = 249.50m, TotalQuantity = 1500 }
            },
            BestAsks = new List<OrderBookEntryDto>
            {
                new() { Price = 251.00m, TotalQuantity = 800 },
                new() { Price = 251.50m, TotalQuantity = 1200 }
            },
            ActiveOfferings = new List<ActiveOfferingDto>
            {
                new() { OfferingId = 1, Price = 250.75m, AvailableQuantity = 5000 },
                new() { OfferingId = 2, Price = 251.25m, AvailableQuantity = 3000 }
            },
            RecentTrades = new List<SimpleTradeDto>
            {
                new() { Price = 250.50m, Quantity = 100, TradeTime = DateTime.UtcNow.AddMinutes(-5) },
                new() { Price = 250.25m, Quantity = 200, TradeTime = DateTime.UtcNow.AddMinutes(-10) }
            }
        };

        public static TradingAccountMarketDataDto TradingAccountMarketDataWithRecentTrades() => new()
        {
            TradingAccountId = 2,
            TradingAccountName = "Apple Inc.",
            LastTradePrice = 175.80m,
            BestBids = new List<OrderBookEntryDto>
            {
                new() { Price = 175.50m, TotalQuantity = 2000 }
            },
            BestAsks = new List<OrderBookEntryDto>
            {
                new() { Price = 176.00m, TotalQuantity = 1800 }
            },
            ActiveOfferings = new List<ActiveOfferingDto>(),
            RecentTrades = new List<SimpleTradeDto>
            {
                new() { Price = 175.80m, Quantity = 150, TradeTime = DateTime.UtcNow.AddMinutes(-2) },
                new() { Price = 175.75m, Quantity = 300, TradeTime = DateTime.UtcNow.AddMinutes(-7) },
                new() { Price = 175.90m, Quantity = 75, TradeTime = DateTime.UtcNow.AddMinutes(-12) }
            }
        };

        public static TradingAccountMarketDataDto TradingAccountMarketDataWithActiveOfferings() => new()
        {
            TradingAccountId = 3,
            TradingAccountName = "Microsoft Corp.",
            LastTradePrice = 420.25m,
            BestBids = new List<OrderBookEntryDto>
            {
                new() { Price = 419.50m, TotalQuantity = 500 }
            },
            BestAsks = new List<OrderBookEntryDto>
            {
                new() { Price = 421.00m, TotalQuantity = 700 }
            },
            ActiveOfferings = new List<ActiveOfferingDto>
            {
                new() { OfferingId = 3, Price = 420.00m, AvailableQuantity = 10000 },
                new() { OfferingId = 4, Price = 421.50m, AvailableQuantity = 7500 },
                new() { OfferingId = 5, Price = 422.00m, AvailableQuantity = 5000 }
            },
            RecentTrades = new List<SimpleTradeDto>
            {
                new() { Price = 420.25m, Quantity = 50, TradeTime = DateTime.UtcNow.AddMinutes(-1) }
            }
        };

        public static TradingAccountMarketDataDto EmptyTradingAccountMarketData() => new()
        {
            TradingAccountId = 4,
            TradingAccountName = "Empty Trading Account",
            LastTradePrice = null,
            BestBids = new List<OrderBookEntryDto>(),
            BestAsks = new List<OrderBookEntryDto>(),
            ActiveOfferings = new List<ActiveOfferingDto>(),
            RecentTrades = new List<SimpleTradeDto>()
        };

        public static string InvalidTradingAccountIds() => "invalid,format";

        public static string ValidTradingAccountIds() => "1,2,3";

        public static string SingleTradingAccountId() => "1";
    }

    // SCRUM-61: Test data for GET /exchange/trades/my endpoint testing
    public static class GetMyTrades
    {
        /// <summary>
        /// Valid query with all parameters
        /// </summary>
        public static GetMyShareTradesQuery ValidFullQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            TradingAccountId = 1,
            OrderSide = "Buy",
            DateFrom = DateTime.UtcNow.AddDays(-30),
            DateTo = DateTime.UtcNow,
            SortBy = "TradeDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Valid query with minimal parameters
        /// </summary>
        public static GetMyShareTradesQuery ValidMinimalQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10
        };

        /// <summary>
        /// Query with invalid pagination
        /// </summary>
        public static GetMyShareTradesQuery InvalidPaginationQuery() => new()
        {
            PageNumber = -1,
            PageSize = 0
        };

        /// <summary>
        /// Query with large page size
        /// </summary>
        public static GetMyShareTradesQuery LargePageSizeQuery() => new()
        {
            PageNumber = 1,
            PageSize = 200 // Exceeds max of 100
        };

        /// <summary>
        /// Query with buy orders filter
        /// </summary>
        public static GetMyShareTradesQuery BuyTradesQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            OrderSide = "Buy"
        };

        /// <summary>
        /// Query with sell orders filter
        /// </summary>
        public static GetMyShareTradesQuery SellTradesQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            OrderSide = "Sell"
        };

        /// <summary>
        /// Query with date range filter
        /// </summary>
        public static GetMyShareTradesQuery DateRangeQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            DateFrom = DateTime.UtcNow.AddDays(-7),
            DateTo = DateTime.UtcNow
        };

        /// <summary>
        /// Query with trading account filter
        /// </summary>
        public static GetMyShareTradesQuery TradingAccountFilterQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            TradingAccountId = 2
        };

        /// <summary>
        /// Query with combined filters
        /// </summary>
        public static GetMyShareTradesQuery CombinedFiltersQuery() => new()
        {
            PageNumber = 1,
            PageSize = 15,
            TradingAccountId = 1,
            OrderSide = "Sell",
            DateFrom = DateTime.UtcNow.AddDays(-14),
            DateTo = DateTime.UtcNow,
            SortBy = "TradePrice",
            SortOrder = "asc"
        };

        /// <summary>
        /// Successful response with trades
        /// </summary>
        public static PaginatedList<MyShareTradeDto> SuccessfulTradesResponse() => new(
            new List<MyShareTradeDto>
            {
                new()
                {
                    TradeId = 2001,
                    TradingAccountId = 1,
                    TradingAccountName = "Tech Solutions Inc.",
                    OrderSide = "Buy",
                    QuantityTraded = 100,
                    TradePrice = 25.50m,
                    TotalValue = 2550.00m,
                    FeeAmount = 5.10m,
                    TradeDate = DateTime.UtcNow.AddHours(-2)
                },
                new()
                {
                    TradeId = 2002,
                    TradingAccountId = 1,
                    TradingAccountName = "Tech Solutions Inc.",
                    OrderSide = "Sell",
                    QuantityTraded = 50,
                    TradePrice = 26.00m,
                    TotalValue = 1300.00m,
                    FeeAmount = 2.60m,
                    TradeDate = DateTime.UtcNow.AddHours(-4)
                },
                new()
                {
                    TradeId = 2003,
                    TradingAccountId = 2,
                    TradingAccountName = "Green Energy Corp.",
                    OrderSide = "Buy",
                    QuantityTraded = 200,
                    TradePrice = 18.75m,
                    TotalValue = 3750.00m,
                    FeeAmount = 7.50m,
                    TradeDate = DateTime.UtcNow.AddDays(-1)
                }
            },
            20,
            1,
            10
        );

        /// <summary>
        /// Filtered trades response (buy orders only)
        /// </summary>
        public static PaginatedList<MyShareTradeDto> BuyTradesResponse() => new(
            new List<MyShareTradeDto>
            {
                new()
                {
                    TradeId = 2004,
                    TradingAccountId = 1,
                    TradingAccountName = "Tech Solutions Inc.",
                    OrderSide = "Buy",
                    QuantityTraded = 150,
                    TradePrice = 24.80m,
                    TotalValue = 3720.00m,
                    FeeAmount = 7.44m,
                    TradeDate = DateTime.UtcNow.AddHours(-6)
                },
                new()
                {
                    TradeId = 2005,
                    TradingAccountId = 2,
                    TradingAccountName = "Green Energy Corp.",
                    OrderSide = "Buy",
                    QuantityTraded = 75,
                    TradePrice = 19.20m,
                    TotalValue = 1440.00m,
                    FeeAmount = 2.88m,
                    TradeDate = DateTime.UtcNow.AddHours(-8)
                }
            },
            12,
            1,
            10
        );

        /// <summary>
        /// Sell trades only response
        /// </summary>
        public static PaginatedList<MyShareTradeDto> SellTradesResponse() => new(
            new List<MyShareTradeDto>
            {
                new()
                {
                    TradeId = 2006,
                    TradingAccountId = 1,
                    TradingAccountName = "Tech Solutions Inc.",
                    OrderSide = "Sell",
                    QuantityTraded = 80,
                    TradePrice = 27.10m,
                    TotalValue = 2168.00m,
                    FeeAmount = 4.34m,
                    TradeDate = DateTime.UtcNow.AddHours(-3)
                }
            },
            8,
            1,
            10
        );

        /// <summary>
        /// Empty trades response
        /// </summary>
        public static PaginatedList<MyShareTradeDto> EmptyTradesResponse() => new(
            new List<MyShareTradeDto>(),
            0,
            1,
            10
        );

        /// <summary>
        /// Large trades response with high values
        /// </summary>
        public static PaginatedList<MyShareTradeDto> LargeTradesResponse() => new(
            new List<MyShareTradeDto>
            {
                new()
                {
                    TradeId = 2007,
                    TradingAccountId = 3,
                    TradingAccountName = "Financial Holdings Ltd.",
                    OrderSide = "Buy",
                    QuantityTraded = 1000,
                    TradePrice = 125.50m,
                    TotalValue = 125500.00m,
                    FeeAmount = 251.00m,
                    TradeDate = DateTime.UtcNow.AddDays(-2)
                }
            },
            5,
            1,
            10
        );

        /// <summary>
        /// Trades from specific trading account
        /// </summary>
        public static PaginatedList<MyShareTradeDto> TradingAccountFilteredResponse() => new(
            new List<MyShareTradeDto>
            {
                new()
                {
                    TradeId = 2008,
                    TradingAccountId = 2,
                    TradingAccountName = "Green Energy Corp.",
                    OrderSide = "Buy",
                    QuantityTraded = 300,
                    TradePrice = 18.90m,
                    TotalValue = 5670.00m,
                    FeeAmount = 11.34m,
                    TradeDate = DateTime.UtcNow.AddHours(-12)
                },
                new()
                {
                    TradeId = 2009,
                    TradingAccountId = 2,
                    TradingAccountName = "Green Energy Corp.",
                    OrderSide = "Sell",
                    QuantityTraded = 100,
                    TradePrice = 19.25m,
                    TotalValue = 1925.00m,
                    FeeAmount = 3.85m,
                    TradeDate = DateTime.UtcNow.AddHours(-15)
                }
            },
            6,
            1,
            10
        );

        /// <summary>
        /// Date range filtered response
        /// </summary>
        public static PaginatedList<MyShareTradeDto> DateRangeFilteredResponse() => new(
            new List<MyShareTradeDto>
            {
                new()
                {
                    TradeId = 2010,
                    TradingAccountId = 1,
                    TradingAccountName = "Tech Solutions Inc.",
                    OrderSide = "Buy",
                    QuantityTraded = 60,
                    TradePrice = 24.00m,
                    TotalValue = 1440.00m,
                    FeeAmount = 2.88m,
                    TradeDate = DateTime.UtcNow.AddDays(-3)
                }
            },
            3,
            1,
            10
        );

        /// <summary>
        /// Multi-page response with pagination info
        /// </summary>
        public static PaginatedList<MyShareTradeDto> SecondPageResponse() => new(
            new List<MyShareTradeDto>
            {
                new()
                {
                    TradeId = 2011,
                    TradingAccountId = 1,
                    TradingAccountName = "Tech Solutions Inc.",
                    OrderSide = "Sell",
                    QuantityTraded = 120,
                    TradePrice = 25.75m,
                    TotalValue = 3090.00m,
                    FeeAmount = 6.18m,
                    TradeDate = DateTime.UtcNow.AddDays(-5)
                }
            },
            25,
            2,
            10
        );

        /// <summary>
        /// Trades with zero fees
        /// </summary>
        public static PaginatedList<MyShareTradeDto> ZeroFeeTradesResponse() => new(
            new List<MyShareTradeDto>
            {
                new()
                {
                    TradeId = 2012,
                    TradingAccountId = 1,
                    TradingAccountName = "Tech Solutions Inc.",
                    OrderSide = "Buy",
                    QuantityTraded = 25,
                    TradePrice = 30.00m,
                    TotalValue = 750.00m,
                    FeeAmount = null, // No fee
                    TradeDate = DateTime.UtcNow.AddHours(-1)
                }
            },
            1,
            1,
            10
        );

        /// <summary>
        /// Custom query for testing
        /// </summary>
        public static GetMyShareTradesQuery CustomQuery(int pageNumber, int pageSize, int? tradingAccountId = null, string? orderSide = null) => new()
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TradingAccountId = tradingAccountId,
            OrderSide = orderSide,
            SortBy = "TradeDate",
            SortOrder = "desc"
        };

        /// <summary>
        /// Custom trade response
        /// </summary>
        public static MyShareTradeDto CustomTrade(long tradeId, int tradingAccountId, string tradingAccountName, string orderSide, long quantity, decimal price) => new()
        {
            TradeId = tradeId,
            TradingAccountId = tradingAccountId,
            TradingAccountName = tradingAccountName,
            OrderSide = orderSide,
            QuantityTraded = quantity,
            TradePrice = price,
            TotalValue = quantity * price,
            FeeAmount = Math.Round((quantity * price) * 0.002m, 2), // 0.2% fee
            TradeDate = DateTime.UtcNow
        };
    }







    // SCRUM-78: Test data for GET /admin/wallets/withdrawals/pending-approval endpoint testing
    /// <summary>
    /// Test data builder for GetPendingWithdrawals functionality
    /// Provides comprehensive test data for unit testing the pending withdrawals retrieval feature.
    /// 
    /// This class supports testing of:
    /// - Valid query requests (happy path scenarios)
    /// - Pagination parameters (page number, page size)
    /// - Filtering scenarios (date ranges, amounts, user filters)
    /// - Sorting options (RequestedAt, Amount, Username)
    /// - Response DTOs for pending withdrawal requests
    /// 
    /// Usage:
    /// - WalletsTestDataBuilder.GetPendingWithdrawals.ValidQuery() - Returns valid query with default parameters
    /// - WalletsTestDataBuilder.GetPendingWithdrawals.PendingWithdrawalsResponse() - Returns expected response list
    /// - Various filtering and pagination methods for comprehensive testing coverage
    /// </summary>
    public static class GetPendingWithdrawals
    {
        /// <summary>
        /// Valid query for getting pending withdrawals with default parameters
        /// </summary>
        public static GetAdminPendingWithdrawalsQuery ValidQuery() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "RequestedAt",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with custom pagination parameters
        /// </summary>
        public static GetAdminPendingWithdrawalsQuery QueryWithPagination(int pageNumber, int pageSize) => new()
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = "RequestedAt",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with date range filtering
        /// </summary>
        public static GetAdminPendingWithdrawalsQuery QueryWithDateRange() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "RequestedAt",
            SortOrder = "desc",
            DateFrom = DateTime.UtcNow.AddDays(-30),
            DateTo = DateTime.UtcNow
        };

        /// <summary>
        /// Query with amount range filtering
        /// </summary>
        public static GetAdminPendingWithdrawalsQuery QueryWithAmountRange() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "Amount",
            SortOrder = "desc",
            MinAmount = 100m,
            MaxAmount = 10000m
        };

        /// <summary>
        /// Query with user filtering
        /// </summary>
        public static GetAdminPendingWithdrawalsQuery QueryWithUserFilter() => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "RequestedAt",
            SortOrder = "desc",
            UserId = 123,
            UsernameOrEmail = "testuser"
        };

        /// <summary>
        /// Query with custom sorting
        /// </summary>
        public static GetAdminPendingWithdrawalsQuery QueryWithCustomSorting(string sortBy, string sortOrder) => new()
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = sortBy,
            SortOrder = sortOrder
        };

        /// <summary>
        /// Query with invalid pagination (negative values)
        /// </summary>
        public static GetAdminPendingWithdrawalsQuery QueryWithInvalidPagination() => new()
        {
            PageNumber = -1,
            PageSize = -5,
            SortBy = "RequestedAt",
            SortOrder = "desc"
        };

        /// <summary>
        /// Query with oversized page size
        /// </summary>
        public static GetAdminPendingWithdrawalsQuery QueryWithOversizedPageSize() => new()
        {
            PageNumber = 1,
            PageSize = 500, // Exceeds max limit
            SortBy = "RequestedAt",
            SortOrder = "desc"
        };

        /// <summary>
        /// Valid pending withdrawal request DTO for response testing
        /// </summary>
        public static WithdrawalRequestAdminViewDto ValidPendingWithdrawalDto() => new()
        {
            TransactionId = 2001,
            UserId = 123,
            Username = "testuser123",
            UserEmail = "testuser123@example.com",
            Amount = 500.00m,
            CurrencyCode = "USD",
            Status = "PendingAdminApproval",
            WithdrawalMethodDetails = "Bank Transfer - Account ending in 1234",
            UserNotes = "Need funds for urgent payment",
            RequestedAt = DateTime.UtcNow.AddHours(-6),
            AdminNotes = null // Pending requests don't have admin notes yet
        };

        /// <summary>
        /// List of multiple pending withdrawal requests for testing pagination
        /// </summary>
        public static List<WithdrawalRequestAdminViewDto> MultiplePendingWithdrawals() => new()
        {
            new WithdrawalRequestAdminViewDto
            {
                TransactionId = 2001,
                UserId = 123,
                Username = "user1",
                UserEmail = "user1@example.com",
                Amount = 500.00m,
                CurrencyCode = "USD",
                Status = "PendingAdminApproval",
                WithdrawalMethodDetails = "Bank Transfer",
                UserNotes = "Urgent payment needed",
                RequestedAt = DateTime.UtcNow.AddHours(-1),
                AdminNotes = null
            },
            new WithdrawalRequestAdminViewDto
            {
                TransactionId = 2002,
                UserId = 456,
                Username = "user2",
                UserEmail = "user2@example.com",
                Amount = 1000.00m,
                CurrencyCode = "USD",
                Status = "PendingAdminApproval",
                WithdrawalMethodDetails = "PayPal",
                UserNotes = "Monthly withdrawal",
                RequestedAt = DateTime.UtcNow.AddHours(-2),
                AdminNotes = null
            },
            new WithdrawalRequestAdminViewDto
            {
                TransactionId = 2003,
                UserId = 789,
                Username = "user3",
                UserEmail = "user3@example.com",
                Amount = 250.00m,
                CurrencyCode = "USD",
                Status = "PendingAdminApproval",
                WithdrawalMethodDetails = "Wire Transfer",
                UserNotes = null,
                RequestedAt = DateTime.UtcNow.AddHours(-3),
                AdminNotes = null
            }
        };

        /// <summary>
        /// Empty result list for testing scenarios with no pending withdrawals
        /// </summary>
        public static List<WithdrawalRequestAdminViewDto> EmptyWithdrawalsList() => new();

        /// <summary>
        /// Pending withdrawals with varied amounts for testing sorting
        /// </summary>
        public static List<WithdrawalRequestAdminViewDto> PendingWithdrawalsWithVariedAmounts() => new()
        {
            new WithdrawalRequestAdminViewDto
            {
                TransactionId = 2010,
                UserId = 100,
                Username = "lowamount",
                UserEmail = "low@example.com",
                Amount = 50.00m,
                CurrencyCode = "USD",
                Status = "PendingAdminApproval",
                WithdrawalMethodDetails = "Bank Transfer",
                UserNotes = "Small withdrawal",
                RequestedAt = DateTime.UtcNow.AddHours(-1),
                AdminNotes = null
            },
            new WithdrawalRequestAdminViewDto
            {
                TransactionId = 2011,
                UserId = 200,
                Username = "highamount",
                UserEmail = "high@example.com",
                Amount = 5000.00m,
                CurrencyCode = "USD",
                Status = "PendingAdminApproval",
                WithdrawalMethodDetails = "Wire Transfer",
                UserNotes = "Large withdrawal",
                RequestedAt = DateTime.UtcNow.AddHours(-2),
                AdminNotes = null
            },
            new WithdrawalRequestAdminViewDto
            {
                TransactionId = 2012,
                UserId = 300,
                Username = "mediumamount",
                UserEmail = "medium@example.com",
                Amount = 1000.00m,
                CurrencyCode = "USD",
                Status = "PendingAdminApproval",
                WithdrawalMethodDetails = "PayPal",
                UserNotes = "Medium withdrawal",
                RequestedAt = DateTime.UtcNow.AddHours(-3),
                AdminNotes = null
            }
        };

        /// <summary>
        /// Custom withdrawal request DTO
        /// </summary>
        public static WithdrawalRequestAdminViewDto CustomWithdrawalDto(
            long transactionId,
            int userId,
            string username,
            string email,
            decimal amount,
            string status = "PendingAdminApproval") => new()
        {
            TransactionId = transactionId,
            UserId = userId,
            Username = username,
            UserEmail = email,
            Amount = amount,
            CurrencyCode = "USD",
            Status = status,
            WithdrawalMethodDetails = "Bank Transfer",
            UserNotes = $"Withdrawal for user {username}",
            RequestedAt = DateTime.UtcNow.AddHours(-1),
            AdminNotes = null
        };
    }

    /// <summary>
    /// Test data for ApproveWithdrawal endpoint testing
    /// </summary>
    public static class ApproveWithdrawal
    {
        /// <summary>
        /// Valid approval request with minimal data
        /// </summary>
        public static ApproveWithdrawalRequest ValidRequest() => new()
        {
            TransactionId = 1001L,
            AdminNotes = "Approved after verification",
            ExternalTransactionReference = "EXT-12345"
        };

        /// <summary>
        /// Valid approval request without optional fields
        /// </summary>
        public static ApproveWithdrawalRequest ValidRequestMinimal() => new()
        {
            TransactionId = 1002L
        };

        /// <summary>
        /// Approval request with maximum length admin notes
        /// </summary>
        public static ApproveWithdrawalRequest ValidRequestMaxNotes() => new()
        {
            TransactionId = 1003L,
            AdminNotes = new string('a', 500), // Max 500 chars
            ExternalTransactionReference = "EXT-MAX-NOTES"
        };

        /// <summary>
        /// Invalid request with zero transaction ID
        /// </summary>
        public static ApproveWithdrawalRequest InvalidTransactionIdZero() => new()
        {
            TransactionId = 0L,
            AdminNotes = "Invalid transaction ID"
        };

        /// <summary>
        /// Invalid request with negative transaction ID
        /// </summary>
        public static ApproveWithdrawalRequest InvalidTransactionIdNegative() => new()
        {
            TransactionId = -1L,
            AdminNotes = "Negative transaction ID"
        };

        /// <summary>
        /// Invalid request with admin notes too long
        /// </summary>
        public static ApproveWithdrawalRequest InvalidAdminNotesTooLong() => new()
        {
            TransactionId = 1004L,
            AdminNotes = new string('x', 501), // Exceeds 500 chars limit
            ExternalTransactionReference = "EXT-LONG-NOTES"
        };

        /// <summary>
        /// Invalid request with external reference too long
        /// </summary>
        public static ApproveWithdrawalRequest InvalidExternalReferenceTooLong() => new()
        {
            TransactionId = 1005L,
            AdminNotes = "Valid notes",
            ExternalTransactionReference = new string('y', 256) // Exceeds 255 chars limit
        };

        /// <summary>
        /// Request for non-existent transaction
        /// </summary>
        public static ApproveWithdrawalRequest NonExistentTransaction() => new()
        {
            TransactionId = 999999L,
            AdminNotes = "Transaction not found"
        };

        /// <summary>
        /// Request for already processed transaction
        /// </summary>
        public static ApproveWithdrawalRequest AlreadyProcessedTransaction() => new()
        {
            TransactionId = 2001L,
            AdminNotes = "Already processed"
        };

        /// <summary>
        /// Request for cancelled transaction
        /// </summary>
        public static ApproveWithdrawalRequest CancelledTransaction() => new()
        {
            TransactionId = 2002L,
            AdminNotes = "Cancelled transaction"
        };

        /// <summary>
        /// Valid wallet transaction response for successful approval
        /// </summary>
        public static WalletTransactionDto SuccessfulApprovalResponse() => new()
        {
            TransactionId = 1001L,
            TransactionTypeName = "Withdrawal",
            Amount = 100.00m,
            CurrencyCode = "USD",
            BalanceAfter = 900.00m,
            ReferenceId = "WD-1001",
            PaymentMethod = "Bank Transfer",
            ExternalTransactionId = "EXT-12345",
            Description = "Withdrawal approved by admin",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow.AddMinutes(-5),
            UpdatedAt = DateTime.UtcNow
        };

        /// <summary>
        /// Valid wallet transaction response for large amount approval
        /// </summary>
        public static WalletTransactionDto LargeAmountApprovalResponse() => new()
        {
            TransactionId = 1003L,
            TransactionTypeName = "Withdrawal",
            Amount = 5000.00m,
            CurrencyCode = "USD",
            BalanceAfter = 15000.00m,
            ReferenceId = "WD-1003",
            PaymentMethod = "Wire Transfer",
            ExternalTransactionId = "EXT-LARGE-001",
            Description = "Large withdrawal approved after enhanced verification",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow.AddMinutes(-2),
            UpdatedAt = DateTime.UtcNow
        };

        /// <summary>
        /// Valid wallet transaction response for minimal approval
        /// </summary>
        public static WalletTransactionDto MinimalApprovalResponse() => new()
        {
            TransactionId = 1002L,
            TransactionTypeName = "Withdrawal",
            Amount = 50.00m,
            CurrencyCode = "USD",
            BalanceAfter = 450.00m,
            ReferenceId = "WD-1002",
            PaymentMethod = "Bank Transfer",
            ExternalTransactionId = null,
            Description = "Withdrawal approved",
            Status = "Completed",
            TransactionDate = DateTime.UtcNow.AddMinutes(-3),
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Test data for RejectWithdrawal endpoint testing
    /// </summary>
    public static class RejectWithdrawal
    {
        /// <summary>
        /// Valid rejection request with minimal data
        /// </summary>
        public static RejectWithdrawalRequest ValidRequest() => new()
        {
            TransactionId = 2001L,
            AdminNotes = "Insufficient verification documents provided"
        };

        /// <summary>
        /// Valid rejection request with detailed reason
        /// </summary>
        public static RejectWithdrawalRequest ValidRequestDetailed() => new()
        {
            TransactionId = 2002L,
            AdminNotes = "Transaction flagged by risk management system due to suspicious activity patterns. Additional verification required before processing can continue."
        };

        /// <summary>
        /// Rejection request with maximum length admin notes
        /// </summary>
        public static RejectWithdrawalRequest ValidRequestMaxNotes() => new()
        {
            TransactionId = 2003L,
            AdminNotes = new string('a', 500) // Max 500 chars
        };

        /// <summary>
        /// Invalid request with zero transaction ID
        /// </summary>
        public static RejectWithdrawalRequest InvalidTransactionIdZero() => new()
        {
            TransactionId = 0L,
            AdminNotes = "Invalid transaction ID"
        };

        /// <summary>
        /// Invalid request with negative transaction ID
        /// </summary>
        public static RejectWithdrawalRequest InvalidTransactionIdNegative() => new()
        {
            TransactionId = -1L,
            AdminNotes = "Negative transaction ID"
        };

        /// <summary>
        /// Invalid request with empty admin notes
        /// </summary>
        public static RejectWithdrawalRequest InvalidEmptyAdminNotes() => new()
        {
            TransactionId = 2004L,
            AdminNotes = ""
        };

        /// <summary>
        /// Invalid request with whitespace-only admin notes
        /// </summary>
        public static RejectWithdrawalRequest InvalidWhitespaceAdminNotes() => new()
        {
            TransactionId = 2005L,
            AdminNotes = "   "
        };

        /// <summary>
        /// Invalid request with admin notes too long
        /// </summary>
        public static RejectWithdrawalRequest InvalidAdminNotesTooLong() => new()
        {
            TransactionId = 2006L,
            AdminNotes = new string('x', 501) // Exceeds 500 chars limit
        };

        /// <summary>
        /// Request for non-existent transaction
        /// </summary>
        public static RejectWithdrawalRequest NonExistentTransaction() => new()
        {
            TransactionId = 999999L,
            AdminNotes = "Transaction not found"
        };

        /// <summary>
        /// Request for already processed transaction
        /// </summary>
        public static RejectWithdrawalRequest AlreadyProcessedTransaction() => new()
        {
            TransactionId = 3001L,
            AdminNotes = "Already processed transaction"
        };

        /// <summary>
        /// Request for already cancelled transaction
        /// </summary>
        public static RejectWithdrawalRequest AlreadyCancelledTransaction() => new()
        {
            TransactionId = 3002L,
            AdminNotes = "Already cancelled transaction"
        };

        /// <summary>
        /// Valid wallet transaction response for successful rejection
        /// </summary>
        public static WalletTransactionDto SuccessfulRejectionResponse() => new()
        {
            TransactionId = 2001L,
            TransactionTypeName = "Withdrawal",
            Amount = 150.00m,
            CurrencyCode = "USD",
            BalanceAfter = 1150.00m, // Balance restored
            ReferenceId = "WD-2001",
            PaymentMethod = "Bank Transfer",
            ExternalTransactionId = null,
            Description = "Withdrawal rejected by admin",
            Status = "Rejected",
            TransactionDate = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow
        };

        /// <summary>
        /// Valid wallet transaction response for large amount rejection
        /// </summary>
        public static WalletTransactionDto LargeAmountRejectionResponse() => new()
        {
            TransactionId = 2003L,
            TransactionTypeName = "Withdrawal",
            Amount = 10000.00m,
            CurrencyCode = "USD",
            BalanceAfter = 25000.00m, // Balance restored
            ReferenceId = "WD-2003",
            PaymentMethod = "Wire Transfer",
            ExternalTransactionId = null,
            Description = "Large withdrawal rejected after review",
            Status = "Rejected",
            TransactionDate = DateTime.UtcNow.AddMinutes(-5),
            UpdatedAt = DateTime.UtcNow
        };

        /// <summary>
        /// Valid wallet transaction response for detailed rejection
        /// </summary>
        public static WalletTransactionDto DetailedRejectionResponse() => new()
        {
            TransactionId = 2002L,
            TransactionTypeName = "Withdrawal",
            Amount = 500.00m,
            CurrencyCode = "USD",
            BalanceAfter = 2500.00m, // Balance restored
            ReferenceId = "WD-2002",
            PaymentMethod = "Bank Transfer",
            ExternalTransactionId = null,
            Description = "Withdrawal rejected due to compliance concerns",
            Status = "Rejected",
            TransactionDate = DateTime.UtcNow.AddMinutes(-15),
            UpdatedAt = DateTime.UtcNow
        };
    }
} 