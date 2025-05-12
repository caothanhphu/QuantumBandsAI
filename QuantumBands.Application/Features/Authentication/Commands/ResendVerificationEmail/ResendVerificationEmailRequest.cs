// QuantumBands.Application/Features/Authentication/Commands/ResendVerificationEmail/ResendVerificationEmailRequest.cs
namespace QuantumBands.Application.Features.Authentication.Commands.ResendVerificationEmail;

public class ResendVerificationEmailRequest
{
    public required string Email { get; set; }
}