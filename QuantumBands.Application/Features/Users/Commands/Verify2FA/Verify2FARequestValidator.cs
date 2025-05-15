// QuantumBands.Application/Features/Users/Commands/Verify2FA/Verify2FARequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Users.Commands.Verify2FA;

public class Verify2FARequestValidator : AbstractValidator<Verify2FARequest>
{
    public Verify2FARequestValidator()
    {
        RuleFor(x => x.VerificationCode)
            .NotEmpty().WithMessage("Verification code is required.")
            .Length(6).WithMessage("Verification code must be 6 digits.")
            .Matches("^[0-9]{6}$").WithMessage("Verification code must contain only digits.");
    }
}