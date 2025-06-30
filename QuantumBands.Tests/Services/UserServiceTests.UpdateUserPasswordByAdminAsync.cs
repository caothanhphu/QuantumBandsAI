using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.Application.Features.Admin.Users.Commands.UpdateUserPassword;
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Services;
using QuantumBands.Domain.Entities;
using QuantumBands.Tests.Common;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace QuantumBands.Tests.Services;

public class UserServiceUpdateUserPasswordByAdminAsyncTests : TestBase
{
    private readonly UserService _userService;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly Mock<ITwoFactorAuthService> _mockTwoFactorAuthService;
    private readonly Mock<IGenericRepository<User>> _mockUserRepository;

    public UserServiceUpdateUserPasswordByAdminAsyncTests()
    {
        _mockLogger = new Mock<ILogger<UserService>>();
        _mockTwoFactorAuthService = new Mock<ITwoFactorAuthService>();
        _mockUserRepository = new Mock<IGenericRepository<User>>();

        // Setup UnitOfWork to return our mock repository
        MockUnitOfWork.Setup(x => x.Users).Returns(_mockUserRepository.Object);

        _userService = new UserService(
            MockUnitOfWork.Object,
            _mockLogger.Object,
            _mockTwoFactorAuthService.Object,
            MockConfiguration.Object
        );
    }

    private static ClaimsPrincipal CreateAdminUser(int adminId = 1)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, adminId.ToString()),
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static ClaimsPrincipal CreateUnauthenticatedUser()
    {
        return new ClaimsPrincipal(new ClaimsIdentity());
    }

    private static User CreateTestUser(int userId = 2)
    {
        return new User
        {
            UserId = userId,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!"),
            FullName = "Test User",
            IsActive = true,
            RefreshToken = "existing-refresh-token",
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    #region Happy Path Tests

    [Fact]
    public async Task UpdateUserPasswordByAdminAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var adminUser = CreateAdminUser(1);
        var targetUser = CreateTestUser(2);
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = "Password reset requested by user"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(2))
            .ReturnsAsync(targetUser);

        MockUnitOfWork.Setup(x => x.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _userService.UpdateUserPasswordByAdminAsync(2, request, adminUser, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Password updated successfully. User will need to log in again.");

        // Verify password was hashed and updated
        targetUser.PasswordHash.Should().NotBe(BCrypt.Net.BCrypt.HashPassword("OldPassword123!"));
        BCrypt.Net.BCrypt.Verify("NewSecurePass123!", targetUser.PasswordHash).Should().BeTrue();

        // Verify refresh tokens were cleared
        targetUser.RefreshToken.Should().BeNull();
        targetUser.RefreshTokenExpiry.Should().BeNull();

        // Verify UpdatedAt was set
        targetUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify repository interactions
        _mockUserRepository.Verify(x => x.GetByIdAsync(2), Times.Once);
        MockUnitOfWork.Verify(x => x.Users.Update(targetUser), Times.Once);
        MockUnitOfWork.Verify(x => x.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserPasswordByAdminAsync_WithMinimalReason_ShouldReturnSuccess()
    {
        // Arrange
        var adminUser = CreateAdminUser(1);
        var targetUser = CreateTestUser(2);
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = null // No reason provided
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(2))
            .ReturnsAsync(targetUser);

        MockUnitOfWork.Setup(x => x.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _userService.UpdateUserPasswordByAdminAsync(2, request, adminUser, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Password updated successfully. User will need to log in again.");
    }

    #endregion

    #region Authentication/Authorization Tests

    [Fact]
    public async Task UpdateUserPasswordByAdminAsync_WithUnauthenticatedAdmin_ShouldReturnFailure()
    {
        // Arrange
        var unauthenticatedUser = CreateUnauthenticatedUser();
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = "Test"
        };

        // Act
        var result = await _userService.UpdateUserPasswordByAdminAsync(2, request, unauthenticatedUser, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Admin authentication required.");

        // Verify no repository calls were made
        _mockUserRepository.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        MockUnitOfWork.Verify(x => x.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserPasswordByAdminAsync_WithInvalidAdminClaims_ShouldReturnFailure()
    {
        // Arrange
        var invalidClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "admin")
            // Missing NameIdentifier claim
        }, "Test"));

        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = "Test"
        };

        // Act
        var result = await _userService.UpdateUserPasswordByAdminAsync(2, request, invalidClaims, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Admin authentication required.");

        // Verify no repository calls were made
        _mockUserRepository.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        MockUnitOfWork.Verify(x => x.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task UpdateUserPasswordByAdminAsync_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var adminUser = CreateAdminUser(1);
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = "Test"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.UpdateUserPasswordByAdminAsync(999, request, adminUser, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("User not found.");

        // Verify repository interactions
        _mockUserRepository.Verify(x => x.GetByIdAsync(999), Times.Once);
        MockUnitOfWork.Verify(x => x.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task UpdateUserPasswordByAdminAsync_WithDatabaseError_ShouldReturnFailure()
    {
        // Arrange
        var adminUser = CreateAdminUser(1);
        var targetUser = CreateTestUser(2);
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = "Test"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(2))
            .ReturnsAsync(targetUser);

        MockUnitOfWork.Setup(x => x.CompleteAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _userService.UpdateUserPasswordByAdminAsync(2, request, adminUser, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("An error occurred while updating the password.");

        // Verify repository interactions
        _mockUserRepository.Verify(x => x.GetByIdAsync(2), Times.Once);
        MockUnitOfWork.Verify(x => x.Users.Update(targetUser), Times.Once);
        MockUnitOfWork.Verify(x => x.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserPasswordByAdminAsync_WithRepositoryError_ShouldReturnFailure()
    {
        // Arrange
        var adminUser = CreateAdminUser(1);
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = "Test"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(2))
            .ThrowsAsync(new Exception("Repository error"));

        // Act
        var result = await _userService.UpdateUserPasswordByAdminAsync(2, request, adminUser, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("An error occurred while updating the password.");

        // Verify repository interactions
        _mockUserRepository.Verify(x => x.GetByIdAsync(2), Times.Once);
        MockUnitOfWork.Verify(x => x.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task UpdateUserPasswordByAdminAsync_ShouldHashPasswordSecurely()
    {
        // Arrange
        var adminUser = CreateAdminUser(1);
        var targetUser = CreateTestUser(2);
        var originalPasswordHash = targetUser.PasswordHash;
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = "Security test"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(2))
            .ReturnsAsync(targetUser);

        MockUnitOfWork.Setup(x => x.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _userService.UpdateUserPasswordByAdminAsync(2, request, adminUser, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        // Verify password security
        targetUser.PasswordHash.Should().NotBe(originalPasswordHash);
        targetUser.PasswordHash.Should().NotBe("NewSecurePass123!"); // Should not be plain text
        BCrypt.Net.BCrypt.Verify("NewSecurePass123!", targetUser.PasswordHash).Should().BeTrue();
        BCrypt.Net.BCrypt.Verify("WrongPassword", targetUser.PasswordHash).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUserPasswordByAdminAsync_ShouldInvalidateExistingSessions()
    {
        // Arrange
        var adminUser = CreateAdminUser(1);
        var targetUser = CreateTestUser(2);
        targetUser.RefreshToken = "existing-token";
        targetUser.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = "Session security test"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(2))
            .ReturnsAsync(targetUser);

        MockUnitOfWork.Setup(x => x.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _userService.UpdateUserPasswordByAdminAsync(2, request, adminUser, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        // Verify session invalidation
        targetUser.RefreshToken.Should().BeNull();
        targetUser.RefreshTokenExpiry.Should().BeNull();
    }

    #endregion

    #region Audit Logging Tests

    [Fact]
    public async Task UpdateUserPasswordByAdminAsync_ShouldLogAdminAction()
    {
        // Arrange
        var adminUser = CreateAdminUser(1);
        var targetUser = CreateTestUser(2);
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "NewSecurePass123!",
            ConfirmNewPassword = "NewSecurePass123!",
            Reason = "User forgot password"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(2))
            .ReturnsAsync(targetUser);

        MockUnitOfWork.Setup(x => x.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _userService.UpdateUserPasswordByAdminAsync(2, request, adminUser, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        // Verify logging occurred (we can verify the logger was called with appropriate log levels)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Admin 1 attempting to update password for UserID: 2")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Password updated successfully by Admin 1 for UserID: 2")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}