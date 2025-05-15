// QuantumBands.Application/Features/Users/Commands/Disable2FA/Disable2FARequest.cs
namespace QuantumBands.Application.Features.Users.Commands.Disable2FA;

public class Disable2FARequest
{
    // Để đơn giản, yêu cầu mã 2FA hiện tại để vô hiệu hóa.
    // Một lựa chọn khác là yêu cầu mật khẩu.
    public required string VerificationCode { get; set; }
}