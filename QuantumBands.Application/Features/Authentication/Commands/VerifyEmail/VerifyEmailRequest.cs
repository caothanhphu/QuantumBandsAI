// QuantumBands.Application/Features/Authentication/Commands/VerifyEmail/VerifyEmailRequest.cs
namespace QuantumBands.Application.Features.Authentication.Commands.VerifyEmail;

public class VerifyEmailRequest
{
    public int UserId { get; set; }
    public required string Token { get; set; }
}