using QuantumBands.Application.Features.Authentication.Commands.RegisterUser;
using QuantumBands.Application.Features.Authentication;
using QuantumBands.Domain.Entities;

namespace QuantumBands.Tests.Fixtures;

public static class TestDataBuilder
{
    public static class RegisterUser
    {
        public static RegisterUserCommand ValidCommand() => new()
        {
            Username = "testuser123",
            Email = "test@example.com",
            Password = "StrongPassword123!",
            FullName = "Test User"
        };

        public static RegisterUserCommand ValidCommandWithoutFullName() => new()
        {
            Username = "testuser456",
            Email = "test2@example.com",
            Password = "StrongPassword123!"
        };

        public static RegisterUserCommand CommandWithShortUsername() => new()
        {
            Username = "ab", // Too short
            Email = "test@example.com",
            Password = "StrongPassword123!",
            FullName = "Test User"
        };

        public static RegisterUserCommand CommandWithLongUsername() => new()
        {
            Username = new string('a', 51), // Too long (51 chars)
            Email = "test@example.com",
            Password = "StrongPassword123!",
            FullName = "Test User"
        };

        public static RegisterUserCommand CommandWithInvalidUsername() => new()
        {
            Username = "test-user@", // Invalid characters
            Email = "test@example.com",
            Password = "StrongPassword123!",
            FullName = "Test User"
        };

        public static RegisterUserCommand CommandWithInvalidEmail() => new()
        {
            Username = "testuser123",
            Email = "invalid-email", // Invalid format
            Password = "StrongPassword123!",
            FullName = "Test User"
        };

        public static RegisterUserCommand CommandWithLongEmail() => new()
        {
            Username = "testuser123",
            Email = new string('a', 240) + "@example.com", // Too long (256 chars total)
            Password = "StrongPassword123!",
            FullName = "Test User"
        };

        public static RegisterUserCommand CommandWithWeakPassword() => new()
        {
            Username = "testuser123",
            Email = "test@example.com",
            Password = "weak", // Too short
            FullName = "Test User"
        };

        public static RegisterUserCommand CommandWithPasswordWithoutSpecialChars() => new()
        {
            Username = "testuser123",
            Email = "test@example.com",
            Password = "WeakPassword123", // No special characters
            FullName = "Test User"
        };

        public static RegisterUserCommand CommandWithLongFullName() => new()
        {
            Username = "testuser123",
            Email = "test@example.com",
            Password = "StrongPassword123!",
            FullName = new string('a', 201) // Too long (201 chars)
        };
    }

    public static class Users
    {
        public static User ValidUser() => new()
        {
            UserId = 1,
            Username = "testuser123",
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Test User",
            IsEmailVerified = false,
            TwoFactorEnabled = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RoleId = 2, // Investor role
            EmailVerificationToken = null,
            EmailVerificationTokenExpiry = null,
            RefreshToken = null,
            RefreshTokenExpiry = null,
            PasswordResetToken = null,
            PasswordResetTokenExpiry = null,
            TwoFactorSecretKey = null,
            LastLoginDate = null
        };

        public static User ExistingUserWithSameUsername() => new()
        {
            UserId = 2,
            Username = "testuser123", // Same username
            Email = "existing@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Existing User",
            IsEmailVerified = true,
            TwoFactorEnabled = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-30),
            RoleId = 2,
            EmailVerificationToken = null,
            EmailVerificationTokenExpiry = null,
            RefreshToken = null,
            RefreshTokenExpiry = null,
            PasswordResetToken = null,
            PasswordResetTokenExpiry = null,
            TwoFactorSecretKey = null,
            LastLoginDate = DateTime.UtcNow.AddDays(-1)
        };

        public static User ExistingUserWithSameEmail() => new()
        {
            UserId = 3,
            Username = "existinguser",
            Email = "test@example.com", // Same email
            PasswordHash = "hashedpassword",
            FullName = "Existing User",
            IsEmailVerified = true,
            TwoFactorEnabled = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-30),
            RoleId = 2,
            EmailVerificationToken = null,
            EmailVerificationTokenExpiry = null,
            RefreshToken = null,
            RefreshTokenExpiry = null,
            PasswordResetToken = null,
            PasswordResetTokenExpiry = null,
            TwoFactorSecretKey = null,
            LastLoginDate = DateTime.UtcNow.AddDays(-1)
        };
    }

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

    public static class UserDtos
    {
        public static UserDto ValidUserDto() => new()
        {
            UserId = 1,
            Username = "testuser123",
            Email = "test@example.com",
            FullName = "Test User",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static class EmailVerificationTokens
    {
        public static string ValidToken() => Guid.NewGuid().ToString();
        
        public static DateTime ValidExpiry() => DateTime.UtcNow.AddHours(24);
        
        public static DateTime ExpiredDate() => DateTime.UtcNow.AddHours(-1);
    }
} 