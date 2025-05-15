// QuantumBands.Infrastructure/Authentication/TwoFactorAuthService.cs
using OtpNet; // Using Otp.NET
using QuantumBands.Application.Interfaces;
using System;
using System.Text; // For Encoding

namespace QuantumBands.Infrastructure.Authentication;

public class TwoFactorAuthService : ITwoFactorAuthService
{
    private const int TotpStepSeconds = 30;
    private const int TotpSize = 6; // 6 digits

    public (string sharedKey, string authenticatorUri) GenerateSetupInfo(string issuer, string userEmail, string userName)
    {
        // Tạo một secret key ngẫu nhiên (phải là Base32 cho Otp.NET và nhiều authenticator apps)
        var keyBytes = KeyGeneration.GenerateRandomKey(20); // 20 bytes = 160 bits, kích thước phổ biến
        string base32SecretKey = Base32Encoding.ToString(keyBytes);

        // Tạo OTPAuth URI
        // Định dạng: otpauth://totp/LABEL?secret=SECRET&issuer=ISSUER&algorithm=SHA1&digits=6&period=30
        // Label thường là Issuer:Username hoặc Issuer:Email
        string label = $"{issuer}:{userName ?? userEmail}"; // Ưu tiên username nếu có

        // Otp.NET không có hàm tạo URI trực tiếp, nhưng chúng ta có thể tự xây dựng
        var authenticatorUri = $"otpauth://totp/{Uri.EscapeDataString(label)}?" +
                               $"secret={base32SecretKey}&" +
                               $"issuer={Uri.EscapeDataString(issuer)}&" +
                               $"digits={TotpSize}&" +
                               $"period={TotpStepSeconds}&" +
                               $"algorithm=SHA1"; // SHA1 là mặc định và phổ biến nhất

        return (base32SecretKey, authenticatorUri);
    }

    public bool VerifyCode(string base32SecretKey, string code)
    {
        if (string.IsNullOrWhiteSpace(base32SecretKey) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        try
        {
            var keyBytes = Base32Encoding.ToBytes(base32SecretKey);
            var totp = new Totp(keyBytes, step: TotpStepSeconds, mode: OtpHashMode.Sha1, totpSize: TotpSize);

            // VerifyTotp cho phép một khoảng thời gian chênh lệch (window)
            // để xử lý sự không đồng bộ nhỏ về thời gian giữa server và client.
            // Cửa sổ mặc định của Otp.NET là 1 (tức là kiểm tra code hiện tại, code trước đó, và code tiếp theo).
            // Bạn có thể tùy chỉnh VerificationWindow nếu cần.
            bool isValid = totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
            return isValid;
        }
        catch (Exception) // Ví dụ: Base32Encoding.ToBytes có thể throw lỗi nếu key không hợp lệ
        {
            return false;
        }
    }
}