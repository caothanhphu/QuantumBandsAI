// SCRUM-40: Unit Tests for GET /users/me - Get User Profile Endpoint
// SCRUM-41: Unit Tests for PUT /users/me - Update User Profile Endpoint
// This test class provides comprehensive test coverage for the UsersController endpoints:
// - GetMyProfile (GET /users/me): Profile retrieval with authentication and data mapping tests
// - UpdateMyProfile (PUT /users/me): Profile updates with validation, authentication, and security tests

using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Features.Authentication;
using QuantumBands.Application.Features.Users.Commands.UpdateProfile;
using QuantumBands.Application.Interfaces;
using QuantumBands.Tests.Common;
using QuantumBands.Tests.Fixtures;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace QuantumBands.Tests.Controllers;

/// <summary>
/// Test class for UsersController.GetMyProfile endpoint (GET /users/me)
/// Covers authentication, authorization, data mapping, error handling, and security scenarios
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
        var expectedProfile = TestDataBuilder.UserProfile.ValidUserProfile();
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
        var expectedProfile = TestDataBuilder.UserProfile.AdminUserProfile();
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
        var expectedProfile = TestDataBuilder.UserProfile.UserProfileWithoutFullName();
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
        var expectedProfile = TestDataBuilder.UserProfile.UnverifiedUserProfile();
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
        var expectedProfile = TestDataBuilder.UserProfile.UserWith2FAEnabled();
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
        var expectedProfile = TestDataBuilder.UserProfile.ValidUserProfile();
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
        var expectedProfile = TestDataBuilder.UserProfile.ValidUserProfile();
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
        var expectedProfile = TestDataBuilder.UserProfile.ValidUserProfile();
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
        var expectedProfile = TestDataBuilder.UserProfile.ValidUserProfile();
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
        var expectedProfile = TestDataBuilder.UserProfile.ValidUserProfile();
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
        var expectedProfile = TestDataBuilder.UserProfile.ValidUserProfile();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var expectedProfile = TestDataBuilder.UserProfile.ValidUserProfile();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.UpdateWithNullFullName();
        var expectedProfile = TestDataBuilder.UserProfile.ValidUserProfile();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.UpdateWithSameFullName();
        var expectedProfile = TestDataBuilder.UserProfile.ValidUserProfile();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.UpdateWithSpecialCharacters();
        var expectedProfile = TestDataBuilder.UserProfile.ValidUserProfile();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.UpdateWithMaxValidLength();
        var expectedProfile = TestDataBuilder.UserProfile.ValidUserProfile();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var expectedProfile = TestDataBuilder.UserProfile.ValidUserProfile();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var expectedProfile = TestDataBuilder.UserProfile.ValidUserProfile();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var expectedProfile = TestDataBuilder.UserProfile.ValidUserProfile();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var expectedProfile = TestDataBuilder.UserProfile.ValidUserProfile();
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
        var updateRequest = TestDataBuilder.UpdateUserProfile.ValidUpdateRequest();
        var expectedProfile = TestDataBuilder.UserProfile.ValidUserProfile();
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
}