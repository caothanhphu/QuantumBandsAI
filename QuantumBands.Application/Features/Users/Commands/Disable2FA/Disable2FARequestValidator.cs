// QuantumBands.Application/Features/Users/Commands/Disable2FA/Disable2FARequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Users.Commands.Disable2FA;

public class Disable2FARequestValidator : AbstractValidator<Disable2FARequest>
{
    public Disable2FARequestValidator()
    {
        RuleFor(x => x.VerificationCode)
            .NotEmpty().WithMessage("Verification code is required.")
            .Length(6).WithMessage("Verification code must be 6 digits.")
            .Matches("^[0-9]{6}$").WithMessage("Verification code must contain only digits.");
    }
}