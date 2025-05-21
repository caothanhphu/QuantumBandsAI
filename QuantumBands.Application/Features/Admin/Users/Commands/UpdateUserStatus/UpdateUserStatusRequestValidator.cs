// QuantumBands.Application/Features/Admin/Users/Commands/UpdateUserStatus/UpdateUserStatusRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Admin.Users.Commands.UpdateUserStatus;

public class UpdateUserStatusRequestValidator : AbstractValidator<UpdateUserStatusRequest>
{
    public UpdateUserStatusRequestValidator()
    {
        // Không có rule cụ thể cho một boolean đơn lẻ,
        // nhưng bạn có thể thêm nếu có logic phức tạp hơn.
        // Ví dụ: RuleFor(x => x.IsActive).NotNull(); // Nếu bạn muốn nó luôn được cung cấp
    }
}