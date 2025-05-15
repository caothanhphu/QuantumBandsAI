// QuantumBands.Application/Features/Users/Commands/Setup2FA/Setup2FAResponse.cs
namespace QuantumBands.Application.Features.Users.Commands.Setup2FA;

public class Setup2FAResponse
{
    public required string SharedKey { get; set; } // Base32 encoded secret key
    public required string AuthenticatorUri { get; set; }
}