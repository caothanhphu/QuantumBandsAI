// QuantumBands.Application/Features/Authentication/Commands/ForgotPassword/ForgotPasswordRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Authentication.Commands.ForgotPassword;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");
    }
}