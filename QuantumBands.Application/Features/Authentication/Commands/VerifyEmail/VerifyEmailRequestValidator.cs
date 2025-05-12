// QuantumBands.Application/Features/Authentication/Commands/VerifyEmail/VerifyEmailRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Authentication.Commands.VerifyEmail;

public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("User ID must be valid.");
        RuleFor(x => x.Token).NotEmpty().WithMessage("Verification token is required.");
    }
}