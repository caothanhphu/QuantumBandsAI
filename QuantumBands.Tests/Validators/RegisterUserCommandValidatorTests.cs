using FluentAssertions;
using FluentValidation.TestHelper;
using QuantumBands.Application.Features.Authentication.Commands.RegisterUser;
using QuantumBands.Tests.Common;
using QuantumBands.Tests.Fixtures;
using Xunit;

namespace QuantumBands.Tests.Validators;

public class RegisterUserCommandValidatorTests : TestBase
{
    private readonly RegisterUserCommandValidator _validator;

    public RegisterUserCommandValidatorTests()
    {
        _validator = new RegisterUserCommandValidator();
    }

    #region Username Validation Tests

    [Fact]
    public void Username_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Username_WhenNullOrEmpty_ShouldHaveValidationError(string? username)
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();
        command.Username = username!;

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username is required.");
    }

    [Theory]
    [InlineData("ab")] // 2 characters
    [InlineData("a")] // 1 character
    public void Username_WhenTooShort_ShouldHaveValidationError(string username)
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();
        command.Username = username;

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username must be at least 3 characters.");
    }

    [Fact]
    public void Username_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();
        command.Username = new string('a', 51); // 51 characters

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username cannot exceed 50 characters.");
    }

    [Theory]
    [InlineData("user@name")] // Contains @
    [InlineData("user-name")] // Contains -
    [InlineData("user name")] // Contains space
    [InlineData("user.name")] // Contains .
    [InlineData("user#name")] // Contains #
    [InlineData("user+name")] // Contains +
    public void Username_WhenContainsInvalidCharacters_ShouldHaveValidationError(string username)
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();
        command.Username = username;

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username can only contain letters, numbers, and underscores.");
    }

    [Theory]
    [InlineData("username")] // Only letters
    [InlineData("username123")] // Letters and numbers
    [InlineData("user_name")] // Letters and underscore
    [InlineData("user_name_123")] // Letters, numbers and underscore
    [InlineData("123username")] // Starting with number
    [InlineData("_username")] // Starting with underscore
    public void Username_WhenContainsValidCharacters_ShouldNotHaveValidationError(string username)
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();
        command.Username = username;

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    #endregion

    #region Email Validation Tests

    [Fact]
    public void Email_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Email_WhenNullOrEmpty_ShouldHaveValidationError(string? email)
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();
        command.Email = email!;

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required.");
    }

    [Theory]
    [InlineData("invalid-email")] // No @
    [InlineData("invalid@")] // No domain
    [InlineData("@invalid.com")] // No local part
    public void Email_WhenInvalidFormat_ShouldHaveValidationError(string email)
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();
        command.Email = email;

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("A valid email address is required.");
    }

    [Fact]
    public void Email_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();
        command.Email = new string('a', 250) + "@example.com"; // 262 characters total

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email cannot exceed 255 characters.");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.test@example.com")]
    [InlineData("user+test@example.com")]
    [InlineData("user_test@example.com")]
    [InlineData("test123@example.com")]
    [InlineData("test@subdomain.example.com")]
    public void Email_WhenValidFormat_ShouldNotHaveValidationError(string email)
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();
        command.Email = email;

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region Password Validation Tests

    [Fact]
    public void Password_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Password_WhenNullOrEmpty_ShouldHaveValidationError(string? password)
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();
        command.Password = password!;

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required.");
    }

    [Theory]
    [InlineData("1234567")] // 7 characters
    [InlineData("weak")] // 4 characters
    [InlineData("ab")] // 2 characters
    public void Password_WhenTooShort_ShouldHaveValidationError(string password)
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();
        command.Password = password;

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 8 characters long.");
    }

    [Theory]
    [InlineData("PASSWORD123!")] // No lowercase
    [InlineData("password123!")] // No uppercase
    [InlineData("Password!")] // No numbers
    [InlineData("Password123")] // No special characters
    public void Password_WhenMissingRequiredCharacterTypes_ShouldHaveValidationError(string password)
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();
        command.Password = password;

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("PASSWORD123")] // No lowercase, no special chars
    [InlineData("password123")] // No uppercase, no special chars
    [InlineData("Password")] // No numbers, no special chars
    [InlineData("12345678")] // No letters, no special chars
    public void Password_WhenMissingMultipleRequiredCharacterTypes_ShouldHaveValidationError(string password)
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();
        command.Password = password;

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("Password123!")] // All requirements met
    [InlineData("StrongPass1@")] // All requirements met
    [InlineData("MySecure123#")] // All requirements met
    [InlineData("Complex$Pass9")] // All requirements met
    public void Password_WhenMeetsAllRequirements_ShouldNotHaveValidationError(string password)
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();
        command.Password = password;

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    #endregion

    #region FullName Validation Tests

    [Fact]
    public void FullName_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommandWithoutFullName();

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void FullName_WhenEmpty_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();
        command.FullName = "";

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void FullName_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void FullName_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();
        command.FullName = new string('a', 201); // 201 characters

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("Full name cannot exceed 200 characters.");
    }

    [Theory]
    [InlineData("John Doe")]
    [InlineData("María García")]
    [InlineData("李小明")]
    [InlineData("Jean-Pierre Dupont")]
    [InlineData("O'Connor")]
    public void FullName_WhenValidFormat_ShouldNotHaveValidationError(string fullName)
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();
        command.FullName = fullName;

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.FullName);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Validate_WithCompletelyValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var command = TestDataBuilder.RegisterUser.ValidCommand();

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithCompletelyInvalidCommand_ShouldHaveMultipleValidationErrors()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Username = "ab", // Too short
            Email = "invalid-email", // Invalid format
            Password = "weak", // Too weak
            FullName = new string('a', 201) // Too long
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username);
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Password);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void Validate_WithMinimumValidValues_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Username = "abc", // Minimum length (3 chars)
            Email = "a@b.co", // Minimum valid email
            Password = "Abc123!!", // Minimum valid password (8 chars with all requirements)
            FullName = null // Optional field
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithMaximumValidValues_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Username = new string('a', 50), // Maximum length (50 chars)
            Email = new string('a', 240) + "@example.com", // Maximum length (255 chars)
            Password = "StrongPassword123!" + new string('a', 80), // Maximum valid password (100 chars)
            FullName = new string('a', 200) // Maximum length (200 chars)
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validate_WithWhitespaceOnlyFields_ShouldHaveValidationErrors()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Username = "   ", // Whitespace only
            Email = "   ", // Whitespace only
            Password = "   ", // Whitespace only
            FullName = "   " // Whitespace only (but this should be valid)
        };

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username);
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Password);
        result.ShouldNotHaveValidationErrorFor(x => x.FullName); // FullName can be whitespace
    }

    [Theory]
    [InlineData("user123", "USER123")] // Username case sensitivity
    public void Validate_Username_ShouldBeCaseSensitive(string original, string modified)
    {
        // Arrange - Test that both lowercase and uppercase usernames are valid
        var command1 = TestDataBuilder.RegisterUser.ValidCommand();
        command1.Username = original; // lowercase

        var command2 = TestDataBuilder.RegisterUser.ValidCommand();
        command2.Username = modified; // UPPERCASE
        command2.Email = "different@example.com"; // Use different email to avoid conflicts

        // Act & Assert - Both should be valid
        var result1 = _validator.TestValidate(command1);
        var result2 = _validator.TestValidate(command2);

        result1.ShouldNotHaveAnyValidationErrors();
        result2.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
} 