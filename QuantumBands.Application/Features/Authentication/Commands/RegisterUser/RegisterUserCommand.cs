// QuantumBands.Application/Features/Authentication/Commands/RegisterUser/RegisterUserCommand.cs
namespace QuantumBands.Application.Features.Authentication.Commands.RegisterUser;

public class RegisterUserCommand
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string? FullName { get; set; }
}
