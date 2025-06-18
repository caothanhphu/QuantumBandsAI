using QuantumBands.Application.Features.Authentication;
using QuantumBands.Application.Features.Users.Commands.ChangePassword;
using QuantumBands.Application.Features.Users.Commands.UpdateProfile;
using QuantumBands.Domain.Entities;

namespace QuantumBands.Tests.Fixtures;

/// <summary>
/// Test data builders for Users domain functionality.
/// Contains test data for user entities, DTOs, and user-related operations.
/// Extracted from TestDataBuilder.cs to improve organization and maintainability.
/// </summary>
public static class UsersTestDataBuilder
{
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
} 