// QuantumBands.Application/Features/Authentication/Commands/Login/LoginRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Authentication.Commands.Login;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UsernameOrEmail)
            .NotEmpty().WithMessage("Username or Email is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}