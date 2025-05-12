// QuantumBands.Application/Features/Authentication/Commands/ResendVerificationEmail/ResendVerificationEmailRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Authentication.Commands.ResendVerificationEmail;

public class ResendVerificationEmailRequestValidator : AbstractValidator<ResendVerificationEmailRequest>
{
    public ResendVerificationEmailRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");
    }
}