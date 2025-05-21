// QuantumBands.Application/Features/Admin/Users/Commands/UpdateUserRole/UpdateUserRoleRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Admin.Users.Commands.UpdateUserRole;

public class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(x => x.RoleId)
            .GreaterThan(0).WithMessage("Role ID must be a positive number.");
    }
}