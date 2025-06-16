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
using QuantumBands.Application.Features.Wallets.Dtos;
using QuantumBands.Application.Features.Exchange.Commands.CreateOrder;
using QuantumBands.Application.Features.Exchange.Dtos;
using QuantumBands.Application.Features.Users.Commands.Setup2FA;
using QuantumBands.Application.Features.Users.Commands.Enable2FA;
using QuantumBands.Application.Features.Users.Commands.Verify2FA;
using QuantumBands.Application.Features.Users.Commands.Disable2FA;
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
} 