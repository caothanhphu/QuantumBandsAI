// QuantumBands.Application/Features/Authentication/Commands/ForgotPassword/ForgotPasswordRequest.cs
namespace QuantumBands.Application.Features.Authentication.Commands.ForgotPassword;

public class ForgotPasswordRequest
{
    public required string Email { get; set; }
}