// QuantumBands.Application/Features/Admin/Users/Dtos/AdminUserViewDto.cs
namespace QuantumBands.Application.Features.Admin.Users.Dtos;

public class AdminUserViewDto
{
    public int UserId { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public string? FullName { get; set; }
    public required string RoleName { get; set; }
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal? WalletBalance { get; set; } // Nullable nếu user có thể không có ví
    public string? WalletCurrency { get; set; } // Nullable
}