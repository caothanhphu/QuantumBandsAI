// QuantumBands.Application/Features/Authentication/Commands/RefreshToken/RefreshTokenRequest.cs
namespace QuantumBands.Application.Features.Authentication.Commands.RefreshToken;

public class RefreshTokenRequest
{
    public required string ExpiredJwtToken { get; set; }
    public required string RefreshToken { get; set; }
}