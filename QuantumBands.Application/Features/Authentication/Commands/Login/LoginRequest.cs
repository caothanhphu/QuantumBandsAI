// QuantumBands.Application/Features/Authentication/Commands/Login/LoginRequest.cs
namespace QuantumBands.Application.Features.Authentication.Commands.Login;

public class LoginRequest
{
    public required string UsernameOrEmail { get; set; }
    public required string Password { get; set; }
}