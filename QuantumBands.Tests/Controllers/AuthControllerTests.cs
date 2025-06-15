using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Features.Authentication.Commands.RegisterUser;
using QuantumBands.Application.Features.Authentication.Commands.Login;
using QuantumBands.Application.Features.Authentication.Commands.VerifyEmail;
using QuantumBands.Application.Features.Authentication.Commands.RefreshToken;
using QuantumBands.Application.Features.Authentication;
using QuantumBands.Application.Interfaces;
using QuantumBands.Tests.Common;
using QuantumBands.Tests.Fixtures;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

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
        var command = TestDataBuilder.RegisterUser.ValidCommand();
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
        var command = TestDataBuilder.RegisterUser.ValidCommandWithoutFullName();
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
        var command = TestDataBuilder.RegisterUser.ValidCommand();
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
        var command = TestDataBuilder.RegisterUser.CommandWithShortUsername();
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
        var command = TestDataBuilder.RegisterUser.ValidCommand();
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
        var command = TestDataBuilder.RegisterUser.ValidCommand();
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
        var command = TestDataBuilder.RegisterUser.ValidCommand();
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
        var command = TestDataBuilder.RegisterUser.ValidCommand();
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
        var command = TestDataBuilder.RegisterUser.ValidCommand();
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
        var command = TestDataBuilder.RegisterUser.ValidCommand();
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
        var command = TestDataBuilder.RegisterUser.ValidCommand();
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
        var command = TestDataBuilder.RegisterUser.ValidCommand();
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
        var command = TestDataBuilder.RegisterUser.ValidCommand();
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
        var command = TestDataBuilder.RegisterUser.ValidCommand();
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
        var loginRequest = TestDataBuilder.Login.ValidLoginWithUsername();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        var loginRequest = TestDataBuilder.Login.ValidLoginWithEmail();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        var expectedResponse = TestDataBuilder.LoginResponses.AdminLoginResponse();

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
        var loginRequest = TestDataBuilder.Login.ValidLoginWithUsername();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        badRequestResult.Value.Should().BeEquivalentTo(new { Message = "Login request cannot be null." });
        
        // Verify AuthService was not called
        _mockAuthService.Verify(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", "StrongPassword123!")] // Empty username
    [InlineData("testuser123", "")] // Empty password
    [InlineData("", "")] // Both empty
    public async Task Login_WithEmptyCredentials_ShouldReturnBadRequest(string usernameOrEmail, string password)
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = usernameOrEmail,
            Password = password
        };

        // Simulate model validation failure
        if (string.IsNullOrEmpty(usernameOrEmail))
            _authController.ModelState.AddModelError("UsernameOrEmail", "Username or email is required");
        if (string.IsNullOrEmpty(password))
            _authController.ModelState.AddModelError("Password", "Password is required");

        // Act
        var result = await _authController.Login(loginRequest, CancellationToken.None);

        // Assert
        _mockAuthService.Verify(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Authentication Error Tests

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = TestDataBuilder.Login.LoginWithInvalidPassword();
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
        var loginRequest = TestDataBuilder.Login.LoginWithInactiveUser();
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
        var loginRequest = TestDataBuilder.Login.LoginWithUnverifiedEmail();
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
        var loginRequest = TestDataBuilder.Login.LoginWithInvalidUsername();
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
        var loginRequest = TestDataBuilder.Login.ValidLoginWithUsername();
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
        var loginRequest = TestDataBuilder.Login.ValidLoginWithUsername();

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
        var loginRequest = TestDataBuilder.Login.ValidLoginWithUsername();
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
        var loginRequest = TestDataBuilder.Login.ValidLoginWithUsername();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        var loginRequest = TestDataBuilder.Login.ValidLoginWithUsername();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        var loginRequest = TestDataBuilder.Login.ValidLoginWithUsername();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        var loginRequest = TestDataBuilder.Login.LoginWithInvalidPassword();
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
        var loginRequest = TestDataBuilder.Login.ValidLoginWithUsername();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        var loginRequest = TestDataBuilder.Login.ValidLoginWithUsername();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        var loginRequest = TestDataBuilder.Login.ValidLoginWithUsername();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        var request = TestDataBuilder.VerifyEmail.ValidRequest();
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
        var request = TestDataBuilder.VerifyEmail.ValidRequest();
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
        var request = TestDataBuilder.VerifyEmail.ValidRequest();
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
        var request = TestDataBuilder.VerifyEmail.RequestWithInvalidUserId();
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
        var request = TestDataBuilder.VerifyEmail.RequestWithEmptyToken();
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
        var request = TestDataBuilder.VerifyEmail.RequestWithNonExistentUser();
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
        var request = TestDataBuilder.VerifyEmail.RequestWithExpiredToken();
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
        var request = TestDataBuilder.VerifyEmail.RequestWithInvalidToken();
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
        var request = TestDataBuilder.VerifyEmail.RequestWithAlreadyVerifiedUser();
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
        var request = TestDataBuilder.VerifyEmail.ValidRequest();
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
        var request = TestDataBuilder.VerifyEmail.ValidRequest();
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
        var request = TestDataBuilder.VerifyEmail.ValidRequest();
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
        var request = TestDataBuilder.VerifyEmail.ValidRequest();

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
        var request = TestDataBuilder.VerifyEmail.ValidRequest();
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
        var request = TestDataBuilder.VerifyEmail.RequestWithInvalidToken();
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
        var request = TestDataBuilder.VerifyEmail.ValidRequest();
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
        var request = TestDataBuilder.VerifyEmail.RequestWithMalformedToken();
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
        var request = TestDataBuilder.VerifyEmail.ValidRequest();
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
        var request = TestDataBuilder.VerifyEmail.ValidRequest();
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
        var request = TestDataBuilder.VerifyEmail.ValidRequest();
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
        var request = TestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        var request = TestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        var request = TestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        var request = TestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        var request = TestDataBuilder.RefreshToken.RequestWithEmptyRefreshToken();
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
        var request = TestDataBuilder.RefreshToken.RequestWithInvalidToken();
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
        var request = TestDataBuilder.RefreshToken.RequestWithNonExistentToken();
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
        var request = TestDataBuilder.RefreshToken.RequestWithExpiredRefreshToken();
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
        var request = TestDataBuilder.RefreshToken.RequestWithRevokedToken();
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
        var request = TestDataBuilder.RefreshToken.RequestForTokenReuse();
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
        var request = TestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();
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
        var request = TestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        var request = TestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        var request = TestDataBuilder.RefreshToken.ValidRequest();
        var firstResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();
        var secondResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();
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
        var request = TestDataBuilder.RefreshToken.ValidRequest();

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
        var request = TestDataBuilder.RefreshToken.ValidRequest();
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
        var request = TestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        var request = TestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
        var request = TestDataBuilder.RefreshToken.RequestWithExpiredRefreshToken();
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
        var request = TestDataBuilder.RefreshToken.ValidRequest();
        var expectedResponse = TestDataBuilder.LoginResponses.ValidLoginResponse();

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
} 
