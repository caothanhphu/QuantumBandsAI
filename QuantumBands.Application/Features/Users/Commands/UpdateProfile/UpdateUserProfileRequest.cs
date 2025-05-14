// QuantumBands.Application/Features/Users/Commands/UpdateProfile/UpdateUserProfileRequest.cs
namespace QuantumBands.Application.Features.Users.Commands.UpdateProfile;

public class UpdateUserProfileRequest
{
    // Chỉ cho phép cập nhật FullName ở bước này
    // Bạn có thể thêm các trường khác sau nếu cần (ví dụ: PhoneNumber, Bio, etc.)
    // và đảm bảo chúng được phép cập nhật.
    public string? FullName { get; set; }
    // Các trường khác có thể được thêm vào đây sau này
    // public string? AvatarUrl { get; set; }
}