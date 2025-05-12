// QuantumBands.Application/Features/Authentication/UserDto.cs
namespace QuantumBands.Application.Features.Authentication;

public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; }
}
