using QuantumBands.Application.Features.Authentication.Commands.RegisterUser;
using QuantumBands.Application.Features.Authentication.Commands.Login;
using QuantumBands.Application.Features.Authentication.Commands.VerifyEmail;
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

    public static class Login
    {
        public static LoginRequest ValidLoginWithUsername() => new()
        {
            UsernameOrEmail = "testuser123",
            Password = "StrongPassword123!"
        };

        public static LoginRequest ValidLoginWithEmail() => new()
        {
            UsernameOrEmail = "test@example.com",
            Password = "StrongPassword123!"
        };

        public static LoginRequest LoginWithEmptyCredentials() => new()
        {
            UsernameOrEmail = "",
            Password = ""
        };

        public static LoginRequest LoginWithInvalidUsername() => new()
        {
            UsernameOrEmail = "nonexistentuser",
            Password = "StrongPassword123!"
        };

        public static LoginRequest LoginWithInvalidPassword() => new()
        {
            UsernameOrEmail = "testuser123",
            Password = "WrongPassword123!"
        };

        public static LoginRequest LoginWithInactiveUser() => new()
        {
            UsernameOrEmail = "inactiveuser",
            Password = "StrongPassword123!"
        };

        public static LoginRequest LoginWithUnverifiedEmail() => new()
        {
            UsernameOrEmail = "unverified@example.com",
            Password = "StrongPassword123!"
        };
    }

    public static class LoginResponses
    {
        public static LoginResponse ValidLoginResponse() => new()
        {
            UserId = 1,
            Username = "testuser123",
            Email = "test@example.com",
            Role = "Investor",
            JwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibmFtZSI6InRlc3R1c2VyMTIzIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
            RefreshToken = Guid.NewGuid().ToString(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)
        };

        public static LoginResponse AdminLoginResponse() => new()
        {
            UserId = 2,
            Username = "admin",
            Email = "admin@example.com",
            Role = "Admin",
            JwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIyIiwibmFtZSI6ImFkbWluIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
            RefreshToken = Guid.NewGuid().ToString(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)
        };
    }

    public static class EmailVerificationTokens
    {
        public static string ValidToken() => Guid.NewGuid().ToString();
        
        public static DateTime ValidExpiry() => DateTime.UtcNow.AddHours(24);
        
        public static DateTime ExpiredDate() => DateTime.UtcNow.AddHours(-1);
    }

    public static class VerifyEmail
    {
        public static VerifyEmailRequest ValidRequest() => new()
        {
            UserId = 1,
            Token = "valid-token-123"
        };

        public static VerifyEmailRequest RequestWithInvalidUserId() => new()
        {
            UserId = 0,
            Token = "valid-token-123"
        };

        public static VerifyEmailRequest RequestWithNegativeUserId() => new()
        {
            UserId = -1,
            Token = "valid-token-123"
        };

        public static VerifyEmailRequest RequestWithEmptyToken() => new()
        {
            UserId = 1,
            Token = ""
        };

        public static VerifyEmailRequest RequestWithNullToken() => new()
        {
            UserId = 1,
            Token = null!
        };

        public static VerifyEmailRequest RequestWithExpiredToken() => new()
        {
            UserId = 1,
            Token = "expired-token-456"
        };

        public static VerifyEmailRequest RequestWithInvalidToken() => new()
        {
            UserId = 1,
            Token = "invalid-token-789"
        };

        public static VerifyEmailRequest RequestWithNonExistentUser() => new()
        {
            UserId = 999,
            Token = "valid-token-123"
        };

        public static VerifyEmailRequest RequestWithAlreadyVerifiedUser() => new()
        {
            UserId = 2,
            Token = "valid-token-123"
        };

        public static VerifyEmailRequest RequestWithMalformedToken() => new()
        {
            UserId = 1,
            Token = "malformed@#$%^&*()"
        };
    }

    // SCRUM-35: Test data for refresh token endpoint testing
    public static class RefreshToken
    {
        public static QuantumBands.Application.Features.Authentication.Commands.RefreshToken.RefreshTokenRequest ValidRequest() => new()
        {
            ExpiredJwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibmFtZSI6InRlc3R1c2VyMTIzIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
            RefreshToken = "valid-refresh-token-guid"
        };

        public static QuantumBands.Application.Features.Authentication.Commands.RefreshToken.RefreshTokenRequest RequestWithEmptyRefreshToken() => new()
        {
            ExpiredJwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibmFtZSI6InRlc3R1c2VyMTIzIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
            RefreshToken = ""
        };

        public static QuantumBands.Application.Features.Authentication.Commands.RefreshToken.RefreshTokenRequest RequestWithEmptyJwtToken() => new()
        {
            ExpiredJwtToken = "",
            RefreshToken = "valid-refresh-token-guid"
        };

        public static QuantumBands.Application.Features.Authentication.Commands.RefreshToken.RefreshTokenRequest RequestWithInvalidToken() => new()
        {
            ExpiredJwtToken = "invalid-jwt-token",
            RefreshToken = "invalid-refresh-token"
        };

        public static QuantumBands.Application.Features.Authentication.Commands.RefreshToken.RefreshTokenRequest RequestWithExpiredRefreshToken() => new()
        {
            ExpiredJwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibmFtZSI6InRlc3R1c2VyMTIzIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
            RefreshToken = "expired-refresh-token"
        };

        public static QuantumBands.Application.Features.Authentication.Commands.RefreshToken.RefreshTokenRequest RequestWithRevokedToken() => new()
        {
            ExpiredJwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibmFtZSI6InRlc3R1c2VyMTIzIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
            RefreshToken = "revoked-refresh-token"
        };

        public static QuantumBands.Application.Features.Authentication.Commands.RefreshToken.RefreshTokenRequest RequestWithNonExistentToken() => new()
        {
            ExpiredJwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibmFtZSI6InRlc3R1c2VyMTIzIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
            RefreshToken = "non-existent-token"
        };

        public static QuantumBands.Application.Features.Authentication.Commands.RefreshToken.RefreshTokenRequest RequestForTokenReuse() => new()
        {
            ExpiredJwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibmFtZSI6InRlc3R1c2VyMTIzIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
            RefreshToken = "reused-refresh-token"
        };
    }

    public static class VerifyEmailUsers
    {
        public static User UnverifiedUser() => new()
        {
            UserId = 1,
            Username = "unverifieduser",
            Email = "unverified@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Unverified User",
            IsEmailVerified = false,
            TwoFactorEnabled = false,
            IsActive = false,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-1),
            RoleId = 2,
            EmailVerificationToken = "valid-token-123",
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            RefreshToken = null,
            RefreshTokenExpiry = null,
            PasswordResetToken = null,
            PasswordResetTokenExpiry = null,
            TwoFactorSecretKey = null,
            LastLoginDate = null
        };

        public static User AlreadyVerifiedUser() => new()
        {
            UserId = 2,
            Username = "verifieduser",
            Email = "verified@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Verified User",
            IsEmailVerified = true,
            TwoFactorEnabled = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            RoleId = 2,
            EmailVerificationToken = null,
            EmailVerificationTokenExpiry = null,
            RefreshToken = null,
            RefreshTokenExpiry = null,
            PasswordResetToken = null,
            PasswordResetTokenExpiry = null,
            TwoFactorSecretKey = null,
            LastLoginDate = DateTime.UtcNow.AddHours(-2)
        };

        public static User UserWithExpiredToken() => new()
        {
            UserId = 3,
            Username = "expireduser",
            Email = "expired@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Expired Token User",
            IsEmailVerified = false,
            TwoFactorEnabled = false,
            IsActive = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            RoleId = 2,
            EmailVerificationToken = "expired-token-456",
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(-1),
            RefreshToken = null,
            RefreshTokenExpiry = null,
            PasswordResetToken = null,
            PasswordResetTokenExpiry = null,
            TwoFactorSecretKey = null,
            LastLoginDate = null
        };

        public static User UserWithInvalidToken() => new()
        {
            UserId = 4,
            Username = "invalidtokenuser",
            Email = "invalidtoken@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Invalid Token User",
            IsEmailVerified = false,
            TwoFactorEnabled = false,
            IsActive = false,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-2),
            RoleId = 2,
            EmailVerificationToken = "different-token-111",
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            RefreshToken = null,
            RefreshTokenExpiry = null,
            PasswordResetToken = null,
            PasswordResetTokenExpiry = null,
            TwoFactorSecretKey = null,
            LastLoginDate = null
        };
    }
} 