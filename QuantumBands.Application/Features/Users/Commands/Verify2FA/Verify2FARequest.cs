// QuantumBands.Application/Features/Users/Commands/Verify2FA/Verify2FARequest.cs
namespace QuantumBands.Application.Features.Users.Commands.Verify2FA;

public class Verify2FARequest
{
    public required string VerificationCode { get; set; }
}