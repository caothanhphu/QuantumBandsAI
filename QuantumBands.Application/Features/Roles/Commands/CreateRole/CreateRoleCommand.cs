// QuantumBands.Application/Features/Roles/Commands/CreateRole/CreateRoleCommand.cs
namespace QuantumBands.Application.Features.Roles.Commands.CreateRole;

public class CreateRoleCommand
{
    public required string RoleName { get; set; }
    public string? Description { get; set; } // Thêm một trường tùy chọn để minh họa validation phức tạp hơn
}