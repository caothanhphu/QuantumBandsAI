// QuantumBands.Application/Features/Users/Commands/Enable2FA/Enable2FARequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Users.Commands.Enable2FA;

public class Enable2FARequestValidator : AbstractValidator<Enable2FARequest>
{
    public Enable2FARequestValidator()
    {
        RuleFor(x => x.VerificationCode)
            .NotEmpty().WithMessage("Verification code is required.")
            .Length(6).WithMessage("Verification code must be 6 digits.")
            .Matches("^[0-9]{6}$").WithMessage("Verification code must contain only digits.");
    }
}