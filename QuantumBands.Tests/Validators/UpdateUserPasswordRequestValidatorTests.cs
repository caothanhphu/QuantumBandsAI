using FluentAssertions;
using FluentValidation.TestHelper;
using QuantumBands.Application.Features.Admin.Users.Commands.UpdateUserPassword;
using QuantumBands.Tests.Common;
using Xunit;

namespace QuantumBands.Tests.Validators;

public class UpdateUserPasswordRequestValidatorTests : TestBase
{
    private readonly UpdateUserPasswordRequestValidator _validator;

    public UpdateUserPasswordRequestValidatorTests()
    {
        _validator = new UpdateUserPasswordRequestValidator();
    }

    #region NewPassword Validation Tests

    [Fact]
    public void NewPassword_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "SecurePass123!",
            ConfirmNewPassword = "SecurePass123!",
            Reason = "Password reset requested"
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void NewPassword_WhenNullOrEmpty_ShouldHaveValidationError(string? newPassword)
    {
        // Arrange
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = newPassword!,
            ConfirmNewPassword = "SecurePass123!",
            Reason = "Test"
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password is required.");
    }

    [Theory]
    [InlineData("1234567")] // 7 characters
    [InlineData("abc123")] // 6 characters
    public void NewPassword_WhenTooShort_ShouldHaveValidationError(string newPassword)
    {
        // Arrange
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = newPassword,
            ConfirmNewPassword = newPassword,
            Reason = "Test"
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password must be at least 8 characters long.");
    }

    [Fact]
    public void NewPassword_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var longPassword = new string('a', 101); // 101 characters
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = longPassword,
            ConfirmNewPassword = longPassword,
            Reason = "Test"
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password cannot exceed 100 characters.");
    }

    [Theory]
    [InlineData("password123!")] // No uppercase
    [InlineData("PASSWORD123!")] // No lowercase
    [InlineData("Password!")] // No number
    [InlineData("Password123")] // No special character
    public void NewPassword_WhenMissingRequiredCharacters_ShouldHaveValidationError(string newPassword)
    {
        // Arrange
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = newPassword,
            ConfirmNewPassword = newPassword,
            Reason = "Test"
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    #endregion

    #region ConfirmNewPassword Validation Tests

    [Fact]
    public void ConfirmNewPassword_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "SecurePass123!",
            ConfirmNewPassword = "SecurePass123!",
            Reason = "Password reset requested"
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.ConfirmNewPassword);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ConfirmNewPassword_WhenNullOrEmpty_ShouldHaveValidationError(string? confirmPassword)
    {
        // Arrange
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "SecurePass123!",
            ConfirmNewPassword = confirmPassword!,
            Reason = "Test"
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ConfirmNewPassword)
            .WithErrorMessage("Confirm new password is required.");
    }

    [Fact]
    public void ConfirmNewPassword_WhenDoesNotMatch_ShouldHaveValidationError()
    {
        // Arrange
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "SecurePass123!",
            ConfirmNewPassword = "DifferentPass456@",
            Reason = "Test"
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ConfirmNewPassword)
            .WithErrorMessage("New password and confirmation password do not match.");
    }

    #endregion

    #region Reason Validation Tests

    [Fact]
    public void Reason_WhenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "SecurePass123!",
            ConfirmNewPassword = "SecurePass123!",
            Reason = "Password reset requested by user"
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Reason_WhenNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "SecurePass123!",
            ConfirmNewPassword = "SecurePass123!",
            Reason = null
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Reason_WhenEmpty_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "SecurePass123!",
            ConfirmNewPassword = "SecurePass123!",
            Reason = ""
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Reason_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var longReason = new string('a', 501); // 501 characters
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "SecurePass123!",
            ConfirmNewPassword = "SecurePass123!",
            Reason = longReason
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Reason cannot exceed 500 characters.");
    }

    #endregion

    #region Full Validation Tests

    [Fact]
    public void ValidRequest_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var request = new UpdateUserPasswordRequest
        {
            NewPassword = "SecurePass123!",
            ConfirmNewPassword = "SecurePass123!",
            Reason = "Password reset requested by user"
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}