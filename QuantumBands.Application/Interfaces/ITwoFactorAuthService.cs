// QuantumBands.Application/Interfaces/ITwoFactorAuthService.cs
using QuantumBands.Application.Features.Users.Commands.Setup2FA;
using System.Threading.Tasks;

namespace QuantumBands.Application.Interfaces;

public interface ITwoFactorAuthService
{
    (string sharedKey, string authenticatorUri) GenerateSetupInfo(string issuer, string userEmail, string userName);
    bool VerifyCode(string secretKey, string code);
    // Có thể thêm các phương thức khác nếu cần, ví dụ mã hóa/giải mã secret key
}