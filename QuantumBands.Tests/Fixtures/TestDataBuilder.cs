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

    // SCRUM-36: Test data for forgot password endpoint testing
    public static class ForgotPassword
    {
        public static ForgotPasswordRequest ValidRequest() => new()
        {
            Email = "test@example.com"
        };

        public static ForgotPasswordRequest ValidRequestWithExistingUser() => new()
        {
            Email = "existing@example.com"
        };

        public static ForgotPasswordRequest RequestWithInvalidEmail() => new()
        {
            Email = "invalid-email"
        };

        public static ForgotPasswordRequest RequestWithEmptyEmail() => new()
        {
            Email = ""
        };

        public static ForgotPasswordRequest RequestWithLongEmail() => new()
        {
            Email = new string('a', 240) + "@example.com" // Too long
        };

        public static ForgotPasswordRequest RequestWithNonExistentEmail() => new()
        {
            Email = "nonexistent@example.com"
        };

        public static ForgotPasswordRequest RequestWithInactiveUserEmail() => new()
        {
            Email = "inactive@example.com"
        };

        public static ForgotPasswordRequest RequestWithUnverifiedUserEmail() => new()
        {
            Email = "unverified@example.com"
        };

        public static ForgotPasswordRequest RequestForRateLimitTesting() => new()
        {
            Email = "ratelimit@example.com"
        };

        public static ForgotPasswordRequest RequestWithMalformedEmail() => new()
        {
            Email = "user@"
        };

        public static ForgotPasswordRequest RequestWithSpecialCharacterEmail() => new()
        {
            Email = "user+tag@example.com"
        };
    }

    // SCRUM-37: Test data for reset password endpoint testing
    public static class ResetPassword
    {
        public static ResetPasswordRequest ValidRequest() => new()
        {
            Email = "test@example.com",
            ResetToken = "valid-reset-token-123",
            NewPassword = "NewStrongPassword123!",
            ConfirmNewPassword = "NewStrongPassword123!"
        };

        public static ResetPasswordRequest ValidRequestWithExistingUser() => new()
        {
            Email = "existing@example.com",
            ResetToken = "valid-reset-token-456",
            NewPassword = "NewStrongPassword123!",
            ConfirmNewPassword = "NewStrongPassword123!"
        };

        public static ResetPasswordRequest RequestWithInvalidEmail() => new()
        {
            Email = "invalid-email",
            ResetToken = "valid-reset-token-123",
            NewPassword = "NewStrongPassword123!",
            ConfirmNewPassword = "NewStrongPassword123!"
        };

        public static ResetPasswordRequest RequestWithEmptyEmail() => new()
        {
            Email = "",
            ResetToken = "valid-reset-token-123",
            NewPassword = "NewStrongPassword123!",
            ConfirmNewPassword = "NewStrongPassword123!"
        };

        public static ResetPasswordRequest RequestWithEmptyToken() => new()
        {
            Email = "test@example.com",
            ResetToken = "",
            NewPassword = "NewStrongPassword123!",
            ConfirmNewPassword = "NewStrongPassword123!"
        };

        public static ResetPasswordRequest RequestWithInvalidToken() => new()
        {
            Email = "test@example.com",
            ResetToken = "invalid-token",
            NewPassword = "NewStrongPassword123!",
            ConfirmNewPassword = "NewStrongPassword123!"
        };

        public static ResetPasswordRequest RequestWithExpiredToken() => new()
        {
            Email = "test@example.com",
            ResetToken = "expired-token-789",
            NewPassword = "NewStrongPassword123!",
            ConfirmNewPassword = "NewStrongPassword123!"
        };

        public static ResetPasswordRequest RequestWithUsedToken() => new()
        {
            Email = "test@example.com",
            ResetToken = "used-token-111",
            NewPassword = "NewStrongPassword123!",
            ConfirmNewPassword = "NewStrongPassword123!"
        };

        public static ResetPasswordRequest RequestWithWeakPassword() => new()
        {
            Email = "test@example.com",
            ResetToken = "valid-reset-token-123",
            NewPassword = "weak",
            ConfirmNewPassword = "weak"
        };

        public static ResetPasswordRequest RequestWithPasswordNoSpecialChars() => new()
        {
            Email = "test@example.com",
            ResetToken = "valid-reset-token-123",
            NewPassword = "WeakPassword123",
            ConfirmNewPassword = "WeakPassword123"
        };

        public static ResetPasswordRequest RequestWithPasswordNoNumbers() => new()
        {
            Email = "test@example.com",
            ResetToken = "valid-reset-token-123",
            NewPassword = "WeakPassword!",
            ConfirmNewPassword = "WeakPassword!"
        };

        public static ResetPasswordRequest RequestWithPasswordNoUppercase() => new()
        {
            Email = "test@example.com",
            ResetToken = "valid-reset-token-123",
            NewPassword = "weakpassword123!",
            ConfirmNewPassword = "weakpassword123!"
        };

        public static ResetPasswordRequest RequestWithPasswordMismatch() => new()
        {
            Email = "test@example.com",
            ResetToken = "valid-reset-token-123",
            NewPassword = "NewStrongPassword123!",
            ConfirmNewPassword = "DifferentPassword123!"
        };

        public static ResetPasswordRequest RequestWithEmptyPassword() => new()
        {
            Email = "test@example.com",
            ResetToken = "valid-reset-token-123",
            NewPassword = "",
            ConfirmNewPassword = ""
        };

        public static ResetPasswordRequest RequestWithEmptyConfirmPassword() => new()
        {
            Email = "test@example.com",
            ResetToken = "valid-reset-token-123",
            NewPassword = "NewStrongPassword123!",
            ConfirmNewPassword = ""
        };

        public static ResetPasswordRequest RequestWithNonExistentUser() => new()
        {
            Email = "nonexistent@example.com",
            ResetToken = "valid-reset-token-123",
            NewPassword = "NewStrongPassword123!",
            ConfirmNewPassword = "NewStrongPassword123!"
        };

        public static ResetPasswordRequest RequestWithMalformedToken() => new()
        {
            Email = "test@example.com",
            ResetToken = "malformed@#$%token",
            NewPassword = "NewStrongPassword123!",
            ConfirmNewPassword = "NewStrongPassword123!"
        };

        public static ResetPasswordRequest RequestWithSpecialCharacterEmail() => new()
        {
            Email = "user+tag@example.com",
            ResetToken = "valid-reset-token-123",
            NewPassword = "NewStrongPassword123!",
            ConfirmNewPassword = "NewStrongPassword123!"
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

    // SCRUM-40: Test data for user profile endpoint testing
    public static class UserProfile
    {
        public static UserProfileDto ValidUserProfile() => new()
        {
            UserId = 1,
            Username = "testuser123",
            Email = "test@example.com",
            FullName = "Test User",
            RoleName = "Investor",
            IsEmailVerified = true,
            TwoFactorEnabled = false,
            CreatedAt = DateTime.UtcNow
        };

        public static UserProfileDto AdminUserProfile() => new()
        {
            UserId = 2,
            Username = "admin",
            Email = "admin@example.com",
            FullName = "Administrator",
            RoleName = "Admin",
            IsEmailVerified = true,
            TwoFactorEnabled = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        public static UserProfileDto UserProfileWithoutFullName() => new()
        {
            UserId = 3,
            Username = "usernoname",
            Email = "noname@example.com",
            FullName = null,
            RoleName = "Investor",
            IsEmailVerified = false,
            TwoFactorEnabled = false,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };

        public static UserProfileDto UnverifiedUserProfile() => new()
        {
            UserId = 4,
            Username = "unverifieduser",
            Email = "unverified@example.com",
            FullName = "Unverified User",
            RoleName = "Investor",
            IsEmailVerified = false,
            TwoFactorEnabled = false,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };

        public static UserProfileDto UserWith2FAEnabled() => new()
        {
            UserId = 5,
            Username = "secure_user",
            Email = "secure@example.com",
            FullName = "Secure User",
            RoleName = "Investor",
            IsEmailVerified = true,
            TwoFactorEnabled = true,
            CreatedAt = DateTime.UtcNow.AddDays(-14)
        };
    }

    // SCRUM-41: Test data for update user profile endpoint testing
    public static class UpdateUserProfile
    {
        public static UpdateUserProfileRequest ValidUpdateRequest() => new()
        {
            FullName = "Updated Full Name"
        };

        public static UpdateUserProfileRequest UpdateWithLongFullName() => new()
        {
            FullName = "This is a very long full name that contains exactly two hundred characters to test the maximum length validation rule for the FullName field in our system which should be rejected"
        };

        public static UpdateUserProfileRequest UpdateWithExtraLongFullName() => new()
        {
            FullName = new string('A', 201) // 201 characters - exceeds limit
        };

        public static UpdateUserProfileRequest UpdateWithEmptyFullName() => new()
        {
            FullName = ""
        };

        public static UpdateUserProfileRequest UpdateWithNullFullName() => new()
        {
            FullName = null
        };

        public static UpdateUserProfileRequest UpdateWithWhitespaceFullName() => new()
        {
            FullName = "   "
        };

        public static UpdateUserProfileRequest UpdateWithSpecialCharacters() => new()
        {
            FullName = "João São Paulo-Smith O'Connor Jr."
        };

        public static UpdateUserProfileRequest UpdateWithUnicodeCharacters() => new()
        {
            FullName = "测试 用户 名字 français ñoño"
        };

        public static UpdateUserProfileRequest UpdateWithMaxValidLength() => new()
        {
            FullName = new string('A', 200) // Exactly 200 characters
        };

        public static UpdateUserProfileRequest UpdateWithMinimalValidName() => new()
        {
            FullName = "A"
        };

        public static UpdateUserProfileRequest UpdateWithSameFullName() => new()
        {
            FullName = "Test User" // Same as existing user
        };

        public static UpdateUserProfileRequest UpdateToRemoveFullName() => new()
        {
            FullName = null // Explicitly remove full name
        };
    }

    // SCRUM-42: Test data for change password endpoint testing
    public static class ChangePassword
    {
        public static ChangePasswordRequest ValidChangePasswordRequest() => new()
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        public static ChangePasswordRequest RequestWithWrongCurrentPassword() => new()
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        public static ChangePasswordRequest RequestWithWeakNewPassword() => new()
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "weak",
            ConfirmNewPassword = "weak"
        };

        public static ChangePasswordRequest RequestWithPasswordMismatch() => new()
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "DifferentPassword123!"
        };

        public static ChangePasswordRequest RequestWithEmptyCurrentPassword() => new()
        {
            CurrentPassword = "",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        public static ChangePasswordRequest RequestWithEmptyNewPassword() => new()
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "",
            ConfirmNewPassword = ""
        };

        public static ChangePasswordRequest RequestWithEmptyConfirmPassword() => new()
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = ""
        };

        public static ChangePasswordRequest RequestWithSamePassword() => new()
        {
            CurrentPassword = "SamePassword123!",
            NewPassword = "SamePassword123!",
            ConfirmNewPassword = "SamePassword123!"
        };

        public static ChangePasswordRequest RequestWithPasswordTooShort() => new()
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "Short1!",
            ConfirmNewPassword = "Short1!"
        };

        public static ChangePasswordRequest RequestWithPasswordTooLong() => new()
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = new string('A', 95) + "1234!",  // 100+ characters
            ConfirmNewPassword = new string('A', 95) + "1234!"
        };

        public static ChangePasswordRequest RequestWithPasswordNoUppercase() => new()
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "newpassword123!",
            ConfirmNewPassword = "newpassword123!"
        };

        public static ChangePasswordRequest RequestWithPasswordNoLowercase() => new()
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "NEWPASSWORD123!",
            ConfirmNewPassword = "NEWPASSWORD123!"
        };

        public static ChangePasswordRequest RequestWithPasswordNoNumber() => new()
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "NewPassword!",
            ConfirmNewPassword = "NewPassword!"
        };

        public static ChangePasswordRequest RequestWithPasswordNoSpecialChar() => new()
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "NewPassword123",
            ConfirmNewPassword = "NewPassword123"
        };

        public static ChangePasswordRequest RequestWithSpecialCharacters() => new()
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "NewP@ssw0rd#2024$",
            ConfirmNewPassword = "NewP@ssw0rd#2024$"
        };

        public static ChangePasswordRequest RequestWithUnicodeCharacters() => new()
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "Nêwpässwörd123!",
            ConfirmNewPassword = "Nêwpässwörd123!"
        };

        public static ChangePasswordRequest RequestWithMinimumValidPassword() => new()
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "NewPass1!",
            ConfirmNewPassword = "NewPass1!"
        };

        public static ChangePasswordRequest RequestWithMaximumValidPassword() => new()
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = new string('A', 92) + "bc1!",  // Exactly 100 characters
            ConfirmNewPassword = new string('A', 92) + "bc1!"
        };
    }

    // SCRUM-39: Test data for resend verification email endpoint testing
    public static class ResendVerificationEmail
    {
        public static ResendVerificationEmailRequest ValidRequest() => new()
        {
            Email = "test@example.com"
        };

        public static ResendVerificationEmailRequest ValidRequestWithExistingUnverifiedUser() => new()
        {
            Email = "unverified@example.com"
        };

        public static ResendVerificationEmailRequest RequestWithInvalidEmail() => new()
        {
            Email = "invalid-email"
        };

        public static ResendVerificationEmailRequest RequestWithEmptyEmail() => new()
        {
            Email = ""
        };

        public static ResendVerificationEmailRequest RequestWithNullEmail() => new()
        {
            Email = null!
        };

        public static ResendVerificationEmailRequest RequestWithLongEmail() => new()
        {
            Email = new string('a', 240) + "@example.com" // Too long
        };

        public static ResendVerificationEmailRequest RequestWithNonExistentEmail() => new()
        {
            Email = "nonexistent@example.com"
        };

        public static ResendVerificationEmailRequest RequestWithAlreadyVerifiedEmail() => new()
        {
            Email = "verified@example.com"
        };

        public static ResendVerificationEmailRequest RequestWithInactiveUserEmail() => new()
        {
            Email = "inactive@example.com"
        };

        public static ResendVerificationEmailRequest RequestForRateLimitTesting() => new()
        {
            Email = "ratelimit@example.com"
        };

        public static ResendVerificationEmailRequest RequestWithMalformedEmail() => new()
        {
            Email = "user@"
        };

        public static ResendVerificationEmailRequest RequestWithSpecialCharacterEmail() => new()
        {
            Email = "user+tag@example.com"
        };

        public static ResendVerificationEmailRequest RequestWithCaseSensitiveEmail() => new()
        {
            Email = "Test@Example.COM"
        };

        public static ResendVerificationEmailRequest RequestForSpamPrevention() => new()
        {
            Email = "spam@example.com"
        };

        public static ResendVerificationEmailRequest RequestWithMaximumValidEmail() => new()
        {
            Email = "verylongusernamebutwithinlimits@example.com"
        };
    }

    public static class UserProfileUsers
    {
        public static User ValidUserWithRole() => new()
        {
            UserId = 1,
            Username = "testuser123",
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Test User",
            IsEmailVerified = true,
            TwoFactorEnabled = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RoleId = 2,
            Role = new() { RoleId = 2, RoleName = "Investor" },
            EmailVerificationToken = null,
            EmailVerificationTokenExpiry = null,
            RefreshToken = null,
            RefreshTokenExpiry = null,
            PasswordResetToken = null,
            PasswordResetTokenExpiry = null,
            TwoFactorSecretKey = null,
            LastLoginDate = DateTime.UtcNow.AddDays(-1)
        };

        public static User AdminUserWithRole() => new()
        {
            UserId = 2,
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Administrator",
            IsEmailVerified = true,
            TwoFactorEnabled = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-30),
            RoleId = 1,
            Role = new() { RoleId = 1, RoleName = "Admin" },
            EmailVerificationToken = null,
            EmailVerificationTokenExpiry = null,
            RefreshToken = null,
            RefreshTokenExpiry = null,
            PasswordResetToken = null,
            PasswordResetTokenExpiry = null,
            TwoFactorSecretKey = null,
            LastLoginDate = DateTime.UtcNow.AddHours(-2)
        };

        public static User UserWithoutRole() => new()
        {
            UserId = 3,
            Username = "noroleuser",
            Email = "norole@example.com",
            PasswordHash = "hashedpassword",
            FullName = "No Role User",
            IsEmailVerified = true,
            TwoFactorEnabled = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            UpdatedAt = DateTime.UtcNow.AddDays(-7),
            RoleId = 999, // Non-existent role
            Role = null, // No role associated
            EmailVerificationToken = null,
            EmailVerificationTokenExpiry = null,
            RefreshToken = null,
            RefreshTokenExpiry = null,
            PasswordResetToken = null,
            PasswordResetTokenExpiry = null,
            TwoFactorSecretKey = null,
            LastLoginDate = null
        };

        public static User UserWithoutFullName() => new()
        {
            UserId = 4,
            Username = "nofullname",
            Email = "nofullname@example.com",
            PasswordHash = "hashedpassword",
            FullName = null,
            IsEmailVerified = false,
            TwoFactorEnabled = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow.AddDays(-3),
            RoleId = 2,
            Role = new() { RoleId = 2, RoleName = "Investor" },
            EmailVerificationToken = null,
            EmailVerificationTokenExpiry = null,
            RefreshToken = null,
            RefreshTokenExpiry = null,
            PasswordResetToken = null,
            PasswordResetTokenExpiry = null,
            TwoFactorSecretKey = null,
            LastLoginDate = null
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
                    TransactionDate = DateTime.UtcNow.AddDays(-i)
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

    // SCRUM-62: Test data for GET /portfolio/me endpoint testing
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
    /// - TestDataBuilder.CancelBankDeposit.ValidRequest() - Returns valid cancellation request
    /// - TestDataBuilder.CancelBankDeposit.ValidCancelledTransactionDto() - Returns expected response
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
            TransactionId = 0,
            AdminNotes = "Invalid transaction ID test"
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
            AdminNotes = "Trying to cancel already confirmed deposit"
        };

        /// <summary>
        /// Cancel request for already cancelled deposit
        /// </summary>
        public static CancelBankDepositRequest RequestForAlreadyCancelledDeposit() => new()
        {
            TransactionId = 1003,
            AdminNotes = "Trying to cancel already cancelled deposit"
        };

        /// <summary>
        /// Valid cancelled transaction response
        /// </summary>
        public static WalletTransactionDto ValidCancelledTransactionDto() => new()
        {
            TransactionId = 1001,
            TransactionTypeName = "Bank Deposit",
            Amount = 1000.00m,
            CurrencyCode = "USD",
            BalanceAfter = 5000.00m,
            ReferenceId = "DEP001",
            PaymentMethod = "Bank Transfer",
            ExternalTransactionId = "BANK_TXN_001",
            Description = "Bank deposit cancelled by admin",
            Status = "Cancelled",
            TransactionDate = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow
        };

        /// <summary>
        /// Custom cancel request
        /// </summary>
        public static CancelBankDepositRequest CustomRequest(long transactionId, string adminNotes) => new()
        {
            TransactionId = transactionId,
            AdminNotes = adminNotes
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
    /// - TestDataBuilder.GetPendingWithdrawals.ValidQuery() - Returns valid query with default parameters
    /// - TestDataBuilder.GetPendingWithdrawals.PendingWithdrawalsResponse() - Returns expected response list
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
} 