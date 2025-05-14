// QuantumBands.Application/Features/Users/Commands/UpdateProfile/UpdateUserProfileRequestValidator.cs
using FluentValidation;

namespace QuantumBands.Application.Features.Users.Commands.UpdateProfile;

public class UpdateUserProfileRequestValidator : AbstractValidator<UpdateUserProfileRequest>
{
    public UpdateUserProfileRequestValidator()
    {
        // FullName có thể là null (nếu người dùng không muốn cập nhật)
        // nhưng nếu được cung cấp, nó không nên quá dài.
        RuleFor(x => x.FullName)
            .MaximumLength(200).WithMessage("Full name cannot exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.FullName)); // Chỉ validate độ dài nếu FullName không rỗng

        // Bạn có thể thêm các quy tắc cho các trường khác ở đây nếu chúng được thêm vào DTO
    }
}