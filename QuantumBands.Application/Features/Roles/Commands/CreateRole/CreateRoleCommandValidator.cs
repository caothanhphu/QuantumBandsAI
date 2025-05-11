// QuantumBands.Application/Features/Roles/Commands/CreateRole/CreateRoleCommandValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Roles.Commands.CreateRole;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role name is required.")
            .MinimumLength(3).WithMessage("Role name must be at least 3 characters long.")
            .MaximumLength(50).WithMessage("Role name cannot exceed 50 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(200).WithMessage("Description cannot exceed 200 characters.");

        // Thêm các rule khác nếu cần
        // Ví dụ: RuleFor(x => x.RoleName).MustAsync(BeUniqueRoleName).WithMessage("Role name must be unique.");
    }

    // Ví dụ một phương thức kiểm tra không đồng bộ (nếu cần truy cập DB)
    // private async Task<bool> BeUniqueRoleName(string roleName, CancellationToken cancellationToken)
    // {
    //     // Gọi service hoặc repository để kiểm tra tính duy nhất
    //     // return await _roleRepository.IsRoleNameUniqueAsync(roleName);
    //     return true; // Placeholder
    // }
}