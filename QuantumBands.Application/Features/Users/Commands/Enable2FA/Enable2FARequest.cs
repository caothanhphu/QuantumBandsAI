// QuantumBands.Application/Features/Users/Commands/Enable2FA/Enable2FARequest.cs
namespace QuantumBands.Application.Features.Users.Commands.Enable2FA;

public class Enable2FARequest
{
    public required string VerificationCode { get; set; }
}