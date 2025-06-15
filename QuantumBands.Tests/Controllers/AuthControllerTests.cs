using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Features.Authentication.Commands.RegisterUser;
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
} 
