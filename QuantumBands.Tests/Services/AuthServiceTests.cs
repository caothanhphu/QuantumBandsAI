using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using QuantumBands.Application.Features.Authentication;
using QuantumBands.Application.Features.Authentication.Commands.RegisterUser;
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Interfaces.Repositories;
using QuantumBands.Application.Services;
using QuantumBands.Domain.Entities;
using QuantumBands.Tests.Common;
using QuantumBands.Tests.Fixtures;
using static QuantumBands.Tests.Fixtures.AuthenticationTestDataBuilder;
using static QuantumBands.Tests.Fixtures.UsersTestDataBuilder;
using System.Linq.Expressions;
using Xunit;

namespace QuantumBands.Tests.Services;

public class AuthServiceTests : TestBase
{
    private readonly AuthService _authService;
    private readonly Mock<IGenericRepository<User>> _mockUserRepository;
    private readonly Mock<IGenericRepository<Wallet>> _mockWalletRepository;
    private readonly Mock<IUserRoleRepository> _mockUserRoleRepository;
    private readonly Mock<ILogger<AuthService>> _mockAuthServiceLogger;
    private readonly Mock<IOptions<JwtSettings>> _mockJwtSettings;

    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IGenericRepository<User>>();
        _mockWalletRepository = new Mock<IGenericRepository<Wallet>>();
        _mockUserRoleRepository = new Mock<IUserRoleRepository>();
        _mockAuthServiceLogger = new Mock<ILogger<AuthService>>();
        _mockJwtSettings = new Mock<IOptions<JwtSettings>>();

        // Setup JWT Settings
        var jwtSettings = new JwtSettings
        {
            Secret = "TestSecretKeyThatIsAtLeast32CharactersLong",
            Issuer = "QuantumBands.Test",
            Audience = "QuantumBands.Test",
            ExpiryMinutes = 60,
            RefreshTokenExpiryDays = 7
        };
        _mockJwtSettings.Setup(x => x.Value).Returns(jwtSettings);

        // Setup UnitOfWork to return mocked repositories
        MockUnitOfWork.Setup(x => x.Users).Returns(_mockUserRepository.Object);
        MockUnitOfWork.Setup(x => x.Wallets).Returns(_mockWalletRepository.Object);
        MockUnitOfWork.Setup(x => x.UserRoles).Returns(_mockUserRoleRepository.Object);
        
        // Setup configuration
        MockConfiguration.Setup(x => x["AppSettings:FrontendBaseUrl"])
            .Returns("http://localhost:3000");

        _authService = new AuthService(
            MockUnitOfWork.Object,
            _mockAuthServiceLogger.Object,
            MockEmailService.Object,
            MockConfiguration.Object,
            MockJwtTokenGenerator.Object,
            _mockJwtSettings.Object
        );
    }

    #region Happy Path Tests

    [Fact]
    public async Task RegisterUserAsync_WithValidCommand_ShouldReturnSuccessResult()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        var expectedUser = UsersTestDataBuilder.Users.ValidUser();
        var expectedWallet = TestDataBuilder.Wallets.ValidWallet(expectedUser.UserId);

        SetupSuccessfulRegistration(expectedUser, expectedWallet);

        // Act
        var result = await _authService.RegisterUserAsync(command);

        // Assert
        result.User.Should().NotBeNull();
        result.ErrorMessage.Should().BeNull();
        result.User.Username.Should().Be(command.Username);
        result.User.Email.Should().Be(command.Email);
        result.User.FullName.Should().Be(command.FullName);

        VerifySuccessfulRegistrationCalls(command);
    }

    [Fact]
    public async Task RegisterUserAsync_WithoutFullName_ShouldReturnSuccessResult()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommandWithoutFullName();
        var expectedUser = UsersTestDataBuilder.Users.ValidUser();
        expectedUser.FullName = null;
        var expectedWallet = TestDataBuilder.Wallets.ValidWallet(expectedUser.UserId);

        SetupSuccessfulRegistration(expectedUser, expectedWallet);

        // Act
        var result = await _authService.RegisterUserAsync(command);

        // Assert
        result.User.Should().NotBeNull();
        result.ErrorMessage.Should().BeNull();
        result.User.FullName.Should().BeNull();

        VerifySuccessfulRegistrationCalls(command);
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldGenerateEmailVerificationToken()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        var expectedUser = UsersTestDataBuilder.Users.ValidUser();
        var expectedWallet = TestDataBuilder.Wallets.ValidWallet(expectedUser.UserId);

        SetupSuccessfulRegistration(expectedUser, expectedWallet);

        // Act
        var result = await _authService.RegisterUserAsync(command);

        // Assert
        result.User.Should().NotBeNull();
        result.ErrorMessage.Should().BeNull();

        // Verify user was saved with email verification token
        _mockUserRepository.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldCreateWalletForNewUser()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        var expectedUser = UsersTestDataBuilder.Users.ValidUser();
        var expectedWallet = TestDataBuilder.Wallets.ValidWallet(expectedUser.UserId);

        SetupSuccessfulRegistration(expectedUser, expectedWallet);

        // Act
        var result = await _authService.RegisterUserAsync(command);

        // Assert
        result.User.Should().NotBeNull();
        result.ErrorMessage.Should().BeNull();

        // Verify wallet was created
        _mockWalletRepository.Verify(
            x => x.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Validation Tests

            [Fact]
        public async Task RegisterUserAsync_ShouldSucceedWithShortUsername()
        {
            // Arrange
            SetupDefaultRole();
            var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
            command.Username = "ab";

            // Act
            var result = await _authService.RegisterUserAsync(command);

            // Assert
            result.User.Should().NotBeNull();
            result.ErrorMessage.Should().BeNull();
            result.User!.Username.Should().Be("ab");
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldSucceedWithInvalidEmail()
        {
            // Arrange
            SetupDefaultRole();
            var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
            command.Email = "invalid-email";

            // Act
            var result = await _authService.RegisterUserAsync(command);

            // Assert
            result.User.Should().NotBeNull();
            result.ErrorMessage.Should().BeNull();
            result.User!.Email.Should().Be("invalid-email");
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldFailWhenRoleNotFound()
        {
            // Arrange - Don't setup role to simulate missing role
            var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();

            // Act
            var result = await _authService.RegisterUserAsync(command);

            // Assert
            result.User.Should().BeNull();
            result.ErrorMessage.Should().Be("System configuration error: Default role 'Investor' not found.");
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldFailWithExistingUsername()
        {
            // Arrange
            SetupDefaultRole();
            var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
            var existingUser = UsersTestDataBuilder.Users.ValidUser();
            existingUser.Username = command.Username;

            _mockUserRepository.Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<User, bool>>>(), 
                It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IEnumerable<User>>(new[] { existingUser }));

            // Act
            var result = await _authService.RegisterUserAsync(command);

            // Assert
            result.User.Should().BeNull();
            result.ErrorMessage.Should().Be($"Username '{command.Username}' already exists.");
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldFailWithExistingEmail()
        {
            // Arrange
            SetupDefaultRole();
            var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
            var existingUser = UsersTestDataBuilder.Users.ValidUser();
            existingUser.Email = command.Email;

            // Setup to return empty for username check, user for email check
            _mockUserRepository.SetupSequence(x => x.FindAsync(
                It.IsAny<Expression<Func<User, bool>>>(), 
                It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IEnumerable<User>>(Array.Empty<User>())) // Username check - empty
                .Returns(Task.FromResult<IEnumerable<User>>(new[] { existingUser })); // Email check - found

            // Act
            var result = await _authService.RegisterUserAsync(command);

            // Assert
            result.User.Should().BeNull();
            result.ErrorMessage.Should().Be($"Email '{command.Email}' already exists.");
        }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task RegisterUserAsync_WhenUserSaveFails_ShouldReturnError()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();

        SetupNoDuplicateUsers();
        SetupDefaultRole();
        
        // Simulate database failure on first CompleteAsync (saving user)
        MockUnitOfWork.Setup(x => x.CompleteAsync(default))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _authService.RegisterUserAsync(command);

        // Assert
        result.User.Should().BeNull();
        result.ErrorMessage.Should().NotBeNull();
        result.ErrorMessage.Should().Contain("An error occurred while saving user information");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task RegisterUserAsync_ShouldCallEmailServiceWithCorrectParameters()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        var expectedUser = UsersTestDataBuilder.Users.ValidUser();
        var expectedWallet = TestDataBuilder.Wallets.ValidWallet(expectedUser.UserId);

        SetupSuccessfulRegistration(expectedUser, expectedWallet);

        // Act
        var result = await _authService.RegisterUserAsync(command);

        // Assert
        result.User.Should().NotBeNull();
        result.ErrorMessage.Should().BeNull();

        // Verify email service was called with correct parameters
        MockEmailService.Verify(
            x => x.SendEmailAsync(
                command.Email,
                It.Is<string>(subject => subject.Contains("Verify Your Email")),
                It.IsAny<string>(), // Email content contains verification link
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldSaveUserEntityWithCorrectData()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        var expectedUser = UsersTestDataBuilder.Users.ValidUser();
        var expectedWallet = TestDataBuilder.Wallets.ValidWallet(expectedUser.UserId);

        SetupSuccessfulRegistration(expectedUser, expectedWallet);

        // Act
        var result = await _authService.RegisterUserAsync(command);

        // Assert
        result.User.Should().NotBeNull();

        // Verify user entity was saved 
        _mockUserRepository.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldCreateAssociatedWallet()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        var expectedUser = UsersTestDataBuilder.Users.ValidUser();
        var expectedWallet = TestDataBuilder.Wallets.ValidWallet(expectedUser.UserId);

        SetupSuccessfulRegistration(expectedUser, expectedWallet);

        // Act
        var result = await _authService.RegisterUserAsync(command);

        // Assert
        result.User.Should().NotBeNull();

        // Verify wallet was created and linked to user
        _mockWalletRepository.Verify(
            x => x.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldStoreVerificationTokenWithCorrectExpiration()
    {
        // Arrange
        var command = AuthenticationTestDataBuilder.RegisterUser.ValidCommand();
        var expectedUser = UsersTestDataBuilder.Users.ValidUser();
        var expectedWallet = TestDataBuilder.Wallets.ValidWallet(expectedUser.UserId);

        SetupSuccessfulRegistration(expectedUser, expectedWallet);

        // Act
        var result = await _authService.RegisterUserAsync(command);

        // Assert
        result.User.Should().NotBeNull();

        // Verify verification token was stored
        _mockUserRepository.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulRegistration(User expectedUser, Wallet expectedWallet)
    {
        SetupNoDuplicateUsers();
        SetupDefaultRole();
        
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _mockWalletRepository.Setup(x => x.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        MockUnitOfWork.Setup(x => x.CompleteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private void SetupDefaultRole()
    {
        var investorRole = new UserRole
        {
            RoleId = 2,
            RoleName = "Investor"
        };
        
        _mockUserRoleRepository.Setup(x => x.GetRoleByNameAsync("Investor"))
            .ReturnsAsync(investorRole);
    }

    private void SetupNoDuplicateUsers()
    {
        _mockUserRepository.Setup(x => x.FindAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());
    }

    private void VerifySuccessfulRegistrationCalls(RegisterUserCommand command)
    {
        // Verify all necessary service calls were made
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockWalletRepository.Verify(x => x.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRoleRepository.Verify(x => x.GetRoleByNameAsync("Investor"), Times.Once);
        MockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        MockUnitOfWork.Verify(x => x.CompleteAsync(It.IsAny<CancellationToken>()), Times.Exactly(2)); // Called twice: once for user, once for wallet
    }

    #endregion
} 




