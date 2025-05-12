// QuantumBands.Application/Features/Authentication/LoginResponse.cs
namespace QuantumBands.Application.Features.Authentication;

public class LoginResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Tên vai trò
    public string JwtToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiry { get; set; }
}