// QuantumBands.Application/Features/Authentication/UserProfileDto.cs
namespace QuantumBands.Application.Features.Authentication;

public class UserProfileDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string RoleName { get; set; } = string.Empty; // Tên vai trò
    public bool IsEmailVerified { get; set; }
    public bool TwoFactorEnabled { get; set; } // Sẽ lấy từ User entity
    public DateTime CreatedAt { get; set; }
}