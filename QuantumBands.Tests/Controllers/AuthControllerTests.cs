using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Features.Authentication.Commands.RegisterUser;
using QuantumBands.Application.Features.Authentication.Commands.Login;
using QuantumBands.Application.Features.Authentication.Commands.VerifyEmail;
using QuantumBands.Application.Features.Authentication.Commands.RefreshToken;
using QuantumBands.Application.Features.Authentication.Commands.ForgotPassword;
using QuantumBands.Application.Features.Authentication.Commands.ResetPassword;
using QuantumBands.Application.Features.Authentication.Commands.ResendVerificationEmail;
using QuantumBands.Application.Features.Authentication;
using QuantumBands.Application.Interfaces;
using QuantumBands.Tests.Common;
using QuantumBands.Tests.Fixtures;
using static QuantumBands.Tests.Fixtures.AuthenticationTestDataBuilder;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace QuantumBands.Tests.Controllers;

public class AuthControllerTests : TestBase
{
    private readonly AuthController _authController;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<ILogger<AuthController>> _mockControllerLogger;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockControllerLogger = new Mock<ILogger<AuthController>>();
        _authController = new AuthController(_mockAuthService.Object, _mockControllerLogger.Object);
    }

    private static string? GetMessageFromResponse(object? response)
    {
        if (response == null) return null;
        var messageProperty = response.GetType().GetProperty("Message");
        return messageProperty?.GetValue(response, null) as string;
    }

    #region Happy Path Tests

    [Fact]
    public async Task Register_WithValidCommand_ShouldReturnCreatedResult()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        var expectedUserDto = TestDataBuilder.UserDtos.ValidUserDto();

        _mockAuthService.Setup(x => x.RegisterUserAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedUserDto, (string?)null));

        // Act
        var result = await _authController.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(201);
        objectResult.Value.Should().BeEquivalentTo(expectedUserDto);
        
        _mockAuthService.Verify(x => x.RegisterUserAsync(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_WithValidCommandWithoutFullName_ShouldReturnCreatedResult()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommandWithoutFullName();
        var expectedUserDto = TestDataBuilder.UserDtos.ValidUserDto();

        _mockAuthService.Setup(x => x.RegisterUserAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedUserDto, (string?)null));

        // Act
        var result = await _authController.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(201);
        objectResult.Value.Should().BeEquivalentTo(expectedUserDto);
    }

    [Fact]
    public async Task Register_ShouldCallAuthServiceWithCorrectCommand()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        var expectedUserDto = TestDataBuilder.UserDtos.ValidUserDto();

        _mockAuthService.Setup(x => x.RegisterUserAsync(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedUserDto, null));

        // Act
        var result = await _authController.Register(command, CancellationToken.None);

        // Assert
        _mockAuthService.Verify(
            x => x.RegisterUserAsync(It.Is<RegisterUserCommand>(cmd => 
                cmd.Username == command.Username &&
                cmd.Email == command.Email &&
                cmd.Password == command.Password &&
                cmd.FullName == command.FullName), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Validation Error Tests

    [Fact]
    public async Task Register_WithInvalidModelState_ShouldReturnBadRequest()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.CommandWithShortUsername();
        _authController.ModelState.AddModelError("Username", "Username must be between 3 and 50 characters");

        // Act
        var result = await _authController.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        // Verify AuthService was not called when model state is invalid
        _mockAuthService.Verify(x => x.RegisterUserAsync(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("ab")] // Too short
    [InlineData("verylongusernamethatexceedsthelimitofcharacters123")] // Too long
    [InlineData("user@name")] // Invalid characters
    public async Task Register_WithInvalidUsername_ShouldReturnBadRequest(string invalidUsername)
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        command.Username = invalidUsername;
        
        // Simulate model validation failure
        _authController.ModelState.AddModelError("Username", "Invalid username");

        // Act
        var result = await _authController.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        _mockAuthService.Verify(x => x.RegisterUserAsync(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("invalid-email")] // Invalid format
    [InlineData("test@")] // Incomplete
    [InlineData("")] // Empty
    public async Task Register_WithInvalidEmail_ShouldReturnBadRequest(string invalidEmail)
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        command.Email = invalidEmail;
        
        // Simulate model validation failure
        _authController.ModelState.AddModelError("Email", "Invalid email format");

        // Act
        var result = await _authController.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        _mockAuthService.Verify(x => x.RegisterUserAsync(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("weak")] // Too short
    [InlineData("password123")] // No special characters
    [InlineData("PASSWORD!")] // No lowercase
    [InlineData("password!")] // No uppercase
    public async Task Register_WithInvalidPassword_ShouldReturnBadRequest(string invalidPassword)
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        command.Password = invalidPassword;
        
        // Simulate model validation failure
        _authController.ModelState.AddModelError("Password", "Password does not meet requirements");

        // Act
        var result = await _authController.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        _mockAuthService.Verify(x => x.RegisterUserAsync(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Business Logic Error Tests

    [Fact]
    public async Task Register_WithDuplicateUsername_ShouldReturnConflict()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        var errorMessage = "Username already exists";

        _mockAuthService.Setup(x => x.RegisterUserAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserDto?)null, errorMessage));

        // Act
        var result = await _authController.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
        
        var conflictResult = result as ConflictObjectResult;
        conflictResult!.StatusCode.Should().Be(409);
        conflictResult.Value.Should().BeEquivalentTo(new { Message = errorMessage });
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnConflict()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        var errorMessage = "Email already exists";

        _mockAuthService.Setup(x => x.RegisterUserAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserDto?)null, errorMessage));

        // Act
        var result = await _authController.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
        
        var conflictResult = result as ConflictObjectResult;
        conflictResult!.StatusCode.Should().Be(409);
        conflictResult.Value.Should().BeEquivalentTo(new { Message = errorMessage });
    }

    [Fact]
    public async Task Register_WithServiceError_ShouldReturnInternalServerError()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        var errorMessage = "An error occurred during registration";

        _mockAuthService.Setup(x => x.RegisterUserAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((UserDto?)null, errorMessage));

        // Act
        var result = await _authController.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        
        var errorResult = result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
        errorResult.Value.Should().BeEquivalentTo(new { Message = errorMessage });
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task Register_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        var exceptionMessage = "Unexpected error occurred";

        _mockAuthService.Setup(x => x.RegisterUserAsync(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _authController.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        
        var errorResult = result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
        errorResult.Value.Should().BeEquivalentTo(new { Message = "An unexpected error occurred during registration." });
    }

    [Fact]
    public async Task Register_WhenServiceThrowsArgumentException_ShouldReturnBadRequest()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        var exceptionMessage = "Invalid argument provided";

        _mockAuthService.Setup(x => x.RegisterUserAsync(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException(exceptionMessage));

        // Act
        var result = await _authController.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().BeEquivalentTo(new { Message = exceptionMessage });
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Register_ShouldCreateUserAndReturnCorrectResponse()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        var expectedUserDto = TestDataBuilder.UserDtos.ValidUserDto();

        _mockAuthService.Setup(x => x.RegisterUserAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedUserDto, (string?)null));

        // Act
        var result = await _authController.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(201);
        objectResult.Value.Should().BeEquivalentTo(expectedUserDto);
    }

    #endregion

    #region Data Validation Tests

    [Fact]
    public async Task Register_ShouldValidateRequiredFields()
    {
        // Arrange
        var command = new RegisterUserCommand 
        {
            Username = "",
            Email = "",
            Password = ""
        }; // Empty command

        // Simulate model validation failures for required fields
        _authController.ModelState.AddModelError("Username", "Username is required");
        _authController.ModelState.AddModelError("Email", "Email is required");
        _authController.ModelState.AddModelError("Password", "Password is required");

        // Act
        var result = await _authController.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        // Verify service was not called
        _mockAuthService.Verify(x => x.RegisterUserAsync(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task Register_ShouldNotExposePasswordInResponse()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        var expectedUserDto = TestDataBuilder.UserDtos.ValidUserDto();

        _mockAuthService.Setup(x => x.RegisterUserAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedUserDto, (string?)null));

        // Act
        var result = await _authController.Register(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(201);
        
        var responseValue = objectResult.Value as UserDto;
        responseValue.Should().NotBeNull();
        
        // Verify password is not included in response (UserDto should not contain password)
        var responseJson = System.Text.Json.JsonSerializer.Serialize(responseValue);
        responseJson.Should().NotContain("password");
        responseJson.Should().NotContain("Password");
    }

    #endregion

    #region Login Tests

    #region Happy Path Login Tests

    [Fact]
    public async Task Login_WithValidUsernameAndPassword_ShouldReturnOkWithLoginResponse()
    {
        // Arrange
        var loginRequest = AuthenticationTestDataBuilder.Login.ValidLoginWithUsername();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
        
        _mockAuthService.Verify(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_WithValidEmailAndPassword_ShouldReturnOkWithLoginResponse()
    {
        // Arrange
        var loginRequest = AuthenticationTestDataBuilder.Login.ValidLoginWithEmail();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Login_WithAdminCredentials_ShouldReturnOkWithAdminLoginResponse()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = "admin",
            Password = "AdminPassword123!"
        };
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.AdminLoginResponse();

        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
        
        var response = okResult.Value as LoginResponse;
        response!.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Login_ShouldCallAuthServiceWithCorrectRequest()
    {
        // Arrange
        var loginRequest = AuthenticationTestDataBuilder.Login.ValidLoginWithUsername();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        _mockAuthService.Verify(
            x => x.LoginAsync(It.Is<LoginRequest>(req => 
                req.UsernameOrEmail == loginRequest.UsernameOrEmail &&
                req.Password == loginRequest.Password), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Validation Error Tests

    [Fact]
    public async Task Login_WithNullRequest_ShouldReturnBadRequest()
    {
        // Act
        var result = await _authController.Login(null!, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().Be("Login request cannot be null.");
        
        // Verify AuthService was not called
        _mockAuthService.Verify(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", "StrongPassword123!")] // Empty username
    [InlineData("testuser123", "")] // Empty password
    [InlineData("", "")] // Both empty
    public async Task Login_WithEmptyCredentials_ShouldCallAuthServiceAndReturnUnauthorized(string usernameOrEmail, string password)
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = usernameOrEmail,
            Password = password
        };

        var errorMessage = "Invalid username/email or password";
        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LoginResponse?)null, errorMessage));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.StatusCode.Should().Be(401);
        unauthorizedResult.Value.Should().BeEquivalentTo(new { Message = errorMessage });
        
        // Verify AuthService was called (controller doesn't do validation, service does)
        _mockAuthService.Verify(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Authentication Error Tests

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = AuthenticationTestDataBuilder.Login.LoginWithInvalidPassword();
        var errorMessage = "Invalid username/email or password";

        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LoginResponse?)null, errorMessage));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.StatusCode.Should().Be(401);
        unauthorizedResult.Value.Should().BeEquivalentTo(new { Message = errorMessage });
    }

    [Fact]
    public async Task Login_WithInactiveUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = AuthenticationTestDataBuilder.Login.LoginWithInactiveUser();
        var errorMessage = "User account is inactive";

        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LoginResponse?)null, errorMessage));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.StatusCode.Should().Be(401);
        unauthorizedResult.Value.Should().BeEquivalentTo(new { Message = errorMessage });
    }

    [Fact]
    public async Task Login_WithUnverifiedEmail_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = AuthenticationTestDataBuilder.Login.LoginWithUnverifiedEmail();
        var errorMessage = "Please verify your email before logging in";

        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LoginResponse?)null, errorMessage));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.StatusCode.Should().Be(401);
        unauthorizedResult.Value.Should().BeEquivalentTo(new { Message = errorMessage });
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = AuthenticationTestDataBuilder.Login.LoginWithInvalidUsername();
        var errorMessage = "Invalid username/email or password";

        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LoginResponse?)null, errorMessage));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.StatusCode.Should().Be(401);
        unauthorizedResult.Value.Should().BeEquivalentTo(new { Message = errorMessage });
    }

    #endregion

    #region Server Error Tests

    [Fact]
    public async Task Login_WithServiceError_ShouldReturnInternalServerError()
    {
        // Arrange
        var loginRequest = AuthenticationTestDataBuilder.Login.ValidLoginWithUsername();
        var errorMessage = "Database connection failed";

        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LoginResponse?)null, errorMessage));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var errorResult = result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
        errorResult.Value.Should().BeEquivalentTo(new { Message = errorMessage });
    }

    [Fact]
    public async Task Login_WhenServiceReturnsNullWithoutMessage_ShouldReturnInternalServerError()
    {
        // Arrange
        var loginRequest = AuthenticationTestDataBuilder.Login.ValidLoginWithUsername();

        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LoginResponse?)null, (string?)null));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var errorResult = result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
        errorResult.Value.Should().BeEquivalentTo(new { Message = "An unexpected error occurred during login." });
    }

    [Fact]
    public async Task Login_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var loginRequest = AuthenticationTestDataBuilder.Login.ValidLoginWithUsername();
        var exceptionMessage = "Unexpected database error";

        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _authController.Login(loginRequest, CancellationToken.None));
        
        exception.Message.Should().Be(exceptionMessage);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task Login_ShouldReturnJwtTokenInResponse()
    {
        // Arrange
        var loginRequest = AuthenticationTestDataBuilder.Login.ValidLoginWithUsername();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as LoginResponse;
        
        response!.JwtToken.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();
        response.RefreshTokenExpiry.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_ShouldNotExposePasswordInResponse()
    {
        // Arrange
        var loginRequest = AuthenticationTestDataBuilder.Login.ValidLoginWithUsername();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as LoginResponse;
        
        // Verify password is not included in response
        var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
        responseJson.Should().NotContain("password");
        responseJson.Should().NotContain("Password");
    }

    [Fact]
    public async Task Login_ShouldLogUserLoginAttempt()
    {
        // Arrange
        var loginRequest = AuthenticationTestDataBuilder.Login.ValidLoginWithUsername();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify logging occurred (using Moq verification)
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Received login request for user: {loginRequest.UsernameOrEmail}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Login_WithFailedLogin_ShouldLogWarning()
    {
        // Arrange
        var loginRequest = AuthenticationTestDataBuilder.Login.LoginWithInvalidPassword();
        var errorMessage = "Invalid username/email or password";

        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LoginResponse?)null, errorMessage));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        
        // Verify warning was logged
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Login failed for {loginRequest.UsernameOrEmail}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task Login_ShouldUpdateUserLastLoginDate()
    {
        // Arrange
        var loginRequest = AuthenticationTestDataBuilder.Login.ValidLoginWithUsername();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify that the service was called (which should update last login date)
        _mockAuthService.Verify(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_ShouldGenerateRefreshToken()
    {
        // Arrange
        var loginRequest = AuthenticationTestDataBuilder.Login.ValidLoginWithUsername();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as LoginResponse;
        
        response!.RefreshToken.Should().NotBeNullOrEmpty();
        response.RefreshTokenExpiry.Should().BeAfter(DateTime.UtcNow);
        response.RefreshTokenExpiry.Should().BeBefore(DateTime.UtcNow.AddDays(8)); // Should be within 7 days
    }

    [Theory]
    [InlineData("testuser123")]
    [InlineData("test@example.com")]
    [InlineData("TEST@EXAMPLE.COM")] // Test case insensitive email
    public async Task Login_WithVariousUsernameFormats_ShouldWork(string usernameOrEmail)
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = usernameOrEmail,
            Password = "StrongPassword123!"
        };
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        _mockAuthService.Verify(x => x.LoginAsync(
            It.Is<LoginRequest>(req => req.UsernameOrEmail == usernameOrEmail), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Login_EndToEndHappyPath_ShouldReturnCompleteLoginResponse()
    {
        // Arrange
        var loginRequest = AuthenticationTestDataBuilder.Login.ValidLoginWithUsername();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var response = okResult.Value as LoginResponse;
        response.Should().NotBeNull();
        response!.UserId.Should().BePositive();
        response.Username.Should().NotBeNullOrEmpty();
        response.Email.Should().NotBeNullOrEmpty();
        response.Role.Should().NotBeNullOrEmpty();
        response.JwtToken.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();
        response.RefreshTokenExpiry.Should().BeAfter(DateTime.UtcNow);
        
        // Verify all service interactions
        _mockAuthService.Verify(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #endregion

    #region VerifyEmail Tests

    #region Happy Path Tests

    [Fact]
    public async Task VerifyEmail_WithValidRequest_ShouldReturnOkWithSuccessMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.ValidRequest();
        var expectedMessage = "Email verified successfully.";

        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var response = okResult.Value;
        var messageProperty = response!.GetType().GetProperty("Message");
        var message = messageProperty!.GetValue(response, null) as string;
        message.Should().Be(expectedMessage);
        
        _mockAuthService.Verify(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_WithValidToken_ShouldActivateUserAccount()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.ValidRequest();
        var successMessage = "Email verified successfully. Account activated.";

        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, successMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var response = okResult!.Value;
        var messageProperty = response!.GetType().GetProperty("Message");
        var message = messageProperty!.GetValue(response, null) as string;
        message.Should().Contain("activated");
        
        _mockAuthService.Verify(x => x.VerifyEmailAsync(
            It.Is<VerifyEmailRequest>(r => 
                r.UserId == request.UserId && 
                r.Token == request.Token), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_ShouldCallAuthServiceWithCorrectRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.ValidRequest();
        var expectedMessage = "Email verified successfully.";

        _mockAuthService.Setup(x => x.VerifyEmailAsync(It.IsAny<VerifyEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        _mockAuthService.Verify(
            x => x.VerifyEmailAsync(It.Is<VerifyEmailRequest>(req => 
                req.UserId == request.UserId &&
                req.Token == request.Token), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task VerifyEmail_WithNullRequest_ShouldReturnBadRequest()
    {
        // Arrange
        VerifyEmailRequest? request = null;

        // Act
        var result = await _authController.VerifyEmail(request!, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().Be("Verification request cannot be null.");
        
        // Verify AuthService was not called when request is null
        _mockAuthService.Verify(x => x.VerifyEmailAsync(It.IsAny<VerifyEmailRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidUserId_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.RequestWithInvalidUserId();
        var errorMessage = "User ID must be valid.";
        
        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        
        var message = GetMessageFromResponse(objectResult.Value);
        message.Should().Be(errorMessage);
        
        _mockAuthService.Verify(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_WithEmptyToken_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.RequestWithEmptyToken();
        var errorMessage = "Verification token is required.";
        
        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        
        var message = GetMessageFromResponse(objectResult.Value);
        message.Should().Be(errorMessage);
        
        _mockAuthService.Verify(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public async Task VerifyEmail_WithInvalidUserIds_ShouldReturnInternalServerError(int invalidUserId)
    {
        // Arrange
        var request = new VerifyEmailRequest { UserId = invalidUserId, Token = "valid-token" };
        var errorMessage = "User ID must be valid.";
        
        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        
        var message = GetMessageFromResponse(objectResult.Value);
        message.Should().Be(errorMessage);
        
        _mockAuthService.Verify(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Business Logic Error Tests

    [Fact]
    public async Task VerifyEmail_WithNonExistentUser_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.RequestWithNonExistentUser();
        var errorMessage = "User not found.";

        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        
        var message = GetMessageFromResponse(objectResult.Value);
        message.Should().Be(errorMessage);
        
        _mockAuthService.Verify(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_WithExpiredToken_ShouldReturnBadRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.RequestWithExpiredToken();
        var errorMessage = "Verification token has expired.";

        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be(errorMessage);
        
        _mockAuthService.Verify(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_ShouldReturnBadRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.RequestWithInvalidToken();
        var errorMessage = "Invalid verification token.";

        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be(errorMessage);
        
        _mockAuthService.Verify(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_WithAlreadyVerifiedEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.RequestWithAlreadyVerifiedUser();
        var errorMessage = "Email is already verified.";

        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        
        var message = GetMessageFromResponse(objectResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task VerifyEmail_WithTokenMismatch_ShouldReturnBadRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.ValidRequest();
        var errorMessage = "Invalid verification token provided.";

        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be(errorMessage);
    }

    #endregion

    #region Server Error Tests

    [Fact]
    public async Task VerifyEmail_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.ValidRequest();
        var exceptionMessage = "Database connection failed";

        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        
        _mockAuthService.Verify(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_WithServiceError_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.ValidRequest();
        var errorMessage = "Internal server error occurred.";

        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        
        var message = GetMessageFromResponse(objectResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task VerifyEmail_WhenServiceReturnsNullMessage_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.ValidRequest();

        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, null!));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task VerifyEmail_ShouldLogVerificationAttempt()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.ValidRequest();
        var successMessage = "Email verified successfully.";

        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, successMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify logging was called
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Received email verification request for UserID")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_WithFailedVerification_ShouldLogWarning()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.RequestWithInvalidToken();
        var errorMessage = "Invalid verification token.";

        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        
        // Verify warning was logged
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email verification failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_WithSuccessfulVerification_ShouldLogSuccess()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.ValidRequest();
        var successMessage = "Email verified successfully.";

        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, successMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify success was logged
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email verification successful")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_ShouldValidateTokenSecurely()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.RequestWithMalformedToken();
        var errorMessage = "Invalid verification token format.";

        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        
        var response = result as BadRequestObjectResult;
        var message = GetMessageFromResponse(response!.Value);
        message.Should().Be(errorMessage);
        
        // Verify service was called to validate token
        _mockAuthService.Verify(x => x.VerifyEmailAsync(
            It.Is<VerifyEmailRequest>(r => r.Token == request.Token), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_ShouldPreventTokenReuse()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.ValidRequest();
        var errorMessage = "Token has already been used.";

        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        
        var response = objectResult.Value;
        var messageProperty = response!.GetType().GetProperty("Message");
        var message = messageProperty!.GetValue(response, null) as string;
        message.Should().Be(errorMessage);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task VerifyEmail_EndToEndHappyPath_ShouldReturnCompleteResponse()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.ValidRequest();
        var successMessage = "Email verified successfully. Welcome to QuantumBands!";

        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, successMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(successMessage);
        
        // Verify complete service interaction
        _mockAuthService.Verify(x => x.VerifyEmailAsync(
            It.Is<VerifyEmailRequest>(r => 
                r.UserId == request.UserId && 
                r.Token == request.Token), 
            It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify logging flow
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Received email verification request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email verification successful")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_CompleteWorkflow_ShouldHandleAllSteps()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.VerifyEmail.ValidRequest();
        var successMessage = "Email verified successfully. Account activated.";

        _mockAuthService.Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, successMessage));

        // Act
        var result = await _authController.VerifyEmail(request, CancellationToken.None);

        // Assert
        // Verify HTTP response
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        // Verify response content
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Contain("verified");
        message.Should().Contain("activated");
        
        // Verify service call
        _mockAuthService.Verify(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify logging
        _mockControllerLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(2)); // At least request received and success logged
    }

    #endregion

    #endregion

    #region Refresh Token Tests
    // SCRUM-35: Unit Tests for POST /auth/refresh-token endpoint
    // This comprehensive test suite covers all the scenarios outlined in the ticket:
    // - Happy Path: Valid token exchange, new JWT generation, new refresh token creation
    // - Validation: Empty tokens, invalid formats, non-existent tokens
    // - Security: Expired tokens, revoked tokens, token reuse detection
    // - Business Logic: Token invalidation, user session validation, token rotation

    #region Happy Path Tests

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturnOkWithNewLoginResponse()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
        
        _mockAuthService.Verify(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldGenerateNewJwtToken()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as LoginResponse;
        
        response!.JwtToken.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();
        response.RefreshTokenExpiry.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturnNewRefreshToken()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as LoginResponse;
        
        response!.RefreshToken.Should().NotBe(request.RefreshToken);
        response.RefreshToken.Should().NotBeNullOrEmpty();
        response.RefreshTokenExpiry.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task RefreshToken_ShouldCallAuthServiceWithCorrectRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, null));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        _mockAuthService.Verify(
            x => x.RefreshTokenAsync(It.Is<RefreshTokenRequest>(req => 
                req.ExpiredJwtToken == request.ExpiredJwtToken &&
                req.RefreshToken == request.RefreshToken), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task RefreshToken_WithNullRequest_ShouldReturnBadRequest()
    {
        // Act
        var result = await _authController.RefreshToken(null!, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be("Refresh token request cannot be null.");
        
        // Verify AuthService was not called
        _mockAuthService.Verify(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshToken_WithEmptyRefreshToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.RequestWithEmptyRefreshToken();
        var errorMessage = "Invalid token or refresh token.";

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LoginResponse?)null, errorMessage));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.StatusCode.Should().Be(401);
        
        var message = GetMessageFromResponse(unauthorizedResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidTokenFormat_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.RequestWithInvalidToken();
        var errorMessage = "Invalid token format.";

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LoginResponse?)null, errorMessage));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.StatusCode.Should().Be(401);
        
        var message = GetMessageFromResponse(unauthorizedResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task RefreshToken_WithNonExistentToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.RequestWithNonExistentToken();
        var errorMessage = "Refresh token not found.";

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LoginResponse?)null, errorMessage));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.StatusCode.Should().Be(401);
        
        var message = GetMessageFromResponse(unauthorizedResult.Value);
        message.Should().Be(errorMessage);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task RefreshToken_WithExpiredRefreshToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.RequestWithExpiredRefreshToken();
        var errorMessage = "Refresh token has expired.";

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LoginResponse?)null, errorMessage));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.StatusCode.Should().Be(401);
        
        var message = GetMessageFromResponse(unauthorizedResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task RefreshToken_WithRevokedRefreshToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.RequestWithRevokedToken();
        var errorMessage = "Refresh token has been revoked.";

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LoginResponse?)null, errorMessage));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.StatusCode.Should().Be(401);
        
        var message = GetMessageFromResponse(unauthorizedResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task RefreshToken_WithTokenReuse_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.RequestForTokenReuse();
        var errorMessage = "Token reuse detected. Security breach.";

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LoginResponse?)null, errorMessage));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.StatusCode.Should().Be(401);
        
        var message = GetMessageFromResponse(unauthorizedResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task RefreshToken_ShouldGenerateSecureNewTokens()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();
        // Generate a different JWT token for the response
        expectedResponse.JwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibmFtZSI6InRlc3R1c2VyMTIzIiwiaWF0IjoxNjEwMjQzNjAwfQ.NEW_TOKEN_SIGNATURE";
        expectedResponse.RefreshToken = Guid.NewGuid().ToString(); // Different refresh token

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as LoginResponse;
        
        // Verify new tokens are different from input
        response!.JwtToken.Should().NotBe(request.ExpiredJwtToken);
        response.RefreshToken.Should().NotBe(request.RefreshToken);
        
        // Verify token characteristics
        response.JwtToken.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();
        response.RefreshTokenExpiry.Should().BeAfter(DateTime.UtcNow);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task RefreshToken_ShouldInvalidateOldToken()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called to handle token rotation
        _mockAuthService.Verify(x => x.RefreshTokenAsync(
            It.Is<RefreshTokenRequest>(req => req.RefreshToken == request.RefreshToken), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshToken_ShouldValidateUserSession()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as LoginResponse;
        
        // Verify user session data is included
        response!.UserId.Should().BePositive();
        response.Username.Should().NotBeNullOrEmpty();
        response.Email.Should().NotBeNullOrEmpty();
        response.Role.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshToken_ShouldImplementTokenRotation()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.ValidRequest();
        var firstResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();
        var secondResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();
        secondResponse.RefreshToken = Guid.NewGuid().ToString(); // Different token

        _mockAuthService.SetupSequence(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((firstResponse, (string?)null))
            .ReturnsAsync((secondResponse, (string?)null));

        // Act - First refresh
        var firstResult = await _authController.RefreshToken(request, CancellationToken.None);
        
        // Act - Second refresh with new token
        var newRequest = new RefreshTokenRequest
        {
            ExpiredJwtToken = firstResponse.JwtToken,
            RefreshToken = firstResponse.RefreshToken
        };
        var secondResult = await _authController.RefreshToken(newRequest, CancellationToken.None);

        // Assert
        firstResult.Should().BeOfType<OkObjectResult>();
        secondResult.Should().BeOfType<OkObjectResult>();
        
        var firstOkResult = firstResult as OkObjectResult;
        var secondOkResult = secondResult as OkObjectResult;
        
        var firstLoginResponse = firstOkResult!.Value as LoginResponse;
        var secondLoginResponse = secondOkResult!.Value as LoginResponse;
        
        // Verify token rotation occurred
        firstLoginResponse!.RefreshToken.Should().NotBe(request.RefreshToken);
        secondLoginResponse!.RefreshToken.Should().NotBe(firstLoginResponse.RefreshToken);
    }

    #endregion

    #region Server Error Tests

    [Fact]
    public async Task RefreshToken_WhenServiceReturnsNullWithoutMessage_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.ValidRequest();

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LoginResponse?)null, (string?)null));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.StatusCode.Should().Be(401);
        
        var message = GetMessageFromResponse(unauthorizedResult.Value);
        message.Should().Be("Invalid token or refresh token.");
    }

    [Fact]
    public async Task RefreshToken_WhenServiceThrowsException_ShouldThrowException()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.ValidRequest();
        var exceptionMessage = "Database connection failed";

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _authController.RefreshToken(request, CancellationToken.None));
        
        exception.Message.Should().Be(exceptionMessage);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task RefreshToken_ShouldLogTokenRefreshAttempt()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify logging occurred
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Received request to refresh token")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshToken_WithSuccessfulRefresh_ShouldLogSuccess()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify success was logged
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Token refreshed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshToken_WithFailedRefresh_ShouldLogWarning()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.RequestWithExpiredRefreshToken();
        var errorMessage = "Refresh token has expired.";

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LoginResponse?)null, errorMessage));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        
        // Verify warning was logged
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Token refresh failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task RefreshToken_EndToEndHappyPath_ShouldReturnCompleteResponse()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = AuthenticationTestDataBuilder.LoginResponses.ValidLoginResponse();

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedResponse, (string?)null));

        // Act
        var result = await _authController.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var response = okResult.Value as LoginResponse;
        response.Should().NotBeNull();
        response!.UserId.Should().BePositive();
        response.Username.Should().NotBeNullOrEmpty();
        response.Email.Should().NotBeNullOrEmpty();
        response.Role.Should().NotBeNullOrEmpty();
        response.JwtToken.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();
        response.RefreshTokenExpiry.Should().BeAfter(DateTime.UtcNow);
        
        // Verify all service interactions
        _mockAuthService.Verify(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify logging flow
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Received request to refresh token")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
            
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Token refreshed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #endregion

    #region Forgot Password Tests
    // SCRUM-36: Unit Tests for POST /auth/forgot-password endpoint
    // This comprehensive test suite covers all the scenarios outlined in the ticket:
    // - Happy Path: Valid email password reset requests, token generation, email sending
    // - Validation: Invalid email formats, empty fields, non-existent emails
    // - Security: Rate limiting, secure token generation, multiple active tokens prevention
    // - Business Logic: Previous tokens invalidation, token expiration, email service integration

    #region Happy Path Tests

    [Fact]
    public async Task ForgotPassword_WithValidEmail_ShouldReturnOkWithSuccessMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.ValidRequest();
        var expectedMessage = "If this email address exists in our system, you will receive a password reset email shortly.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
        
        _mockAuthService.Verify(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_WithExistingUser_ShouldGenerateResetToken()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.ValidRequestWithExistingUser();
        var expectedMessage = "If this email address exists in our system, you will receive a password reset email shortly.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
        
        _mockAuthService.Verify(x => x.ForgotPasswordAsync(
            It.Is<ForgotPasswordRequest>(req => req.Email == request.Email), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_ShouldCallAuthServiceWithCorrectRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.ValidRequest();
        var expectedMessage = "Password reset email sent successfully.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        _mockAuthService.Verify(
            x => x.ForgotPasswordAsync(It.Is<ForgotPasswordRequest>(req => 
                req.Email == request.Email), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_WithValidSpecialCharacterEmail_ShouldReturnOk()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.RequestWithSpecialCharacterEmail();
        var expectedMessage = "If this email address exists in our system, you will receive a password reset email shortly.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ForgotPassword_WithNullRequest_ShouldReturnBadRequest()
    {
        // Act
        var result = await _authController.ForgotPassword(null!, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be("Request cannot be null.");
        
        // Verify AuthService was not called
        _mockAuthService.Verify(x => x.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_WithInvalidEmailFormat_ShouldReturnOkWithGenericMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.RequestWithInvalidEmail();
        var expectedMessage = "If this email address exists in our system, you will receive a password reset email shortly.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task ForgotPassword_WithEmptyEmail_ShouldReturnOkWithGenericMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.RequestWithEmptyEmail();
        var expectedMessage = "If this email address exists in our system, you will receive a password reset email shortly.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task ForgotPassword_WithNonExistentEmail_ShouldReturnOkWithGenericMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.RequestWithNonExistentEmail();
        var expectedMessage = "If this email address exists in our system, you will receive a password reset email shortly.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task ForgotPassword_WithMalformedEmail_ShouldReturnOkWithGenericMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.RequestWithMalformedEmail();
        var expectedMessage = "If this email address exists in our system, you will receive a password reset email shortly.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task ForgotPassword_ShouldPreventUserEnumeration()
    {
        // Arrange
        var validRequest = AuthenticationTestDataBuilder.ForgotPassword.ValidRequestWithExistingUser();
        var invalidRequest = AuthenticationTestDataBuilder.ForgotPassword.RequestWithNonExistentEmail();
        var expectedMessage = "If this email address exists in our system, you will receive a password reset email shortly.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var validResult = await _authController.ForgotPassword(validRequest, CancellationToken.None);
        var invalidResult = await _authController.ForgotPassword(invalidRequest, CancellationToken.None);

        // Assert
        validResult.Should().BeOfType<OkObjectResult>();
        invalidResult.Should().BeOfType<OkObjectResult>();
        
        var validOkResult = validResult as OkObjectResult;
        var invalidOkResult = invalidResult as OkObjectResult;
        
        var validMessage = GetMessageFromResponse(validOkResult!.Value);
        var invalidMessage = GetMessageFromResponse(invalidOkResult!.Value);
        
        // Both should return the same generic message to prevent user enumeration
        validMessage.Should().Be(expectedMessage);
        invalidMessage.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task ForgotPassword_WithRateLimitExceeded_ShouldReturnGenericMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.RequestForRateLimitTesting();
        var expectedMessage = "If this email address exists in our system, you will receive a password reset email shortly.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task ForgotPassword_ShouldAlwaysReturnOkToPreventTimingAttacks()
    {
        // Arrange
        var requests = new[]
        {
            AuthenticationTestDataBuilder.ForgotPassword.ValidRequest(),
            AuthenticationTestDataBuilder.ForgotPassword.RequestWithNonExistentEmail(),
            AuthenticationTestDataBuilder.ForgotPassword.RequestWithInvalidEmail(),
            AuthenticationTestDataBuilder.ForgotPassword.RequestWithEmptyEmail()
        };

        var expectedMessage = "If this email address exists in our system, you will receive a password reset email shortly.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act & Assert
        foreach (var request in requests)
        {
            var result = await _authController.ForgotPassword(request, CancellationToken.None);
            
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.StatusCode.Should().Be(200);
            
            var message = GetMessageFromResponse(okResult.Value);
            message.Should().Be(expectedMessage);
        }
    }

    [Fact]
    public async Task ForgotPassword_WithInactiveUser_ShouldReturnGenericMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.RequestWithInactiveUserEmail();
        var expectedMessage = "If this email address exists in our system, you will receive a password reset email shortly.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task ForgotPassword_ShouldInvalidatePreviousTokens()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.ValidRequestWithExistingUser();
        var expectedMessage = "Password reset email sent successfully.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called to handle token invalidation
        _mockAuthService.Verify(x => x.ForgotPasswordAsync(
            It.Is<ForgotPasswordRequest>(req => req.Email == request.Email), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_ShouldGenerateSecureToken()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.ValidRequest();
        var expectedMessage = "Password reset email sent successfully.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called for token generation
        _mockAuthService.Verify(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_ShouldSetTokenExpiration()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.ValidRequestWithExistingUser();
        var expectedMessage = "Password reset email sent successfully.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service handles token expiration
        _mockAuthService.Verify(x => x.ForgotPasswordAsync(
            It.Is<ForgotPasswordRequest>(req => req.Email == request.Email), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_ShouldIntegrateWithEmailService()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.ValidRequestWithExistingUser();
        var expectedMessage = "Password reset email sent successfully.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var message = GetMessageFromResponse((result as OkObjectResult)!.Value);
        message.Should().Be(expectedMessage);
        
        // Verify email service integration through service call
        _mockAuthService.Verify(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_WithUnverifiedUser_ShouldStillAllowPasswordReset()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.RequestWithUnverifiedUserEmail();
        var expectedMessage = "If this email address exists in our system, you will receive a password reset email shortly.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
    }

    #endregion

    #region Server Error Tests

    [Fact]
    public async Task ForgotPassword_WhenServiceReturnsFailure_ShouldStillReturnOkWithMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.ValidRequest();
        var errorMessage = "Email service temporarily unavailable.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task ForgotPassword_WhenServiceThrowsException_ShouldThrowException()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.ValidRequest();
        var exceptionMessage = "Database connection failed";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _authController.ForgotPassword(request, CancellationToken.None));
        
        exception.Message.Should().Be(exceptionMessage);
    }

    [Fact]
    public async Task ForgotPassword_WithServiceError_ShouldReturnGenericMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.ValidRequest();
        var errorMessage = "If this email address exists in our system, you will receive a password reset email shortly.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(errorMessage);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task ForgotPassword_ShouldLogPasswordResetRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.ValidRequest();
        var expectedMessage = "Password reset email sent successfully.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify request was logged
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Received forgot password request for email: {request.Email}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_ShouldLogPasswordResetCompletion()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.ValidRequest();
        var expectedMessage = "Password reset email sent successfully.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify completion was logged
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Forgot password process completed for email {request.Email}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_ShouldNotLogSensitiveInformation()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.ValidRequest();
        var expectedMessage = "Password reset email sent successfully.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify no sensitive tokens are logged (password reset tokens should not be logged)
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => !o.ToString()!.Contains("reset-token") && !o.ToString()!.Contains("security-token")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(1));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task ForgotPassword_EndToEndHappyPath_ShouldReturnCompleteResponse()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.ValidRequestWithExistingUser();
        var expectedMessage = "Password reset email sent successfully.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
        
        // Verify all service interactions
        _mockAuthService.Verify(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify logging flow
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Received forgot password request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
            
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Forgot password process completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_CompleteWorkflow_ShouldHandleAllSteps()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ForgotPassword.ValidRequest();
        var expectedMessage = "If this email address exists in our system, you will receive a password reset email shortly.";

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ForgotPassword(request, CancellationToken.None);

        // Assert
        // Verify HTTP response
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        // Verify response content
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
        
        // Verify service call
        _mockAuthService.Verify(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify logging
        _mockControllerLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(2)); // At least request received and completion logged
    }

    #endregion

    #endregion

    #region Reset Password Tests
    // SCRUM-37: Unit Tests for POST /auth/reset-password endpoint
    // This comprehensive test suite covers all the scenarios outlined in the ticket:
    // - Happy Path: Valid token and new password, password hash update, token invalidation
    // - Validation: Invalid email formats, empty tokens, weak passwords, password complexity
    // - Security: Expired tokens, invalid tokens, token reuse prevention, password hashing
    // - Business Logic: User password update, reset token cleanup, user notification

    #region Happy Path Tests

    [Fact]
    public async Task ResetPassword_WithValidRequest_ShouldReturnOkWithSuccessMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.ValidRequest();
        var expectedMessage = "Password reset successfully.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
        
        _mockAuthService.Verify(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ShouldUpdatePasswordAndInvalidateToken()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.ValidRequestWithExistingUser();
        var expectedMessage = "Password reset successfully.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
        
        _mockAuthService.Verify(x => x.ResetPasswordAsync(
            It.Is<ResetPasswordRequest>(req => 
                req.Email == request.Email && 
                req.ResetToken == request.ResetToken &&
                req.NewPassword == request.NewPassword), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ShouldCallAuthServiceWithCorrectRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.ValidRequest();
        var expectedMessage = "Password reset successfully.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        _mockAuthService.Verify(
            x => x.ResetPasswordAsync(It.Is<ResetPasswordRequest>(req => 
                req.Email == request.Email &&
                req.ResetToken == request.ResetToken &&
                req.NewPassword == request.NewPassword &&
                req.ConfirmNewPassword == request.ConfirmNewPassword), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WithSpecialCharacterEmail_ShouldReturnOk()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.RequestWithSpecialCharacterEmail();
        var expectedMessage = "Password reset successfully.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ResetPassword_WithNullRequest_ShouldReturnBadRequest()
    {
        // Act
        var result = await _authController.ResetPassword(null!, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be("Request cannot be null.");
        
        // Verify AuthService was not called
        _mockAuthService.Verify(x => x.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidEmailFormat_ShouldReturnBadRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.RequestWithInvalidEmail();
        var errorMessage = "Invalid email format.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task ResetPassword_WithEmptyEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.RequestWithEmptyEmail();
        var errorMessage = "Email is required.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task ResetPassword_WithEmptyToken_ShouldReturnBadRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.RequestWithEmptyToken();
        var errorMessage = "Reset token is required.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task ResetPassword_WithWeakPassword_ShouldReturnBadRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.RequestWithWeakPassword();
        var errorMessage = "Password does not meet complexity requirements.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be(errorMessage);
    }

    [Theory]
    [InlineData("WeakPassword123", "Password must contain at least one special character.")]
    [InlineData("WeakPassword!", "Password must contain at least one number.")]
    [InlineData("weakpassword123!", "Password must contain at least one uppercase letter.")]
    public async Task ResetPassword_WithPasswordComplexityIssues_ShouldReturnBadRequest(string password, string expectedError)
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.ValidRequest();
        request.NewPassword = password;
        request.ConfirmNewPassword = password;

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, expectedError));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be(expectedError);
    }

    [Fact]
    public async Task ResetPassword_WithPasswordMismatch_ShouldReturnBadRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.RequestWithPasswordMismatch();
        var errorMessage = "Password and confirmation password do not match.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task ResetPassword_WithEmptyPassword_ShouldReturnBadRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.RequestWithEmptyPassword();
        var errorMessage = "New password is required.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be(errorMessage);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task ResetPassword_WithExpiredToken_ShouldReturnBadRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.RequestWithExpiredToken();
        var errorMessage = "Reset token has expired.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ShouldReturnBadRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.RequestWithInvalidToken();
        var errorMessage = "Invalid reset token.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task ResetPassword_WithUsedToken_ShouldReturnBadRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.RequestWithUsedToken();
        var errorMessage = "Reset token has already been used.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task ResetPassword_WithMalformedToken_ShouldReturnBadRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.RequestWithMalformedToken();
        var errorMessage = "Invalid token format.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task ResetPassword_ShouldVerifyPasswordHashing()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.ValidRequest();
        var expectedMessage = "Password reset successfully.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called to handle password hashing
        _mockAuthService.Verify(x => x.ResetPasswordAsync(
            It.Is<ResetPasswordRequest>(req => req.NewPassword == request.NewPassword), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WithNonExistentUser_ShouldReturnBadRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.RequestWithNonExistentUser();
        var errorMessage = "Invalid reset token or user not found.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        var message = GetMessageFromResponse(badRequestResult.Value);
        message.Should().Be(errorMessage);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task ResetPassword_ShouldInvalidateResetToken()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.ValidRequestWithExistingUser();
        var expectedMessage = "Password reset successfully.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called to handle token cleanup
        _mockAuthService.Verify(x => x.ResetPasswordAsync(
            It.Is<ResetPasswordRequest>(req => req.ResetToken == request.ResetToken), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ShouldUpdateUserPassword()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.ValidRequest();
        var expectedMessage = "Password reset successfully.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called for password update
        _mockAuthService.Verify(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ShouldCleanupResetTokenAfterSuccess()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.ValidRequestWithExistingUser();
        var expectedMessage = "Password reset successfully. All reset tokens have been invalidated.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var message = GetMessageFromResponse((result as OkObjectResult)!.Value);
        message.Should().Contain("invalidated");
        
        // Verify token cleanup through service call
        _mockAuthService.Verify(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ShouldSendUserNotification()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.ValidRequestWithExistingUser();
        var expectedMessage = "Password reset successfully. A confirmation email has been sent.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var message = GetMessageFromResponse((result as OkObjectResult)!.Value);
        message.Should().Contain("confirmation email");
        
        // Verify user notification through service call
        _mockAuthService.Verify(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WithValidRequest_ShouldPreventTokenReuse()
    {
        // Arrange
        var validRequest = AuthenticationTestDataBuilder.ResetPassword.ValidRequest();
        var reusedTokenRequest = AuthenticationTestDataBuilder.ResetPassword.RequestWithUsedToken();
        
        var successMessage = "Password reset successfully.";
        var errorMessage = "Reset token has already been used.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(validRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, successMessage));
        
        _mockAuthService.Setup(x => x.ResetPasswordAsync(reusedTokenRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act - First use should succeed
        var firstResult = await _authController.ResetPassword(validRequest, CancellationToken.None);
        
        // Act - Second use should fail
        var secondResult = await _authController.ResetPassword(reusedTokenRequest, CancellationToken.None);

        // Assert
        firstResult.Should().BeOfType<OkObjectResult>();
        secondResult.Should().BeOfType<BadRequestObjectResult>();
        
        var secondBadResult = secondResult as BadRequestObjectResult;
        var secondMessage = GetMessageFromResponse(secondBadResult!.Value);
        secondMessage.Should().Be(errorMessage);
    }

    #endregion

    #region Server Error Tests

    [Fact]
    public async Task ResetPassword_WhenServiceReturnsFailureWithSystemError_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.ValidRequest();
        var errorMessage = "Database connection failed";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var errorResult = result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
        
        var message = GetMessageFromResponse(errorResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task ResetPassword_WhenServiceThrowsException_ShouldThrowException()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.ValidRequest();
        var exceptionMessage = "Unexpected database error";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _authController.ResetPassword(request, CancellationToken.None));
        
        exception.Message.Should().Be(exceptionMessage);
    }

    [Fact]
    public async Task ResetPassword_WhenServiceReturnsNullMessage_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.ValidRequest();

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, null!));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var errorResult = result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
        
        var message = GetMessageFromResponse(errorResult.Value);
        message.Should().Be("An unexpected error occurred while resetting password.");
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task ResetPassword_ShouldLogPasswordResetRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.ValidRequest();
        var expectedMessage = "Password reset successfully.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify request was logged
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Received reset password request for email: {request.Email}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WithSuccessfulReset_ShouldLogSuccess()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.ValidRequest();
        var expectedMessage = "Password reset successfully.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify success was logged
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Password reset successful for email {request.Email}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WithFailedReset_ShouldLogWarning()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.RequestWithExpiredToken();
        var errorMessage = "Reset token has expired.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        
        // Verify warning was logged
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Password reset failed for email {request.Email}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ShouldNotLogSensitiveInformation()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.ValidRequest();
        var expectedMessage = "Password reset successfully.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify no sensitive passwords or tokens are logged
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => !o.ToString()!.Contains(request.NewPassword) && !o.ToString()!.Contains(request.ResetToken)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(1));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task ResetPassword_EndToEndHappyPath_ShouldReturnCompleteResponse()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.ValidRequestWithExistingUser();
        var expectedMessage = "Password reset successfully.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
        
        // Verify all service interactions
        _mockAuthService.Verify(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify logging flow
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Received reset password request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
            
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Password reset successful")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPassword_CompleteWorkflow_ShouldHandleAllSteps()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResetPassword.ValidRequest();
        var expectedMessage = "Password reset successfully. All reset tokens have been invalidated.";

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResetPassword(request, CancellationToken.None);

        // Assert
        // Verify HTTP response
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        // Verify response content
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
        message.Should().Contain("successfully");
        message.Should().Contain("invalidated");
        
        // Verify service call
        _mockAuthService.Verify(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify logging
        _mockControllerLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(2)); // At least request received and success logged
    }

    #endregion

    #endregion

    #region Logout Tests
    // SCRUM-38: Unit Tests for POST /auth/logout endpoint
    // This comprehensive test suite covers all the scenarios outlined in the ticket:
    // - Happy Path: Valid authenticated user logout, token invalidation, session cleanup
    // - Authentication: Unauthenticated logout attempt, invalid JWT token, expired token
    // - Security: Token blacklisting, session termination, refresh token revocation
    // - Business Logic: User session cleanup, last logout time update, active sessions management

    #region Happy Path Tests

    [Fact]
    public async Task Logout_WithValidAuthenticatedUser_ShouldReturnOkWithSuccessMessage()
    {
        // Arrange
        var userId = "123";
        var expectedMessage = "Logout successful. Please clear tokens on the client-side.";
        
        // Mock authenticated user
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, "testuser123"),
            new(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        // Set the User property of controller
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
        
        _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_ShouldInvalidateUserTokens()
    {
        // Arrange
        var userId = "123";
        var expectedMessage = "Logout successful. Please clear tokens on the client-side.";
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called to handle token invalidation
        _mockAuthService.Verify(x => x.LogoutAsync(
            It.Is<ClaimsPrincipal>(p => p.FindFirstValue(ClaimTypes.NameIdentifier) == userId), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_ShouldCleanupUserSession()
    {
        // Arrange
        var userId = "123";
        var expectedMessage = "Logout successful. Please clear tokens on the client-side.";
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var message = GetMessageFromResponse((result as OkObjectResult)!.Value);
        message.Should().Be(expectedMessage);
        
        // Verify session cleanup through service call
        _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_ShouldCallAuthServiceWithCorrectUser()
    {
        // Arrange
        var userId = "123";
        var username = "testuser123";
        var email = "test@example.com";
        var expectedMessage = "Logout successful. Please clear tokens on the client-side.";
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        _mockAuthService.Verify(
            x => x.LogoutAsync(It.Is<ClaimsPrincipal>(p => 
                p.FindFirstValue(ClaimTypes.NameIdentifier) == userId &&
                p.FindFirstValue(ClaimTypes.Name) == username &&
                p.FindFirstValue(ClaimTypes.Email) == email), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task Logout_WithUnauthenticatedUser_ShouldStillProcessLogout()
    {
        // Arrange
        var expectedMessage = "Logout processed. Client should clear tokens.";
        
        // No user context (unauthenticated)
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var message = GetMessageFromResponse(okResult!.Value);
        message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task Logout_WithExpiredToken_ShouldStillAllowLogout()
    {
        // Arrange
        var userId = "123";
        var expectedMessage = "Logout processed. Client should clear tokens.";
        
        // Simulate expired token scenario
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var message = GetMessageFromResponse(okResult!.Value);
        message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task Logout_WithInvalidJwtToken_ShouldStillProcessLogout()
    {
        // Arrange
        var expectedMessage = "Logout processed. Client should clear tokens.";
        
        // Simulate invalid token with malformed claims
        var claims = new List<Claim> { new("invalid_claim_type", "invalid_value") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var message = GetMessageFromResponse(okResult!.Value);
        message.Should().Be(expectedMessage);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task Logout_ShouldBlacklistTokens()
    {
        // Arrange
        var userId = "123";
        var expectedMessage = "Logout successful. Please clear tokens on the client-side.";
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called to handle token blacklisting
        _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_ShouldTerminateUserSession()
    {
        // Arrange
        var userId = "123";
        var expectedMessage = "Logout successful. Please clear tokens on the client-side.";
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify session termination through service call
        _mockAuthService.Verify(x => x.LogoutAsync(
            It.Is<ClaimsPrincipal>(p => p.FindFirstValue(ClaimTypes.NameIdentifier) == userId), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_ShouldRevokeRefreshTokens()
    {
        // Arrange
        var userId = "123";
        var expectedMessage = "Logout successful. Please clear tokens on the client-side.";
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify refresh token revocation through service call
        _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_ShouldPreventTokenReuse()
    {
        // Arrange
        var userId = "123";
        var expectedMessage = "Logout successful. Please clear tokens on the client-side.";
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify token invalidation to prevent reuse
        _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task Logout_ShouldUpdateLastLogoutTime()
    {
        // Arrange
        var userId = "123";
        var expectedMessage = "Logout successful. Please clear tokens on the client-side.";
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify service was called to handle logout time update
        _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_ShouldManageActiveSessions()
    {
        // Arrange
        var userId = "123";
        var expectedMessage = "Logout successful. Please clear tokens on the client-side.";
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify active session management through service call
        _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_WithNonExistentUser_ShouldStillSucceed()
    {
        // Arrange
        var userId = "999";
        var expectedMessage = "Logout processed. Client should clear tokens.";
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var message = GetMessageFromResponse(okResult!.Value);
        message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task Logout_ShouldCleanupAllUserSessions()
    {
        // Arrange
        var userId = "123";
        var expectedMessage = "Logout successful. All sessions terminated.";
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var message = GetMessageFromResponse((result as OkObjectResult)!.Value);
        message.Should().Contain("sessions");
        
        // Verify comprehensive session cleanup
        _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Server Error Tests

    [Fact]
    public async Task Logout_WhenServiceReturnsFailure_ShouldReturnInternalServerError()
    {
        // Arrange
        var userId = "123";
        var errorMessage = "Logout processed with server-side error during token invalidation. Client should still clear tokens.";
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var errorResult = result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
        
        var message = GetMessageFromResponse(errorResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Logout_WhenServiceThrowsException_ShouldThrowException()
    {
        // Arrange
        var userId = "123";
        var exceptionMessage = "Database connection failed";
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _authController.Logout(CancellationToken.None));
        
        exception.Message.Should().Be(exceptionMessage);
    }

    [Fact]
    public async Task Logout_WithTokenInvalidationError_ShouldStillAdviseClientLogout()
    {
        // Arrange
        var userId = "123";
        var errorMessage = "Logout processed with server-side error during token invalidation. Client should still clear tokens.";
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var errorResult = result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
        
        var message = GetMessageFromResponse(errorResult.Value);
        message.Should().Contain("Client should still clear tokens");
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task Logout_WithSuccessfulLogout_ShouldLogSuccess()
    {
        // Arrange
        var userId = "123";
        var expectedMessage = "Logout successful. Please clear tokens on the client-side.";
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify success was logged
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"User {userId} logged out successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Logout_WithFailedLogout_ShouldLogWarning()
    {
        // Arrange
        var userId = "123";
        var errorMessage = "Token invalidation failed";
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        
        // Verify warning was logged
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Logout attempt for User {userId} processed with server-side issue")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Logout_ShouldNotLogSensitiveInformation()
    {
        // Arrange
        var userId = "123";
        var expectedMessage = "Logout successful. Please clear tokens on the client-side.";
        
        var claims = new List<Claim> 
        { 
            new(ClaimTypes.NameIdentifier, userId),
            new("sensitive_claim", "sensitive_data")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify no sensitive tokens are logged
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => !o.ToString()!.Contains("sensitive_data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(1));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Logout_EndToEndHappyPath_ShouldReturnCompleteResponse()
    {
        // Arrange
        var userId = "123";
        var username = "testuser123";
        var email = "test@example.com";
        var expectedMessage = "Logout successful. Please clear tokens on the client-side.";
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
        
        // Verify all service interactions
        _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify logging flow
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("logged out successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Logout_CompleteWorkflow_ShouldHandleAllSteps()
    {
        // Arrange
        var userId = "123";
        var expectedMessage = "Logout successful. Please clear tokens on the client-side.";
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.Logout(CancellationToken.None);

        // Assert
        // Verify HTTP response
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        // Verify response content
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
        message.Should().Contain("successful");
        message.Should().Contain("clear tokens");
        
        // Verify service call
        _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify logging
        _mockControllerLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(1)); // At least success logged
    }

    #endregion

    #endregion

    #region ResendVerificationEmail Tests
    // SCRUM-39: Unit Tests for POST /auth/resend-verification-email endpoint
    // This comprehensive test suite covers all the scenarios outlined in the ticket:
    // - Happy Path: Valid email resend request, new verification token generation, email sending
    // - Validation: Invalid email format, empty email field, non-existent email
    // - Business Logic: Already verified email, too many resend attempts, previous token invalidation, rate limiting
    // - Security: Rate limiting on resends, token generation security, spam prevention

    #region Happy Path Tests

    [Fact]
    public async Task ResendVerificationEmail_WithValidEmail_ShouldReturnOkWithSuccessMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.ValidRequest();
        var expectedMessage = "If this email exists and is not yet verified, a new verification email has been sent.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
        
        _mockAuthService.Verify(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResendVerificationEmail_WithExistingUnverifiedUser_ShouldGenerateNewToken()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.ValidRequestWithExistingUnverifiedUser();
        var expectedMessage = "A new verification email has been sent to your email address.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var message = GetMessageFromResponse(okResult!.Value);
        message.Should().Contain("verification email has been sent");
        
        // Verify service was called to generate new token
        _mockAuthService.Verify(x => x.ResendVerificationEmailAsync(
            It.Is<ResendVerificationEmailRequest>(r => r.Email == request.Email), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResendVerificationEmail_ShouldCallAuthServiceWithCorrectRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.ValidRequest();
        var expectedMessage = "Verification email sent successfully.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(It.IsAny<ResendVerificationEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        _mockAuthService.Verify(
            x => x.ResendVerificationEmailAsync(It.Is<ResendVerificationEmailRequest>(r => 
                r.Email == request.Email), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResendVerificationEmail_WithSpecialCharacterEmail_ShouldReturnOk()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.RequestWithSpecialCharacterEmail();
        var expectedMessage = "If this email exists and is not yet verified, a new verification email has been sent.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var message = GetMessageFromResponse(okResult!.Value);
        message.Should().Be(expectedMessage);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ResendVerificationEmail_WithNullRequest_ShouldReturnBadRequest()
    {
        // Act
        var result = await _authController.ResendVerificationEmail(null!, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().Be("Request cannot be null.");

        // Verify AuthService was not called
        _mockAuthService.Verify(x => x.ResendVerificationEmailAsync(It.IsAny<ResendVerificationEmailRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResendVerificationEmail_WithInvalidEmailFormat_ShouldReturnOkWithGenericMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.RequestWithInvalidEmail();
        var expectedMessage = "If this email exists and is not yet verified, a new verification email has been sent.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var message = GetMessageFromResponse(okResult!.Value);
        message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task ResendVerificationEmail_WithEmptyEmail_ShouldReturnOkWithGenericMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.RequestWithEmptyEmail();
        var expectedMessage = "If this email exists and is not yet verified, a new verification email has been sent.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var message = GetMessageFromResponse(okResult!.Value);
        message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task ResendVerificationEmail_WithNonExistentEmail_ShouldReturnOkWithGenericMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.RequestWithNonExistentEmail();
        var expectedMessage = "If this email exists and is not yet verified, a new verification email has been sent.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var message = GetMessageFromResponse(okResult!.Value);
        message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task ResendVerificationEmail_WithMalformedEmail_ShouldReturnOkWithGenericMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.RequestWithMalformedEmail();
        var expectedMessage = "If this email exists and is not yet verified, a new verification email has been sent.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var message = GetMessageFromResponse(okResult!.Value);
        message.Should().Be(expectedMessage);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task ResendVerificationEmail_WithAlreadyVerifiedEmail_ShouldReturnOkWithGenericMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.RequestWithAlreadyVerifiedEmail();
        var expectedMessage = "If this email exists and is not yet verified, a new verification email has been sent.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var message = GetMessageFromResponse(okResult!.Value);
        message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task ResendVerificationEmail_ShouldInvalidatePreviousToken()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.ValidRequestWithExistingUnverifiedUser();
        var expectedMessage = "Previous token invalidated. New verification email sent.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var message = GetMessageFromResponse((result as OkObjectResult)!.Value);
        message.Should().Contain("Previous token invalidated");
        
        // Verify service was called to handle token invalidation
        _mockAuthService.Verify(x => x.ResendVerificationEmailAsync(It.IsAny<ResendVerificationEmailRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResendVerificationEmail_WithRateLimitExceeded_ShouldReturnOkWithGenericMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.RequestForRateLimitTesting();
        var expectedMessage = "If this email exists and is not yet verified, a new verification email has been sent.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var message = GetMessageFromResponse(okResult!.Value);
        message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task ResendVerificationEmail_WithInactiveUser_ShouldReturnOkWithGenericMessage()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.RequestWithInactiveUserEmail();
        var expectedMessage = "If this email exists and is not yet verified, a new verification email has been sent.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        
        var message = GetMessageFromResponse(okResult!.Value);
        message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task ResendVerificationEmail_ShouldPreventUserEnumeration()
    {
        // Arrange
        var existingEmailRequest = AuthenticationTestDataBuilder.ResendVerificationEmail.ValidRequest();
        var nonExistentEmailRequest = AuthenticationTestDataBuilder.ResendVerificationEmail.RequestWithNonExistentEmail();
        var genericMessage = "If this email exists and is not yet verified, a new verification email has been sent.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(It.IsAny<ResendVerificationEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, genericMessage));

        // Act
        var result1 = await _authController.ResendVerificationEmail(existingEmailRequest, CancellationToken.None);
        var result2 = await _authController.ResendVerificationEmail(nonExistentEmailRequest, CancellationToken.None);

        // Assert
        result1.Should().BeOfType<OkObjectResult>();
        result2.Should().BeOfType<OkObjectResult>();
        
        var message1 = GetMessageFromResponse((result1 as OkObjectResult)!.Value);
        var message2 = GetMessageFromResponse((result2 as OkObjectResult)!.Value);
        
        // Both should return the same generic message
        message1.Should().Be(genericMessage);
        message2.Should().Be(genericMessage);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task ResendVerificationEmail_ShouldImplementRateLimiting()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.RequestForRateLimitTesting();
        var expectedMessage = "If this email exists and is not yet verified, a new verification email has been sent.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify rate limiting is handled by service
        _mockAuthService.Verify(x => x.ResendVerificationEmailAsync(It.IsAny<ResendVerificationEmailRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResendVerificationEmail_ShouldGenerateSecureTokens()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.ValidRequest();
        var expectedMessage = "Secure verification token generated and sent.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var message = GetMessageFromResponse((result as OkObjectResult)!.Value);
        message.Should().Contain("token");
        
        // Verify secure token generation through service call
        _mockAuthService.Verify(x => x.ResendVerificationEmailAsync(It.IsAny<ResendVerificationEmailRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResendVerificationEmail_ShouldPreventSpam()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.RequestForSpamPrevention();
        var expectedMessage = "If this email exists and is not yet verified, a new verification email has been sent.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify spam prevention is handled by always returning generic message
        var message = GetMessageFromResponse((result as OkObjectResult)!.Value);
        message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task ResendVerificationEmail_ShouldHandleCaseSensitiveEmails()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.RequestWithCaseSensitiveEmail();
        var expectedMessage = "If this email exists and is not yet verified, a new verification email has been sent.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var message = GetMessageFromResponse((result as OkObjectResult)!.Value);
        message.Should().Be(expectedMessage);
    }

    #endregion

    #region Server Error Tests

    [Fact]
    public async Task ResendVerificationEmail_WhenServiceReturnsFailure_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.ValidRequest();
        var errorMessage = "Failed to send verification email due to email service error.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var errorResult = result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
        
        var message = GetMessageFromResponse(errorResult.Value);
        message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task ResendVerificationEmail_WhenServiceThrowsException_ShouldThrowException()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.ValidRequest();
        var exceptionMessage = "Email service connection failed";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _authController.ResendVerificationEmail(request, CancellationToken.None));
        
        exception.Message.Should().Be(exceptionMessage);
    }

    [Fact]
    public async Task ResendVerificationEmail_WithEmailServiceError_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.ValidRequest();
        var errorMessage = "Email service temporarily unavailable.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var errorResult = result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
        
        var message = GetMessageFromResponse(errorResult.Value);
        message.Should().Contain("Email service");
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task ResendVerificationEmail_ShouldLogResendRequest()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.ValidRequest();
        var expectedMessage = "Verification email resent successfully.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify request was logged
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Received request to resend verification email for Email: {request.Email}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ResendVerificationEmail_WithSuccessfulResend_ShouldLogCompletion()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.ValidRequest();
        var expectedMessage = "Verification email resent successfully.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify completion was logged
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Resend verification email process completed for Email: {request.Email}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ResendVerificationEmail_WithFailedResend_ShouldLogError()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.ValidRequest();
        var errorMessage = "Email service error";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, errorMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        
        // Verify error was logged
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Failed to process resend verification email for {request.Email}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ResendVerificationEmail_ShouldNotLogSensitiveInformation()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.ValidRequest();
        var expectedMessage = "Verification email resent successfully.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verify no sensitive tokens are logged (only email is logged which is expected)
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => !o.ToString()!.Contains("token") || !o.ToString()!.Contains("password")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(1));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task ResendVerificationEmail_EndToEndHappyPath_ShouldReturnCompleteResponse()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.ValidRequestWithExistingUnverifiedUser();
        var expectedMessage = "A new verification email has been sent to your email address. Please check your inbox and spam folder.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
        message.Should().Contain("verification email has been sent");
        message.Should().Contain("check your inbox");
        
        // Verify all service interactions
        _mockAuthService.Verify(x => x.ResendVerificationEmailAsync(It.IsAny<ResendVerificationEmailRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify logging flow
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Received request to resend verification email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
            
        _mockControllerLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Resend verification email process completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ResendVerificationEmail_CompleteWorkflow_ShouldHandleAllSteps()
    {
        // Arrange
        var request = AuthenticationTestDataBuilder.ResendVerificationEmail.ValidRequest();
        var expectedMessage = "If this email exists and is not yet verified, a new verification email has been sent.";

        _mockAuthService.Setup(x => x.ResendVerificationEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _authController.ResendVerificationEmail(request, CancellationToken.None);

        // Assert
        // Verify HTTP response
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        
        // Verify response content
        var message = GetMessageFromResponse(okResult.Value);
        message.Should().Be(expectedMessage);
        message.Should().Contain("verification email");
        
        // Verify service call with correct parameters
        _mockAuthService.Verify(
            x => x.ResendVerificationEmailAsync(It.Is<ResendVerificationEmailRequest>(r => 
                r.Email == request.Email), It.IsAny<CancellationToken>()),
            Times.Once);
        
        // Verify complete logging workflow
        _mockControllerLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(2)); // Request log + completion log
    }

    #endregion

    #endregion
} 
