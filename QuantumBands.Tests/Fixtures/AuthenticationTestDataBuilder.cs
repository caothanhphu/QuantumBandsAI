using QuantumBands.Application.Features.Authentication.Commands.RegisterUser;
using QuantumBands.Application.Features.Authentication.Commands.Login;
using QuantumBands.Application.Features.Authentication.Commands.VerifyEmail;
using QuantumBands.Application.Features.Authentication.Commands.ForgotPassword;
using QuantumBands.Application.Features.Authentication.Commands.ResetPassword;
using QuantumBands.Application.Features.Authentication.Commands.ResendVerificationEmail;
using QuantumBands.Application.Features.Authentication;

namespace QuantumBands.Tests.Fixtures;

/// <summary>
/// Test data builder cho chức năng Authentication
/// </summary>
public static class AuthenticationTestDataBuilder
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
            Username = "ab",
            Email = "test@example.com",
            Password = "StrongPassword123!",
            FullName = "Test User"
        };

        public static RegisterUserCommand CommandWithLongUsername() => new()
        {
            Username = new string('a', 51),
            Email = "test@example.com",
            Password = "StrongPassword123!",
            FullName = "Test User"
        };

        public static RegisterUserCommand CommandWithInvalidUsername() => new()
        {
            Username = "test-user@",
            Email = "test@example.com",
            Password = "StrongPassword123!",
            FullName = "Test User"
        };

        public static RegisterUserCommand CommandWithInvalidEmail() => new()
        {
            Username = "testuser123",
            Email = "invalid-email",
            Password = "StrongPassword123!",
            FullName = "Test User"
        };

        public static RegisterUserCommand CommandWithLongEmail() => new()
        {
            Username = "testuser123",
            Email = new string('a', 240) + "@example.com",
            Password = "StrongPassword123!",
            FullName = "Test User"
        };

        public static RegisterUserCommand CommandWithWeakPassword() => new()
        {
            Username = "testuser123",
            Email = "test@example.com",
            Password = "weak",
            FullName = "Test User"
        };

        public static RegisterUserCommand CommandWithPasswordWithoutSpecialChars() => new()
        {
            Username = "testuser123",
            Email = "test@example.com",
            Password = "WeakPassword123",
            FullName = "Test User"
        };

        public static RegisterUserCommand CommandWithLongFullName() => new()
        {
            Username = "testuser123",
            Email = "test@example.com",
            Password = "StrongPassword123!",
            FullName = new string('a', 201)
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
            Token = "valid-verification-token-123"
        };

        public static VerifyEmailRequest RequestWithInvalidUserId() => new()
        {
            UserId = 999,
            Token = "valid-verification-token-123"
        };

        public static VerifyEmailRequest RequestWithNegativeUserId() => new()
        {
            UserId = -1,
            Token = "valid-verification-token-123"
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
            Token = "expired-verification-token-123"
        };

        public static VerifyEmailRequest RequestWithInvalidToken() => new()
        {
            UserId = 1,
            Token = "invalid-verification-token-123"
        };

        public static VerifyEmailRequest RequestWithNonExistentUser() => new()
        {
            UserId = 999,
            Token = "valid-verification-token-123"
        };

        public static VerifyEmailRequest RequestWithAlreadyVerifiedUser() => new()
        {
            UserId = 2,
            Token = "valid-verification-token-123"
        };

        public static VerifyEmailRequest RequestWithMalformedToken() => new()
        {
            UserId = 1,
            Token = "malformed@#$%token"
        };
    }

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
            Email = new string('a', 240) + "@example.com"
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
            ResetToken = "invalid-reset-token",
            NewPassword = "NewStrongPassword123!",
            ConfirmNewPassword = "NewStrongPassword123!"
        };

        public static ResetPasswordRequest RequestWithExpiredToken() => new()
        {
            Email = "test@example.com",
            ResetToken = "expired-reset-token-123",
            NewPassword = "NewStrongPassword123!",
            ConfirmNewPassword = "NewStrongPassword123!"
        };

        public static ResetPasswordRequest RequestWithUsedToken() => new()
        {
            Email = "test@example.com",
            ResetToken = "used-reset-token-123",
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
            Email = new string('a', 240) + "@example.com"
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
} 