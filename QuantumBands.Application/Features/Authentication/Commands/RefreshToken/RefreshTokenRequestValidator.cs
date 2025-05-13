// QuantumBands.Application/Features/Authentication/Commands/RefreshToken/RefreshTokenRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Authentication.Commands.RefreshToken;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.ExpiredJwtToken)
            .NotEmpty().WithMessage("Expired JWT token is required.");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}