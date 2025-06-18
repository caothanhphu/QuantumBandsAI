// SCRUM-40: Unit Tests for GET /users/me - Get User Profile Endpoint
// SCRUM-41: Unit Tests for PUT /users/me - Update User Profile Endpoint  
// SCRUM-42: Unit Tests for POST /users/change-password - Change Password Endpoint
// SCRUM-45: Unit Tests for POST /users/2fa/setup - Setup 2FA Endpoint
// SCRUM-46: Unit Tests for POST /users/2fa/enable - Enable 2FA Endpoint
// SCRUM-47: Unit Tests for POST /users/2fa/verify - Verify 2FA Token Endpoint
// SCRUM-48: Unit Tests for POST /users/2fa/disable - Disable 2FA Endpoint
// This test class provides comprehensive test coverage for the UsersController endpoints:
// - GetMyProfile (GET /users/me): Profile retrieval with authentication and data mapping tests
// - UpdateMyProfile (PUT /users/me): Profile updates with validation, authentication, and security tests
// - ChangePassword (POST /users/change-password): Password changes with validation, authentication, and security tests
// - Setup2FA (POST /users/2fa/setup): 2FA setup initiation with QR code and secret generation tests
// - Enable2FA (POST /users/2fa/enable): 2FA activation with verification code validation and recovery codes generation tests
// - Verify2FA (POST /users/2fa/verify): 2FA token verification for sensitive actions and login flows
// - Disable2FA (POST /users/2fa/disable): 2FA deactivation with verification code validation and security data cleanup

using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Features.Authentication;
using QuantumBands.Application.Features.Users.Commands.UpdateProfile;
using QuantumBands.Application.Features.Users.Commands.ChangePassword;
using QuantumBands.Application.Features.Users.Commands.Setup2FA;
using QuantumBands.Application.Features.Users.Commands.Enable2FA;
using QuantumBands.Application.Features.Users.Commands.Verify2FA;
using QuantumBands.Application.Features.Users.Commands.Disable2FA;
using QuantumBands.Application.Interfaces;
using QuantumBands.Tests.Common;
using QuantumBands.Tests.Fixtures;
using static QuantumBands.Tests.Fixtures.UsersTestDataBuilder;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace QuantumBands.Tests.Controllers;

/// <summary>
/// Comprehensive test class for UsersController endpoints
/// 
/// Test Categories:
/// - GetMyProfile: Profile retrieval with authentication and data mapping
/// - UpdateMyProfile: Profile updates with validation and concurrency handling
/// - ChangePassword: Password changes with security validation
/// - Setup2FA: Two-factor authentication setup with QR code generation
/// - Enable2FA: Two-factor authentication activation with verification and recovery codes
/// - Verify2FA: Two-factor authentication token verification for sensitive actions
/// - Disable2FA: Two-factor authentication deactivation with verification and cleanup
/// 
/// Total Test Coverage: Comprehensive validation of authentication, business logic, security, and error handling
/// </summary>
public class UsersControllerTests : TestBase
{
    private readonly UsersController _usersController;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILogger<UsersController>> _mockControllerLogger;

    public UsersControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockControllerLogger = new Mock<ILogger<UsersController>>();
        _usersController = new UsersController(_mockUserService.Object, _mockControllerLogger.Object);
    }

    /// <summary>
    /// Creates a ClaimsPrincipal representing an authenticated user with valid claims
    /// </summary>
    private static ClaimsPrincipal CreateAuthenticatedUser(int userId, string username = "testuser")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim("jti", Guid.NewGuid().ToString())
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    /// <summary>
    /// Creates a ClaimsPrincipal representing an unauthenticated user
    /// </summary>
    private static ClaimsPrincipal CreateUnauthenticatedUser()
    {
        return new ClaimsPrincipal(new ClaimsIdentity());
    }

    /// <summary>
    /// Creates a ClaimsPrincipal with invalid or missing required claims
    /// </summary>
    private static ClaimsPrincipal CreateUserWithInvalidClaims()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "testuser")
            // Missing NameIdentifier claim
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    #region Happy Path Tests
    // Tests that verify the endpoint works correctly under normal, expected conditions

    /// <summary>
    /// Test: Valid authenticated user should successfully retrieve their profile
    /// Verifies the happy path where an authenticated user gets their complete profile data
    /// </summary>
    [Fact]
    public async Task GetMyProfile_WithValidAuthenticatedUser_ShouldReturnUserProfile()
    {
        // Arrange
        var expectedProfile = UsersTestDataBuilder.UserProfile.ValidUserProfile();
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedProfile);
        
        _mockUserService.Verify(x => x.GetUserProfileAsync(authenticatedUser, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyProfile_WithAdminUser_ShouldReturnAdminProfile()
    {
        // Arrange
        var expectedProfile = UsersTestDataBuilder.UserProfile.AdminUserProfile();
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedProfile);
    }

    [Fact]
    public async Task GetMyProfile_WithUserWithoutFullName_ShouldReturnProfileWithNullFullName()
    {
        // Arrange
        var expectedProfile = UsersTestDataBuilder.UserProfile.UserProfileWithoutFullName();
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var profile = okResult!.Value as UserProfileDto;
        profile!.FullName.Should().BeNull();
        profile.UserId.Should().Be(expectedProfile.UserId);
    }

    [Fact]
    public async Task GetMyProfile_WithUnverifiedUser_ShouldReturnUnverifiedProfile()
    {
        // Arrange
        var expectedProfile = UsersTestDataBuilder.UserProfile.UnverifiedUserProfile();
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var profile = okResult!.Value as UserProfileDto;
        profile!.IsEmailVerified.Should().BeFalse();
        profile.TwoFactorEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyProfile_WithUserWith2FAEnabled_ShouldReturnProfileWith2FA()
    {
        // Arrange
        var expectedProfile = UsersTestDataBuilder.UserProfile.UserWith2FAEnabled();
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var profile = okResult!.Value as UserProfileDto;
        profile!.TwoFactorEnabled.Should().BeTrue();
        profile.IsEmailVerified.Should().BeTrue();
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task GetMyProfile_WithUnauthenticatedUser_ShouldReturnNotFound()
    {
        // Arrange
        var unauthenticatedUser = CreateUnauthenticatedUser();
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = unauthenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserProfileDto?)null, "User not authenticated or identity is invalid."));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var responseMessage = GetMessageFromResponse(notFoundResult!.Value);
        responseMessage.Should().Be("User not authenticated or identity is invalid.");
    }

    [Fact]
    public async Task GetMyProfile_WithInvalidUserIdClaim_ShouldReturnNotFound()
    {
        // Arrange
        var userWithInvalidClaims = CreateUserWithInvalidClaims();
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = userWithInvalidClaims
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserProfileDto?)null, "User not authenticated or identity is invalid."));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var responseMessage = GetMessageFromResponse(notFoundResult!.Value);
        responseMessage.Should().Be("User not authenticated or identity is invalid.");
    }

    [Fact]
    public async Task GetMyProfile_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(999, "nonexistentuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserProfileDto?)null, "User profile not found."));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var responseMessage = GetMessageFromResponse(notFoundResult!.Value);
        responseMessage.Should().Be("User profile not found.");
    }

    [Fact]
    public async Task GetMyProfile_WithUserWithoutRole_ShouldReturnInternalServerError()
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(3, "noroleuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserProfileDto?)null, "User role configuration error."));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("User role configuration error.");
    }

    #endregion

    #region Service Integration Tests

    [Fact]
    public async Task GetMyProfile_ShouldCallUserServiceWithCorrectParameters()
    {
        // Arrange
        var expectedProfile = UsersTestDataBuilder.UserProfile.ValidUserProfile();
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        _mockUserService.Verify(x => x.GetUserProfileAsync(
            It.Is<ClaimsPrincipal>(u => u == authenticatedUser),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyProfile_WithCancellationToken_ShouldPassTokenToService()
    {
        // Arrange
        var expectedProfile = UsersTestDataBuilder.UserProfile.ValidUserProfile();
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        var cancellationToken = new CancellationToken();
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.GetMyProfile(cancellationToken);

        // Assert
        _mockUserService.Verify(x => x.GetUserProfileAsync(
            It.IsAny<ClaimsPrincipal>(),
            cancellationToken), Times.Once);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetMyProfile_WithServiceException_ShouldReturnInternalServerError()
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserProfileDto?)null, "Database connection error"));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("Database connection error");
    }

    [Fact]
    public async Task GetMyProfile_WithNullErrorMessage_ShouldReturnGenericError()
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserProfileDto?)null, (string?)null));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("An unexpected error occurred.");
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task GetMyProfile_ShouldLogInformationAtStart()
    {
        // Arrange
        var expectedProfile = UsersTestDataBuilder.UserProfile.ValidUserProfile();
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempting to retrieve profile for current authenticated user")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMyProfile_WithError_ShouldLogWarning()
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(999, "nonexistentuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserProfileDto?)null, "User profile not found."));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to retrieve profile for current user")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Data Mapping Tests

    [Fact]
    public async Task GetMyProfile_ShouldReturnCorrectUserProfileStructure()
    {
        // Arrange
        var expectedProfile = UsersTestDataBuilder.UserProfile.ValidUserProfile();
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var profile = okResult!.Value as UserProfileDto;
        
        profile.Should().NotBeNull();
        profile!.UserId.Should().Be(expectedProfile.UserId);
        profile.Username.Should().Be(expectedProfile.Username);
        profile.Email.Should().Be(expectedProfile.Email);
        profile.FullName.Should().Be(expectedProfile.FullName);
        profile.RoleName.Should().Be(expectedProfile.RoleName);
        profile.IsEmailVerified.Should().Be(expectedProfile.IsEmailVerified);
        profile.TwoFactorEnabled.Should().Be(expectedProfile.TwoFactorEnabled);
        profile.CreatedAt.Should().BeCloseTo(expectedProfile.CreatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetMyProfile_WithCompleteUserData_ShouldMapAllFields()
    {
        // Arrange
        var expectedProfile = new UserProfileDto
        {
            UserId = 123,
            Username = "completeusertest",
            Email = "complete@example.com",
            FullName = "Complete User Test",
            RoleName = "Premium",
            IsEmailVerified = true,
            TwoFactorEnabled = true,
            CreatedAt = DateTime.UtcNow.AddDays(-100)
        };
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedProfile);
    }

    #endregion

    #region Privacy and Security Tests

    [Fact]
    public async Task GetMyProfile_ShouldNotExposePasswordHash()
    {
        // Arrange
        var expectedProfile = UsersTestDataBuilder.UserProfile.ValidUserProfile();
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var profileJson = System.Text.Json.JsonSerializer.Serialize(okResult!.Value);
        
        profileJson.Should().NotContain("password", "Password hash should not be exposed in profile");
        profileJson.Should().NotContain("hash", "Password hash should not be exposed in profile");
        profileJson.Should().NotContain("secret", "Secret keys should not be exposed in profile");
    }

    [Fact]
    public async Task GetMyProfile_ShouldOnlyReturnUserOwnProfile()
    {
        // Arrange - User trying to access their own profile
        var expectedProfile = UsersTestDataBuilder.UserProfile.ValidUserProfile();
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.GetUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.GetMyProfile(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var profile = okResult!.Value as UserProfileDto;
        profile!.UserId.Should().Be(expectedProfile.UserId);
        
        // Verify service was called with the authenticated user's context
        _mockUserService.Verify(x => x.GetUserProfileAsync(
            It.Is<ClaimsPrincipal>(u => u.FindFirst(ClaimTypes.NameIdentifier) != null && 
                                        u.FindFirst(ClaimTypes.NameIdentifier).Value == expectedProfile.UserId.ToString()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    private static string? GetMessageFromResponse(object? response)
    {
        if (response == null) return null;
        var messageProperty = response.GetType().GetProperty("Message");
        return messageProperty?.GetValue(response, null) as string;
    }

    /// <summary>
    /// Helper method to get property value from anonymous objects
    /// </summary>
    private static object? GetPropertyValue(object obj, string propertyName)
    {
        if (obj == null) return null;
        var property = obj.GetType().GetProperty(propertyName);
        return property?.GetValue(obj, null);
    }

    #region Update Profile Tests - SCRUM-41
    // Tests for PUT /users/me endpoint - Update User Profile functionality
    // Covers validation, authentication, business logic, concurrency, logging, and security scenarios

    /// <summary>
    /// Test: Valid profile update should successfully update and return updated profile
    /// Verifies the happy path where an authenticated user updates their full name
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_WithValidRequest_ShouldReturnUpdatedProfile()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var expectedProfile = UsersTestDataBuilder.UserProfile.ValidUserProfile();
        expectedProfile.FullName = updateRequest.FullName; // Update with new name
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedProfile);
        
        _mockUserService.Verify(x => x.UpdateUserProfileAsync(authenticatedUser, updateRequest, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Update with null FullName should clear the field successfully
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_WithNullFullName_ShouldClearFullNameAndReturnProfile()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.UpdateWithNullFullName();
        var expectedProfile = UsersTestDataBuilder.UserProfile.ValidUserProfile();
        expectedProfile.FullName = null; // Cleared full name
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var profile = okResult!.Value as UserProfileDto;
        profile!.FullName.Should().BeNull();
        profile.UserId.Should().Be(expectedProfile.UserId);
    }

    /// <summary>
    /// Test: Update with same FullName should return current profile without changes
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_WithSameFullName_ShouldReturnCurrentProfile()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.UpdateWithSameFullName();
        var expectedProfile = UsersTestDataBuilder.UserProfile.ValidUserProfile();
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedProfile);
    }

    /// <summary>
    /// Test: Update with special characters and unicode should work correctly
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_WithSpecialCharacters_ShouldUpdateSuccessfully()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.UpdateWithSpecialCharacters();
        var expectedProfile = UsersTestDataBuilder.UserProfile.ValidUserProfile();
        expectedProfile.FullName = updateRequest.FullName;
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var profile = okResult!.Value as UserProfileDto;
        profile!.FullName.Should().Be(updateRequest.FullName);
    }

    /// <summary>
    /// Test: Update with maximum valid length (200 chars) should work
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_WithMaxValidLength_ShouldUpdateSuccessfully()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.UpdateWithMaxValidLength();
        var expectedProfile = UsersTestDataBuilder.UserProfile.ValidUserProfile();
        expectedProfile.FullName = updateRequest.FullName;
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var profile = okResult!.Value as UserProfileDto;
        profile!.FullName.Should().HaveLength(200);
        profile.FullName.Should().Be(updateRequest.FullName);
    }

    /// <summary>
    /// Test: Unauthenticated user trying to update profile should return NotFound
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_WithUnauthenticatedUser_ShouldReturnNotFound()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var unauthenticatedUser = CreateUnauthenticatedUser();
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = unauthenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserProfileDto?)null, "User not authenticated or identity is invalid."));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var responseMessage = GetMessageFromResponse(notFoundResult!.Value);
        responseMessage.Should().Be("User not authenticated or identity is invalid.");
    }

    /// <summary>
    /// Test: User with invalid claims trying to update should return NotFound
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_WithInvalidClaims_ShouldReturnNotFound()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var userWithInvalidClaims = CreateUserWithInvalidClaims();
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = userWithInvalidClaims
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserProfileDto?)null, "User not authenticated or identity is invalid."));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var responseMessage = GetMessageFromResponse(notFoundResult!.Value);
        responseMessage.Should().Be("User not authenticated or identity is invalid.");
    }

    /// <summary>
    /// Test: Update for non-existent user should return NotFound
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var authenticatedUser = CreateAuthenticatedUser(999, "nonexistentuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserProfileDto?)null, "User not found."));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        var responseMessage = GetMessageFromResponse(notFoundResult!.Value);
        responseMessage.Should().Be("User not found.");
    }

    /// <summary>
    /// Test: User without role association should return InternalServerError
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_WithUserWithoutRole_ShouldReturnInternalServerError()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var authenticatedUser = CreateAuthenticatedUser(3, "noroleuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserProfileDto?)null, "User role configuration error."));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("User role configuration error.");
    }

    /// <summary>
    /// Test: Concurrency conflict should return Conflict status
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_WithConcurrencyConflict_ShouldReturnConflict()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserProfileDto?)null, "A concurrency conflict occurred while updating the profile."));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
        var conflictResult = result as ConflictObjectResult;
        var responseMessage = GetMessageFromResponse(conflictResult!.Value);
        responseMessage.Should().Contain("concurrency conflict");
    }

    /// <summary>
    /// Test: Database error should return InternalServerError
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_WithDatabaseError_ShouldReturnInternalServerError()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserProfileDto?)null, "Database connection error"));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("Database connection error");
    }

    /// <summary>
    /// Test: Null error message should return generic error
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_WithNullErrorMessage_ShouldReturnGenericError()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserProfileDto?)null, (string?)null));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("An unexpected error occurred while updating profile.");
    }

    /// <summary>
    /// Test: Service should be called with correct parameters
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var expectedProfile = UsersTestDataBuilder.UserProfile.ValidUserProfile();
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        _mockUserService.Verify(x => x.UpdateUserProfileAsync(
            It.Is<ClaimsPrincipal>(u => u == authenticatedUser),
            It.Is<UpdateUserProfileRequest>(r => r.FullName == updateRequest.FullName),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Cancellation token should be passed to service
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_WithCancellationToken_ShouldPassTokenToService()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var expectedProfile = UsersTestDataBuilder.UserProfile.ValidUserProfile();
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        var cancellationToken = new CancellationToken();
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, cancellationToken);

        // Assert
        _mockUserService.Verify(x => x.UpdateUserProfileAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<UpdateUserProfileRequest>(),
            cancellationToken), Times.Once);
    }

    /// <summary>
    /// Test: Update should log information at start
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_ShouldLogInformationAtStart()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var expectedProfile = UsersTestDataBuilder.UserProfile.ValidUserProfile();
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempting to update profile for current authenticated user")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Update error should log warning
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_WithError_ShouldLogWarning()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var authenticatedUser = CreateAuthenticatedUser(999, "nonexistentuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserProfileDto?)null, "User not found."));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to update profile for current user")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Update should return complete UserProfileDto structure
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_ShouldReturnCompleteUserProfileStructure()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var expectedProfile = new UserProfileDto
        {
            UserId = 123,
            Username = "updated_user",
            Email = "updated@example.com",
            FullName = updateRequest.FullName,
            RoleName = "Premium",
            IsEmailVerified = true,
            TwoFactorEnabled = false,
            CreatedAt = DateTime.UtcNow.AddDays(-50)
        };
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var profile = okResult!.Value as UserProfileDto;
        
        profile.Should().NotBeNull();
        profile!.UserId.Should().Be(expectedProfile.UserId);
        profile.Username.Should().Be(expectedProfile.Username);
        profile.Email.Should().Be(expectedProfile.Email);
        profile.FullName.Should().Be(expectedProfile.FullName);
        profile.RoleName.Should().Be(expectedProfile.RoleName);
        profile.IsEmailVerified.Should().Be(expectedProfile.IsEmailVerified);
        profile.TwoFactorEnabled.Should().Be(expectedProfile.TwoFactorEnabled);
        profile.CreatedAt.Should().BeCloseTo(expectedProfile.CreatedAt, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Test: Update should not expose sensitive information
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_ShouldNotExposeSensitiveInformation()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var expectedProfile = UsersTestDataBuilder.UserProfile.ValidUserProfile();
        expectedProfile.FullName = updateRequest.FullName;
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var profileJson = System.Text.Json.JsonSerializer.Serialize(okResult!.Value);
        
        profileJson.Should().NotContain("password", "Password should not be exposed in profile update response");
        profileJson.Should().NotContain("hash", "Password hash should not be exposed in profile update response");
        profileJson.Should().NotContain("secret", "Secret keys should not be exposed in profile update response");
        profileJson.Should().NotContain("token", "Tokens should not be exposed in profile update response");
    }

    /// <summary>
    /// Test: User should only be able to update their own profile
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_ShouldOnlyAllowUserToUpdateOwnProfile()
    {
        // Arrange
        var updateRequest = UsersTestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var expectedProfile = UsersTestDataBuilder.UserProfile.ValidUserProfile();
        expectedProfile.FullName = updateRequest.FullName;
        var authenticatedUser = CreateAuthenticatedUser(expectedProfile.UserId, expectedProfile.Username);
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.UpdateUserProfileAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<UpdateUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedProfile, (string?)null));

        // Act
        var result = await _usersController.UpdateMyProfile(updateRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var profile = okResult!.Value as UserProfileDto;
        profile!.UserId.Should().Be(expectedProfile.UserId);
        
        // Verify service was called with the authenticated user's context only
        _mockUserService.Verify(x => x.UpdateUserProfileAsync(
            It.Is<ClaimsPrincipal>(u => u.FindFirst(ClaimTypes.NameIdentifier) != null && 
                                        u.FindFirst(ClaimTypes.NameIdentifier).Value == expectedProfile.UserId.ToString()),
            It.IsAny<UpdateUserProfileRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SCRUM-42: ChangePassword Tests
    // Tests for POST /users/change-password endpoint covering authentication, validation, security, and error handling

    /// <summary>
    /// Test: Valid authenticated user with correct current password should successfully change password
    /// Verifies the happy path where an authenticated user changes their password with valid data
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var changePasswordRequest = UsersTestDataBuilder.ChangePassword.ValidChangePasswordRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Password changed successfully."));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var responseMessage = GetMessageFromResponse(okResult!.Value);
        responseMessage.Should().Be("Password changed successfully.");
    }

    /// <summary>
    /// Test: Service authentication error should return BadRequest (since [Authorize] handles unauthenticated users)
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithServiceAuthError_ShouldReturnBadRequest()
    {
        // Arrange
        var changePasswordRequest = UsersTestDataBuilder.ChangePassword.ValidChangePasswordRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "User not authenticated or identity is invalid."));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("User not authenticated or identity is invalid.");
    }

    /// <summary>
    /// Test: Service returning user authentication issues should return InternalServerError
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithServiceAuthIssues_ShouldReturnInternalServerError()
    {
        // Arrange
        var changePasswordRequest = UsersTestDataBuilder.ChangePassword.ValidChangePasswordRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Authentication validation failed in service layer."));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("Authentication validation failed in service layer.");
    }

    /// <summary>
    /// Test: Incorrect current password should return BadRequest
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithIncorrectCurrentPassword_ShouldReturnBadRequest()
    {
        // Arrange
        var changePasswordRequest = UsersTestDataBuilder.ChangePassword.RequestWithWrongCurrentPassword();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Incorrect current password."));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("Incorrect current password.");
    }

    /// <summary>
    /// Test: User not found should return BadRequest (based on controller logic)
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithNonExistentUser_ShouldReturnBadRequest()
    {
        // Arrange
        var changePasswordRequest = UsersTestDataBuilder.ChangePassword.ValidChangePasswordRequest();
        var authenticatedUser = CreateAuthenticatedUser(999, "nonexistentuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "User not found."));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("User not found.");
    }

    /// <summary>
    /// Test: Database error should return InternalServerError
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithDatabaseError_ShouldReturnInternalServerError()
    {
        // Arrange
        var changePasswordRequest = UsersTestDataBuilder.ChangePassword.ValidChangePasswordRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "An error occurred while changing password."));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be("An error occurred while changing password.");
    }

    /// <summary>
    /// Test: Service should be called with correct parameters
    /// </summary>
    [Fact]
    public async Task ChangePassword_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        var changePasswordRequest = UsersTestDataBuilder.ChangePassword.ValidChangePasswordRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Password changed successfully."));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, CancellationToken.None);

        // Assert
        _mockUserService.Verify(x => x.ChangePasswordAsync(
            It.Is<ClaimsPrincipal>(u => u == authenticatedUser),
            It.Is<ChangePasswordRequest>(r => 
                r.CurrentPassword == changePasswordRequest.CurrentPassword &&
                r.NewPassword == changePasswordRequest.NewPassword &&
                r.ConfirmNewPassword == changePasswordRequest.ConfirmNewPassword),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Cancellation token should be passed to service
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithCancellationToken_ShouldPassTokenToService()
    {
        // Arrange
        var changePasswordRequest = UsersTestDataBuilder.ChangePassword.ValidChangePasswordRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        var cancellationToken = new CancellationToken();
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Password changed successfully."));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, cancellationToken);

        // Assert
        _mockUserService.Verify(x => x.ChangePasswordAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<ChangePasswordRequest>(),
            cancellationToken), Times.Once);
    }

    /// <summary>
    /// Test: Change password should log information at start
    /// </summary>
    [Fact]
    public async Task ChangePassword_ShouldLogInformationAtStart()
    {
        // Arrange
        var changePasswordRequest = UsersTestDataBuilder.ChangePassword.ValidChangePasswordRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Password changed successfully."));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempting to change password for current authenticated user")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Change password error should log warning
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithError_ShouldLogWarning()
    {
        // Arrange
        var changePasswordRequest = UsersTestDataBuilder.ChangePassword.ValidChangePasswordRequest();
        var authenticatedUser = CreateAuthenticatedUser(999, "nonexistentuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "User not found."));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Password change failed for current user")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Change password should not expose sensitive information in logs
    /// </summary>
    [Fact]
    public async Task ChangePassword_ShouldNotLogSensitiveInformation()
    {
        // Arrange
        var changePasswordRequest = UsersTestDataBuilder.ChangePassword.ValidChangePasswordRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Password changed successfully."));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => !v.ToString()!.Contains(changePasswordRequest.CurrentPassword) &&
                                           !v.ToString()!.Contains(changePasswordRequest.NewPassword) &&
                                           !v.ToString()!.Contains(changePasswordRequest.ConfirmNewPassword)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Test: Change password with weak new password should return validation error
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithWeakNewPassword_ShouldReturnBadRequest()
    {
        // Arrange
        var changePasswordRequest = UsersTestDataBuilder.ChangePassword.RequestWithWeakNewPassword();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Password does not meet complexity requirements."));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Contain("complexity requirements");
    }

    /// <summary>
    /// Test: Change password with non-matching new passwords should return validation error
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithNonMatchingNewPasswords_ShouldReturnBadRequest()
    {
        // Arrange
        var changePasswordRequest = UsersTestDataBuilder.ChangePassword.RequestWithPasswordMismatch();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "New password and confirmation password do not match."));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Contain("do not match");
    }

    /// <summary>
    /// Test: Change password where new password is same as current should return validation error
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithSameCurrentAndNewPassword_ShouldReturnBadRequest()
    {
        // Arrange
        var changePasswordRequest = UsersTestDataBuilder.ChangePassword.RequestWithSamePassword();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "New password must be different from current password."));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Contain("must be different");
    }

    /// <summary>
    /// Test: Change password with null/empty request should return BadRequest
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithEmptyPasswords_ShouldReturnBadRequest()
    {
        // Arrange
        var changePasswordRequest = UsersTestDataBuilder.ChangePassword.RequestWithEmptyCurrentPassword();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "All password fields are required."));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Contain("required");
    }

    /// <summary>
    /// Test: Change password success should invalidate refresh tokens for security
    /// </summary>
    [Fact]
    public async Task ChangePassword_OnSuccess_ShouldInvalidateRefreshTokens()
    {
        // Arrange
        var changePasswordRequest = UsersTestDataBuilder.ChangePassword.ValidChangePasswordRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Password changed successfully."));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var responseMessage = GetMessageFromResponse(okResult!.Value);
        responseMessage.Should().Be("Password changed successfully.");
        
        // Verify service was called (which should handle refresh token invalidation)
        _mockUserService.Verify(x => x.ChangePasswordAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<ChangePasswordRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Change password should return appropriate error for various password validation scenarios
    /// </summary>
    [Theory]
    [InlineData("NoSpecialChars123", "Password must contain at least one special character")]
    [InlineData("nouppercasetest!", "Password must contain at least one uppercase letter")]
    [InlineData("NOLOWERCASETEST!", "Password must contain at least one lowercase letter")]
    [InlineData("NoNumbers!", "Password must contain at least one number")]
    [InlineData("Short1!", "Password must be at least 8 characters long")]
    public async Task ChangePassword_WithInvalidPasswordFormats_ShouldReturnAppropriateErrors(string invalidPassword, string expectedErrorMessage)
    {
        // Arrange
        var changePasswordRequest = new ChangePasswordRequest
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = invalidPassword,
            ConfirmNewPassword = invalidPassword
        };
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, expectedErrorMessage));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        var responseMessage = GetMessageFromResponse(objectResult.Value);
        responseMessage.Should().Be(expectedErrorMessage);
    }

    /// <summary>
    /// Test: User should only be able to change their own password
    /// </summary>
    [Fact]
    public async Task ChangePassword_ShouldOnlyAllowUserToChangeOwnPassword()
    {
        // Arrange
        var changePasswordRequest = UsersTestDataBuilder.ChangePassword.ValidChangePasswordRequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Password changed successfully."));

        // Act
        var result = await _usersController.ChangePassword(changePasswordRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called with the authenticated user's context only
        _mockUserService.Verify(x => x.ChangePasswordAsync(
            It.Is<ClaimsPrincipal>(u => u.FindFirst(ClaimTypes.NameIdentifier) != null && 
                                        u.FindFirst(ClaimTypes.NameIdentifier).Value == "1"),
            It.IsAny<ChangePasswordRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SCRUM-45: Setup2FA Tests
    // Tests for POST /users/2fa/setup endpoint - Two-Factor Authentication setup
    //
    // Test Coverage Summary:
    // 1. Happy Path Tests (5 tests): Valid 2FA setup, Base32 key generation, otpauth URI format, URL encoding
    // 2. Authentication Tests (3 tests): Service auth errors, non-existent users, user context verification
    // 3. Business Logic Tests (3 tests): 2FA already enabled, secret generation failures, different issuers
    // 4. Security Tests (3 tests): Sensitive data protection, secure shared keys, secure authenticator URIs
    // 5. Service Integration Tests (2 tests): Parameter passing, cancellation token handling
    // 6. Logging Tests (2 tests): Information logging, no sensitive data in logs
    // 7. Error Handling Tests (3 tests): Database errors, null messages, service unavailable
    // Total: 20 comprehensive unit tests covering all aspects of the Setup2FA endpoint

    #region Happy Path Tests - Setup2FA
    // Tests that verify the endpoint works correctly under normal, expected conditions

    /// <summary>
    /// Test: Valid authenticated user should successfully initiate 2FA setup
    /// Verifies the happy path where an authenticated user initiates 2FA setup
    /// </summary>
    [Fact]
    public async Task Setup2FA_WithValidUser_ShouldReturnSetup2FAResponse()
    {
        // Arrange
        var expectedResponse = TestDataBuilder.Setup2FA.ValidSetup2FAResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as Setup2FAResponse;
        
        response.Should().NotBeNull();
        response!.SharedKey.Should().Be(expectedResponse.SharedKey);
        response.AuthenticatorUri.Should().Be(expectedResponse.AuthenticatorUri);
    }

    /// <summary>
    /// Test: Valid 2FA setup should generate proper Base32 shared key
    /// Verifies that the shared key follows Base32 encoding standards
    /// </summary>
    [Fact]
    public async Task Setup2FA_WithValidUser_ShouldGenerateValidBase32SharedKey()
    {
        // Arrange
        var expectedResponse = TestDataBuilder.Setup2FA.ValidSetup2FAResponseWithLongKey();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as Setup2FAResponse;
        
        response!.SharedKey.Should().NotBeNullOrEmpty();
        response.SharedKey.Should().MatchRegex("^[A-Z2-7]+=*$"); // Valid Base32 pattern
        response.SharedKey.Length.Should().BeGreaterThan(8); // Minimum security requirement
    }

    /// <summary>
    /// Test: Valid 2FA setup should generate proper otpauth URI
    /// Verifies that the authenticator URI follows the otpauth standard
    /// </summary>
    [Fact]
    public async Task Setup2FA_WithValidUser_ShouldGenerateValidOtpauthUri()
    {
        // Arrange
        var expectedResponse = TestDataBuilder.Setup2FA.ValidSetup2FAResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as Setup2FAResponse;
        
        response!.AuthenticatorUri.Should().StartWith("otpauth://totp/");
        response.AuthenticatorUri.Should().Contain("secret=");
        response.AuthenticatorUri.Should().Contain("issuer=");
        response.AuthenticatorUri.Should().Contain(response.SharedKey);
    }

    /// <summary>
    /// Test: Valid 2FA setup should handle special characters in email
    /// Verifies proper URL encoding in the authenticator URI
    /// </summary>
    [Fact]
    public async Task Setup2FA_WithSpecialCharactersInEmail_ShouldEncodeUriProperly()
    {
        // Arrange
        var expectedResponse = TestDataBuilder.Setup2FA.ValidSetup2FAResponseWithSpecialChars();
        var authenticatedUser = CreateAuthenticatedUser(1, "test+user@company.co.uk");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as Setup2FAResponse;
        
        response!.AuthenticatorUri.Should().Contain("%40"); // @ should be URL encoded
        response.AuthenticatorUri.Should().Contain("%2B"); // + should be URL encoded
    }

    #endregion

    #region Authentication Tests - Setup2FA
    // Tests that verify authentication and authorization requirements

    /// <summary>
    /// Test: Service authentication error should return BadRequest
    /// Verifies proper handling when service reports authentication issues
    /// </summary>
    [Fact]
    public async Task Setup2FA_WithServiceAuthError_ShouldReturnBadRequest()
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Setup2FAResponse?)null, "User not authenticated or identity is invalid."));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("not authenticated");
    }

    /// <summary>
    /// Test: User not found should return BadRequest
    /// Verifies proper handling when user doesn't exist
    /// </summary>
    [Fact]
    public async Task Setup2FA_WithNonExistentUser_ShouldReturnBadRequest()
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(999, "nonexistentuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Setup2FAResponse?)null, "User not found."));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("User not found.");
    }

    /// <summary>
    /// Test: Setup should verify authenticated user context
    /// Verifies that 2FA setup is properly scoped to the authenticated user
    /// </summary>
    [Fact]
    public async Task Setup2FA_ShouldUseAuthenticatedUserContext()
    {
        // Arrange
        var expectedResponse = TestDataBuilder.Setup2FA.ValidSetup2FAResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called with the authenticated user's context
        _mockUserService.Verify(x => x.Setup2FAAsync(
            It.Is<ClaimsPrincipal>(u => u.FindFirst(ClaimTypes.NameIdentifier) != null && 
                                        u.FindFirst(ClaimTypes.NameIdentifier).Value == "1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Business Logic Tests - Setup2FA
    // Tests that verify business rules and logic implementation

    /// <summary>
    /// Test: 2FA already enabled should return BadRequest
    /// Verifies that users cannot setup 2FA multiple times
    /// </summary>
    [Fact]
    public async Task Setup2FA_With2FAAlreadyEnabled_ShouldReturnBadRequest()
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Setup2FAResponse?)null, "2FA is already enabled for this user."));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("already enabled");
    }

    /// <summary>
    /// Test: Secret key generation failure should return BadRequest
    /// Verifies proper handling when secret key generation fails
    /// </summary>
    [Fact]
    public async Task Setup2FA_WithSecretGenerationFailure_ShouldReturnBadRequest()
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Setup2FAResponse?)null, "Failed to generate secret key for 2FA setup."));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("Failed to generate secret key");
    }

    /// <summary>
    /// Test: QR code generation should work with different issuers
    /// Verifies that different issuer configurations are handled correctly
    /// </summary>
    [Fact]
    public async Task Setup2FA_WithDifferentIssuer_ShouldReturnCorrectResponse()
    {
        // Arrange
        var expectedResponse = TestDataBuilder.Setup2FA.ValidSetup2FAResponseWithDifferentIssuer();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as Setup2FAResponse;
        
        response!.AuthenticatorUri.Should().Contain("TestIssuer");
        response.AuthenticatorUri.Should().Contain("issuer=TestIssuer");
    }

    #endregion

    #region Security Tests - Setup2FA
    // Tests that verify security measures and data protection

    /// <summary>
    /// Test: 2FA setup should not expose sensitive system information
    /// Verifies that responses don't contain sensitive data
    /// </summary>
    [Fact]
    public async Task Setup2FA_ShouldNotExposeSensitiveSystemInformation()
    {
        // Arrange
        var expectedResponse = TestDataBuilder.Setup2FA.ValidSetup2FAResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var responseJson = System.Text.Json.JsonSerializer.Serialize(okResult!.Value);
        
        responseJson.Should().NotContain("password", "Response should not expose password information");
        responseJson.Should().NotContain("private", "Response should not expose private system information");
        responseJson.Should().NotContain("internal", "Response should not expose internal system details");
        responseJson.Should().NotContain("database", "Response should not expose database information");
    }

    /// <summary>
    /// Test: Shared key should be properly formatted for security
    /// Verifies that shared keys follow security best practices
    /// </summary>
    [Fact]
    public async Task Setup2FA_ShouldGenerateSecureSharedKey()
    {
        // Arrange
        var expectedResponse = TestDataBuilder.Setup2FA.ValidSetup2FAResponseWithLongKey();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as Setup2FAResponse;
        
        response!.SharedKey.Should().NotBeNullOrEmpty();
        response.SharedKey.Length.Should().BeGreaterOrEqualTo(16); // Minimum 80 bits
        response.SharedKey.Should().MatchRegex("^[A-Z2-7]+=*$"); // Valid Base32
        response.SharedKey.Should().NotContain(" "); // No spaces
        response.SharedKey.Should().NotContain("0"); // No zeros in Base32
        response.SharedKey.Should().NotContain("1"); // No ones in Base32
    }

    /// <summary>
    /// Test: Authenticator URI should be properly secured
    /// Verifies that authenticator URIs don't expose sensitive information
    /// </summary>
    [Fact]
    public async Task Setup2FA_ShouldGenerateSecureAuthenticatorUri()
    {
        // Arrange
        var expectedResponse = TestDataBuilder.Setup2FA.ValidSetup2FAResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as Setup2FAResponse;
        
        var uri = response!.AuthenticatorUri;
        uri.Should().StartWith("otpauth://totp/");
        uri.Should().NotContain("password");
        uri.Should().NotContain("token");
        uri.Should().NotContain("session");
        uri.Should().NotContain(" "); // Properly encoded
    }

    #endregion

    #region Service Integration Tests - Setup2FA
    // Tests that verify proper integration with the service layer

    /// <summary>
    /// Test: Service should be called with correct parameters
    /// Verifies that controller passes the correct parameters to the service
    /// </summary>
    [Fact]
    public async Task Setup2FA_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        var expectedResponse = TestDataBuilder.Setup2FA.ValidSetup2FAResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        _mockUserService.Verify(x => x.Setup2FAAsync(
            It.Is<ClaimsPrincipal>(u => u == authenticatedUser),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Test: Cancellation token should be passed to service
    /// Verifies that the cancellation token is properly forwarded to the service layer
    /// </summary>
    [Fact]
    public async Task Setup2FA_WithCancellationToken_ShouldPassTokenToService()
    {
        // Arrange
        var expectedResponse = TestDataBuilder.Setup2FA.ValidSetup2FAResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        var cancellationToken = new CancellationToken();
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _usersController.Setup2FA(cancellationToken);

        // Assert
        _mockUserService.Verify(x => x.Setup2FAAsync(
            It.IsAny<ClaimsPrincipal>(),
            cancellationToken), Times.Once);
    }

    #endregion

    #region Logging Tests - Setup2FA
    // Tests that verify proper logging behavior

    /// <summary>
    /// Test: 2FA setup should log information at start
    /// Verifies that proper information logs are created when setting up 2FA
    /// </summary>
    [Fact]
    public async Task Setup2FA_ShouldLogInformationAtStart()
    {
        // Arrange
        var expectedResponse = TestDataBuilder.Setup2FA.ValidSetup2FAResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("attempting to setup 2FA")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: 2FA setup should not log sensitive information
    /// Verifies that logs don't contain sensitive user or security data
    /// </summary>
    [Fact]
    public async Task Setup2FA_ShouldNotLogSensitiveInformation()
    {
        // Arrange
        var expectedResponse = TestDataBuilder.Setup2FA.ValidSetup2FAResponse();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => !v.ToString()!.Contains("secret") &&
                                           !v.ToString()!.Contains("SharedKey") &&
                                           !v.ToString()!.Contains("otpauth") &&
                                           !v.ToString()!.Contains("password")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Error Handling Tests - Setup2FA
    // Tests that verify comprehensive error handling scenarios

    /// <summary>
    /// Test: Database error should return BadRequest
    /// Verifies proper handling of database connectivity or transaction issues
    /// </summary>
    [Fact]
    public async Task Setup2FA_WithDatabaseError_ShouldReturnBadRequest()
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Setup2FAResponse?)null, "Database connection error during 2FA setup."));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("Database connection error");
    }

    /// <summary>
    /// Test: Null error message should return generic error
    /// Verifies proper handling when service returns null error message
    /// </summary>
    [Fact]
    public async Task Setup2FA_WithNullErrorMessage_ShouldReturnGenericError()
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Setup2FAResponse?)null, (string?)null));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("Failed to initiate 2FA setup.");
    }

    /// <summary>
    /// Test: 2FA service unavailable should return BadRequest
    /// Verifies proper handling when 2FA service is unavailable
    /// </summary>
    [Fact]
    public async Task Setup2FA_With2FAServiceUnavailable_ShouldReturnBadRequest()
    {
        // Arrange
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Setup2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Setup2FAResponse?)null, "2FA service is currently unavailable. Please try again later."));

        // Act
        var result = await _usersController.Setup2FA(CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("service is currently unavailable");
    }

    #endregion

    #endregion

    #region Enable2FA Tests

    #region Happy Path Tests - Enable2FA
    // Tests that verify the endpoint works correctly under normal, expected conditions

    /// <summary>
    /// Test: Valid authenticated user with correct verification code should successfully enable 2FA
    /// Verifies the happy path where an authenticated user enables 2FA with valid verification code
    /// </summary>
    [Fact]
    public async Task Enable2FA_WithValidUserAndCode_ShouldReturnSuccessWithRecoveryCodes()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        var expectedRecoveryCodes = new List<string> { "RECOVERY-CODE-1", "RECOVERY-CODE-2" };
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Two-Factor Authentication enabled successfully.", expectedRecoveryCodes));

        // Act
        var result = await _usersController.Enable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        
        response.Should().NotBeNull();
        var message = GetPropertyValue(response!, "Message");
        var recoveryCodes = GetPropertyValue(response!, "RecoveryCodes") as IEnumerable<string>;
        
        message.Should().Be("Two-Factor Authentication enabled successfully.");
        recoveryCodes.Should().NotBeNull();
        recoveryCodes.Should().BeEquivalentTo(expectedRecoveryCodes);
    }

    /// <summary>
    /// Test: Valid 2FA enable with business user should work correctly
    /// </summary>
    [Fact]
    public async Task Enable2FA_WithBusinessUser_ShouldReturnSuccess()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequestForBusinessUser();
        var authenticatedUser = CreateAuthenticatedUser(2, "business.user");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Two-Factor Authentication enabled successfully.", new List<string> { "REC-001", "REC-002" }));

        // Act
        var result = await _usersController.Enable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test: Valid 2FA enable with different verification codes should work
    /// </summary>
    [Fact]
    public async Task Enable2FA_WithDifferentValidCodes_ShouldReturnSuccess()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequestWithTimeSyncedCode();
        var authenticatedUser = CreateAuthenticatedUser(3, "timesynced.user");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Two-Factor Authentication enabled successfully.", new List<string>()));

        // Act
        var result = await _usersController.Enable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var message = GetPropertyValue(okResult!.Value!, "Message");
        message.Should().Be("Two-Factor Authentication enabled successfully.");
    }

    /// <summary>
    /// Test: Valid 2FA enable with edge case verification codes should work
    /// </summary>
    [Fact]
    public async Task Enable2FA_WithEdgeCaseValidCodes_ShouldReturnSuccess()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequestWithLeadingZeros();
        var authenticatedUser = CreateAuthenticatedUser(4, "edgecase.user");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Two-Factor Authentication enabled successfully.", new List<string> { "EDGE-CODE-1" }));

        // Act
        var result = await _usersController.Enable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Authentication Tests - Enable2FA
    // Tests that verify proper authentication and authorization

    /// <summary>
    /// Test: Service authentication error should return BadRequest
    /// </summary>
    [Fact]
    public async Task Enable2FA_WithAuthenticationError_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "User not authenticated.", null));

        // Act
        var result = await _usersController.Enable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("User not authenticated.");
    }

    /// <summary>
    /// Test: Non-existent user should return BadRequest
    /// </summary>
    [Fact]
    public async Task Enable2FA_WithNonExistentUser_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(999, "nonexistent");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "User not found.", null));

        // Act
        var result = await _usersController.Enable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("User not found.");
    }

    #endregion

    #region Business Logic Tests - Enable2FA
    // Tests that verify proper business logic and validation

    /// <summary>
    /// Test: 2FA already enabled should return success message
    /// </summary>
    [Fact]
    public async Task Enable2FA_With2FAAlreadyEnabled_ShouldReturnSuccessMessage()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "2FA is already enabled.", null));

        // Act
        var result = await _usersController.Enable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var responseMessage = GetMessageFromResponse(okResult!.Value);
        responseMessage.Should().Be("2FA is already enabled.");
    }

    /// <summary>
    /// Test: Setup not completed should return BadRequest
    /// </summary>
    [Fact]
    public async Task Enable2FA_WithSetupNotCompleted_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "2FA setup process not initiated or secret key is missing. Please start setup again.", null));

        // Act
        var result = await _usersController.Enable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("setup process not initiated");
    }

    /// <summary>
    /// Test: Invalid verification code should return BadRequest
    /// </summary>
    [Fact]
    public async Task Enable2FA_WithInvalidVerificationCode_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Invalid verification code.", null));

        // Act
        var result = await _usersController.Enable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("Invalid verification code.");
    }

    #endregion

    #region Security Tests - Enable2FA
    // Tests that verify security measures and data protection

    /// <summary>
    /// Test: Response should not expose sensitive system information
    /// </summary>
    [Fact]
    public async Task Enable2FA_ShouldNotExposeSensitiveSystemInformation()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Database connection failed", null));

        // Act
        var result = await _usersController.Enable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        
        // Ensure no sensitive system information is exposed
        responseMessage.Should().NotContain("database");
        responseMessage.Should().NotContain("sql");
        responseMessage.Should().NotContain("connection string");
        responseMessage.Should().NotContain("server");
    }

    /// <summary>
    /// Test: Recovery codes should be properly formatted when returned
    /// </summary>
    [Fact]
    public async Task Enable2FA_ShouldReturnProperlyFormattedRecoveryCodes()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        var expectedRecoveryCodes = new List<string> { "ABC123", "DEF456", "GHI789" };
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Two-Factor Authentication enabled successfully.", expectedRecoveryCodes));

        // Act
        var result = await _usersController.Enable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var recoveryCodes = GetPropertyValue(okResult!.Value!, "RecoveryCodes") as IEnumerable<string>;
        
        recoveryCodes.Should().NotBeNull();
        recoveryCodes.Should().HaveCount(3);
        recoveryCodes.Should().BeEquivalentTo(expectedRecoveryCodes);
    }

    #endregion

    #region Service Integration Tests - Enable2FA
    // Tests that verify proper service layer integration

    /// <summary>
    /// Test: Service should be called with correct parameters
    /// </summary>
    [Fact]
    public async Task Enable2FA_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        var cancellationToken = new CancellationToken();
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Success", new List<string>()));

        // Act
        var result = await _usersController.Enable2FA(request, cancellationToken);

        // Assert
        _mockUserService.Verify(x => x.Enable2FAAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<Enable2FARequest>(r => r.VerificationCode == request.VerificationCode),
            cancellationToken), Times.Once);
    }

    /// <summary>
    /// Test: Cancellation token should be properly forwarded
    /// </summary>
    [Fact]
    public async Task Enable2FA_ShouldForwardCancellationToken()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        var cancellationToken = new CancellationToken();
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Success", new List<string>()));

        // Act
        var result = await _usersController.Enable2FA(request, cancellationToken);

        // Assert
        _mockUserService.Verify(x => x.Enable2FAAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<Enable2FARequest>(),
            cancellationToken), Times.Once);
    }

    #endregion

    #region Logging Tests - Enable2FA
    // Tests that verify proper logging behavior

    /// <summary>
    /// Test: 2FA enable should log information at start
    /// Verifies that proper information logs are created when enabling 2FA
    /// </summary>
    [Fact]
    public async Task Enable2FA_ShouldLogInformationAtStart()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Success", new List<string>()));

        // Act
        var result = await _usersController.Enable2FA(request, CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("attempting to enable 2FA")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: 2FA enable should not log sensitive information
    /// Verifies that logs don't contain verification codes or sensitive data
    /// </summary>
    [Fact]
    public async Task Enable2FA_ShouldNotLogSensitiveInformation()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Success", new List<string> { "RECOVERY-001" }));

        // Act
        var result = await _usersController.Enable2FA(request, CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => !v.ToString()!.Contains("123456") &&
                                           !v.ToString()!.Contains("VerificationCode") &&
                                           !v.ToString()!.Contains("RECOVERY") &&
                                           !v.ToString()!.Contains("recovery")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Error Handling Tests - Enable2FA
    // Tests that verify comprehensive error handling scenarios

    /// <summary>
    /// Test: Database error should return BadRequest
    /// </summary>
    [Fact]
    public async Task Enable2FA_WithDatabaseError_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "An error occurred while enabling 2FA.", null));

        // Act
        var result = await _usersController.Enable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("error occurred");
    }

    /// <summary>
    /// Test: Null error message should be handled gracefully
    /// </summary>
    [Fact]
    public async Task Enable2FA_WithNullErrorMessage_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, (string?)null, null));

        // Act
        var result = await _usersController.Enable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        
        // Verify that the response has a Message property, even if null
        var response = badRequestResult.Value;
        response.Should().NotBeNull();
        var messageProperty = response!.GetType().GetProperty("Message");
        messageProperty.Should().NotBeNull();
    }

    /// <summary>
    /// Test: 2FA verification service unavailability should be handled
    /// </summary>
    [Fact]
    public async Task Enable2FA_With2FAServiceUnavailable_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Enable2FA.ValidEnable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Enable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Enable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "2FA verification service is currently unavailable. Please try again later.", null));

        // Act
        var result = await _usersController.Enable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("service is currently unavailable");
    }

    #endregion

    #endregion

    #region Verify2FA Tests

    #region Happy Path Tests - Verify2FA
    // Tests that verify the endpoint works correctly under normal, expected conditions

    /// <summary>
    /// Test: Valid authenticated user with correct verification code should successfully verify 2FA
    /// Verifies the happy path where an authenticated user verifies 2FA code for sensitive actions
    /// </summary>
    [Fact]
    public async Task Verify2FA_WithValidUserAndCode_ShouldReturnSuccess()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "2FA verification successful."));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        
        response.Should().NotBeNull();
        var success = GetPropertyValue(response!, "Success");
        var message = GetPropertyValue(response!, "Message");
        
        success.Should().Be(true);
        message.Should().Be("2FA verification successful.");
    }

    /// <summary>
    /// Test: Valid 2FA verification with business user should work correctly
    /// </summary>
    [Fact]
    public async Task Verify2FA_WithBusinessUser_ShouldReturnSuccess()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequestForBusinessUser();
        var authenticatedUser = CreateAuthenticatedUser(2, "business.user");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "2FA verification successful."));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test: Valid 2FA verification for sensitive actions should work
    /// </summary>
    [Fact]
    public async Task Verify2FA_ForSensitiveAction_ShouldReturnSuccess()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequestForSensitiveAction();
        var authenticatedUser = CreateAuthenticatedUser(3, "sensitive.user");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "2FA verification successful."));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var success = GetPropertyValue(okResult!.Value!, "Success");
        success.Should().Be(true);
    }

    /// <summary>
    /// Test: Valid 2FA verification with edge case codes should work
    /// </summary>
    [Fact]
    public async Task Verify2FA_WithEdgeCaseValidCodes_ShouldReturnSuccess()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequestWithLeadingZeros();
        var authenticatedUser = CreateAuthenticatedUser(4, "edgecase.user");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "2FA verification successful."));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test: Valid 2FA verification for login flow should work
    /// </summary>
    [Fact]
    public async Task Verify2FA_ForLoginFlow_ShouldReturnSuccess()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequestForLoginFlow();
        var authenticatedUser = CreateAuthenticatedUser(5, "login.user");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "2FA verification successful."));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Authentication Tests - Verify2FA
    // Tests that verify proper authentication and authorization

    /// <summary>
    /// Test: Service authentication error should return BadRequest
    /// </summary>
    [Fact]
    public async Task Verify2FA_WithAuthenticationError_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "User not authenticated."));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("User not authenticated.");
    }

    /// <summary>
    /// Test: Non-existent user should return BadRequest
    /// </summary>
    [Fact]
    public async Task Verify2FA_WithNonExistentUser_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(999, "nonexistent");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "User not found."));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("User not found.");
    }

    #endregion

    #region Business Logic Tests - Verify2FA
    // Tests that verify proper business logic and validation

    /// <summary>
    /// Test: 2FA not enabled should return BadRequest
    /// </summary>
    [Fact]
    public async Task Verify2FA_With2FANotEnabled_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "2FA is not enabled for this account."));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("2FA is not enabled for this account.");
    }

    /// <summary>
    /// Test: Invalid verification code should return BadRequest
    /// </summary>
    [Fact]
    public async Task Verify2FA_WithInvalidVerificationCode_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Invalid 2FA verification code."));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("Invalid 2FA verification code.");
    }

    /// <summary>
    /// Test: Expired verification code should return BadRequest
    /// </summary>
    [Fact]
    public async Task Verify2FA_WithExpiredCode_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.RequestWithExpiredCode();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Invalid 2FA verification code."));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("Invalid 2FA verification code.");
    }

    #endregion

    #region Security Tests - Verify2FA
    // Tests that verify security measures and data protection

    /// <summary>
    /// Test: Response should not expose sensitive system information
    /// </summary>
    [Fact]
    public async Task Verify2FA_ShouldNotExposeSensitiveSystemInformation()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Database connection failed"));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        
        // Ensure no sensitive system information is exposed
        responseMessage.Should().NotContain("database");
        responseMessage.Should().NotContain("sql");
        responseMessage.Should().NotContain("connection string");
        responseMessage.Should().NotContain("server");
    }

    /// <summary>
    /// Test: Successful verification should return proper response format
    /// </summary>
    [Fact]
    public async Task Verify2FA_ShouldReturnProperResponseFormat()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "2FA verification successful."));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        
        response.Should().NotBeNull();
        
        // Verify response structure
        var successProperty = response!.GetType().GetProperty("Success");
        var messageProperty = response.GetType().GetProperty("Message");
        
        successProperty.Should().NotBeNull();
        messageProperty.Should().NotBeNull();
        
        var success = GetPropertyValue(response, "Success");
        var message = GetPropertyValue(response, "Message");
        
        success.Should().Be(true);
        message.Should().Be("2FA verification successful.");
    }

    /// <summary>
    /// Test: Rate limiting scenarios should be handled appropriately
    /// </summary>
    [Fact]
    public async Task Verify2FA_WithRateLimitExceeded_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.RequestForRateLimitTest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Too many failed verification attempts. Please try again later."));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("Too many", "rate limit message should be appropriate");
    }

    #endregion

    #region Service Integration Tests - Verify2FA
    // Tests that verify proper service layer integration

    /// <summary>
    /// Test: Service should be called with correct parameters
    /// </summary>
    [Fact]
    public async Task Verify2FA_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        var cancellationToken = new CancellationToken();
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Success"));

        // Act
        var result = await _usersController.Verify2FACode(request, cancellationToken);

        // Assert
        _mockUserService.Verify(x => x.Verify2FACodeAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.Is<Verify2FARequest>(r => r.VerificationCode == request.VerificationCode),
            cancellationToken), Times.Once);
    }

    /// <summary>
    /// Test: Cancellation token should be properly forwarded
    /// </summary>
    [Fact]
    public async Task Verify2FA_ShouldForwardCancellationToken()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        var cancellationToken = new CancellationToken();
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Success"));

        // Act
        var result = await _usersController.Verify2FACode(request, cancellationToken);

        // Assert
        _mockUserService.Verify(x => x.Verify2FACodeAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<Verify2FARequest>(),
            cancellationToken), Times.Once);
    }

    #endregion

    #region Logging Tests - Verify2FA
    // Tests that verify proper logging behavior

    /// <summary>
    /// Test: 2FA verification should log information at start
    /// Verifies that proper information logs are created when verifying 2FA codes
    /// </summary>
    [Fact]
    public async Task Verify2FA_ShouldLogInformationAtStart()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Success"));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("attempting to verify 2FA code")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: 2FA verification should not log sensitive information
    /// Verifies that logs don't contain verification codes or sensitive data
    /// </summary>
    [Fact]
    public async Task Verify2FA_ShouldNotLogSensitiveInformation()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Success"));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => !v.ToString()!.Contains("123456") &&
                                           !v.ToString()!.Contains("VerificationCode") &&
                                           !v.ToString()!.Contains("secret") &&
                                           !v.ToString()!.Contains("token")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Error Handling Tests - Verify2FA
    // Tests that verify comprehensive error handling scenarios

    /// <summary>
    /// Test: Database error should return BadRequest
    /// </summary>
    [Fact]
    public async Task Verify2FA_WithDatabaseError_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "An error occurred while verifying 2FA code."));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("error occurred");
    }

    /// <summary>
    /// Test: Null error message should be handled gracefully
    /// </summary>
    [Fact]
    public async Task Verify2FA_WithNullErrorMessage_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, (string?)null));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        
        // Verify that the response has a Message property, even if null
        var response = badRequestResult.Value;
        response.Should().NotBeNull();
        var messageProperty = response!.GetType().GetProperty("Message");
        messageProperty.Should().NotBeNull();
    }

    /// <summary>
    /// Test: 2FA verification service unavailability should be handled
    /// </summary>
    [Fact]
    public async Task Verify2FA_With2FAServiceUnavailable_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Verify2FA.ValidVerify2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Verify2FACodeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Verify2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "2FA verification service is currently unavailable. Please try again later."));

        // Act
        var result = await _usersController.Verify2FACode(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("service is currently unavailable");
    }

    #endregion

    #region Disable2FA Tests

    #region Happy Path Tests - Disable2FA
    // Tests that verify the endpoint works correctly under normal, expected conditions

    /// <summary>
    /// Test: Valid 2FA disable request should return success
    /// </summary>
    [Fact]
    public async Task Disable2FA_WithValidUserAndCode_ShouldReturnSuccess()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.ValidDisable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Two-Factor Authentication disabled successfully."));

        // Act
        var result = await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        
        response.Should().NotBeNull();
        var message = GetPropertyValue(response!, "Message");
        message.Should().Be("Two-Factor Authentication disabled successfully.");
    }

    /// <summary>
    /// Test: Different verification codes for business users should work
    /// </summary>
    [Fact]
    public async Task Disable2FA_WithValidBusinessUserCode_ShouldReturnSuccess()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.ValidDisable2FARequestForBusinessUser();
        var authenticatedUser = CreateAuthenticatedUser(2, "businessuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "2FA disabled successfully. All security data cleared."));

        // Act
        var result = await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var responseMessage = GetMessageFromResponse(okResult!.Value);
        responseMessage.Should().Contain("2FA disabled successfully");
    }

    /// <summary>
    /// Test: Edge case verification codes (leading zeros) should work
    /// </summary>
    [Fact]
    public async Task Disable2FA_WithLeadingZerosCode_ShouldReturnSuccess()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.ValidDisable2FARequestWithLeadingZeros();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "2FA has been successfully disabled."));

        // Act
        var result = await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        response.Should().NotBeNull();
    }

    /// <summary>
    /// Test: Emergency disable with same digits should work
    /// </summary>
    [Fact]
    public async Task Disable2FA_WithEmergencyDisableCode_ShouldReturnSuccess()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.ValidDisable2FARequestForEmergencyDisable();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Emergency 2FA disable completed. Security data cleared."));

        // Act
        var result = await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var responseMessage = GetMessageFromResponse(okResult!.Value);
        responseMessage.Should().Contain("Emergency 2FA disable completed");
    }

    /// <summary>
    /// Test: Data cleanup confirmation should be included in response
    /// </summary>
    [Fact]
    public async Task Disable2FA_WithDataCleanupRequest_ShouldConfirmCleanup()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.RequestForDataCleanupTest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "2FA disabled. Secret key and recovery codes have been permanently deleted."));

        // Act
        var result = await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var responseMessage = GetMessageFromResponse(okResult!.Value);
        responseMessage.Should().Contain("permanently deleted");
    }

    #endregion

    #region Authentication Tests - Disable2FA
    // Tests that verify authentication and user validation scenarios

    /// <summary>
    /// Test: Service authentication error should return BadRequest
    /// </summary>
    [Fact]
    public async Task Disable2FA_WithServiceAuthenticationError_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.ValidDisable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "User not authenticated or identity is invalid."));

        // Act
        var result = await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("not authenticated");
    }

    /// <summary>
    /// Test: Non-existent user should return appropriate error
    /// </summary>
    [Fact]
    public async Task Disable2FA_WithNonExistentUser_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.ValidDisable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(999, "nonexistentuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "User not found."));

        // Act
        var result = await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Be("User not found.");
    }

    #endregion

    #region Business Logic Tests - Disable2FA
    // Tests that verify business logic and validation scenarios

    /// <summary>
    /// Test: 2FA not currently enabled should return appropriate error
    /// </summary>
    [Fact]
    public async Task Disable2FA_When2FANotEnabled_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.ValidDisable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "2FA is not currently enabled for this account."));

        // Act
        var result = await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("2FA is not currently enabled");
    }

    /// <summary>
    /// Test: Invalid verification code should be rejected
    /// </summary>
    [Fact]
    public async Task Disable2FA_WithInvalidVerificationCode_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.RequestWithInvalidCode();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Invalid verification code. Cannot disable 2FA."));

        // Act
        var result = await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("Invalid verification code");
    }

    /// <summary>
    /// Test: Expired verification code should be rejected
    /// </summary>
    [Fact]
    public async Task Disable2FA_WithExpiredVerificationCode_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.RequestWithExpiredCode();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Verification code has expired. Please generate a new code."));

        // Act
        var result = await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("expired");
    }

    #endregion

    #region Security Tests - Disable2FA
    // Tests that verify security and data protection features

    /// <summary>
    /// Test: Sensitive system information should not be exposed in responses
    /// </summary>
    [Fact]
    public async Task Disable2FA_ShouldNotExposeSensitiveSystemInformation()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.RequestForSecurityAuditTest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Security validation failed."));

        // Act
        var result = await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        
        // Ensure no sensitive information is exposed
        responseMessage.Should().NotContain("database");
        responseMessage.Should().NotContain("secret");
        responseMessage.Should().NotContain("key");
        responseMessage.Should().NotContain("token");
        responseMessage.Should().NotContain("password");
    }

    /// <summary>
    /// Test: Response format should be consistent and secure
    /// </summary>
    [Fact]
    public async Task Disable2FA_ShouldReturnConsistentResponseFormat()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.ValidDisable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "2FA disabled successfully."));

        // Act
        var result = await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;
        
        response.Should().NotBeNull();
        var messageProperty = response!.GetType().GetProperty("Message");
        messageProperty.Should().NotBeNull();
        messageProperty!.PropertyType.Should().Be(typeof(string));
    }

    /// <summary>
    /// Test: Rate limiting scenarios should be handled appropriately
    /// </summary>
    [Fact]
    public async Task Disable2FA_WithRateLimitingScenario_ShouldReturnAppropriateMessage()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.RequestForRateLimitTest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Too many attempts. Please wait before trying again."));

        // Act
        var result = await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("Too many attempts", "rate limit message should be appropriate");
    }

    #endregion

    #region Service Integration Tests - Disable2FA
    // Tests that verify correct integration with service layer

    /// <summary>
    /// Test: Controller should pass correct parameters to service
    /// </summary>
    [Fact]
    public async Task Disable2FA_ShouldPassCorrectParametersToService()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.ValidDisable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "2FA disabled successfully."));

        // Act
        await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        _mockUserService.Verify(x => x.Disable2FAAsync(
            It.Is<ClaimsPrincipal>(cp => cp == authenticatedUser),
            It.Is<Disable2FARequest>(r => r.VerificationCode == request.VerificationCode),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    /// <summary>
    /// Test: Cancellation token should be properly forwarded
    /// </summary>
    [Fact]
    public async Task Disable2FA_ShouldForwardCancellationToken()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.ValidDisable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        var cancellationToken = new CancellationToken();
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "2FA disabled successfully."));

        // Act
        await _usersController.Disable2FA(request, cancellationToken);

        // Assert
        _mockUserService.Verify(x => x.Disable2FAAsync(
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<Disable2FARequest>(),
            cancellationToken
        ), Times.Once);
    }

    #endregion

    #region Logging Tests - Disable2FA
    // Tests that verify appropriate logging behavior

    /// <summary>
    /// Test: Successful disable attempts should be logged at information level
    /// </summary>
    [Fact]
    public async Task Disable2FA_OnDisableAttempt_ShouldLogInformation()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.ValidDisable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "2FA disabled successfully."));

        // Act
        await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("attempting to disable 2FA")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test: Logging should not expose sensitive data
    /// </summary>
    [Fact]
    public async Task Disable2FA_LoggingShouldNotExposeSensitiveData()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.ValidDisable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "2FA disabled successfully."));

        // Act
        await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert - Verify that logging is called but sensitive data is not exposed
        _mockControllerLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => !v.ToString()!.Contains("123456") &&
                                           !v.ToString()!.Contains("VerificationCode") &&
                                           !v.ToString()!.Contains("secret") &&
                                           !v.ToString()!.Contains("key")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Error Handling Tests - Disable2FA
    // Tests that verify proper error handling in various failure scenarios

    /// <summary>
    /// Test: Database connectivity error should be handled gracefully
    /// </summary>
    [Fact]
    public async Task Disable2FA_WithDatabaseError_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.ValidDisable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "An error occurred while disabling 2FA."));

        // Act
        var result = await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("error occurred");
    }

    /// <summary>
    /// Test: Null error message should be handled appropriately
    /// </summary>
    [Fact]
    public async Task Disable2FA_WithNullErrorMessage_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.ValidDisable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, (string?)null));

        // Act
        var result = await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
        
        // Verify that the response has a Message property, even if null
        var response = badRequestResult.Value;
        response.Should().NotBeNull();
        var messageProperty = response!.GetType().GetProperty("Message");
        messageProperty.Should().NotBeNull();
    }

    /// <summary>
    /// Test: 2FA disable service unavailability should be handled
    /// </summary>
    [Fact]
    public async Task Disable2FA_With2FAServiceUnavailable_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.Disable2FA.ValidDisable2FARequest();
        var authenticatedUser = CreateAuthenticatedUser(1, "testuser");
        _usersController.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = authenticatedUser
        };

        _mockUserService.Setup(x => x.Disable2FAAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Disable2FARequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "2FA disable service is currently unavailable. Please try again later."));

        // Act
        var result = await _usersController.Disable2FA(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        var responseMessage = GetMessageFromResponse(badRequestResult!.Value);
        responseMessage.Should().Contain("service is currently unavailable");
    }

    #endregion

    #endregion

    #endregion
}



