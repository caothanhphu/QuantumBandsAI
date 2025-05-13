// QuantumBands.Infrastructure/Authentication/JwtTokenGenerator.cs
using QuantumBands.Application.Interfaces;
using QuantumBands.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using QuantumBands.Application.Features.Authentication;

namespace QuantumBands.Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenGenerator(IOptions<JwtSettings> jwtSettingsOptions)
    {
        // Inject JwtSettings đã được cấu hình từ appsettings
        _jwtSettings = jwtSettingsOptions.Value;
    }

    public string GenerateJwtToken(User user, string roleName)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        // Lấy khóa bí mật từ cấu hình và chuyển thành byte array
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

        // Định nghĩa các claims (thông tin chứa trong token)
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()), // Subject (thường là User ID)
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID (duy nhất cho mỗi token)
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("uid", user.UserId.ToString()), // Custom claim cho User ID (có thể trùng với Sub)
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), // Chuẩn ClaimTypes
            new Claim(ClaimTypes.Name, user.Username), // Chuẩn ClaimTypes
            new Claim(ClaimTypes.Email, user.Email), // Chuẩn ClaimTypes
            new Claim(ClaimTypes.Role, roleName) // Vai trò người dùng
            // Thêm các claims khác nếu cần
        };

        // Tạo Signing Credentials sử dụng khóa bí mật và thuật toán HS256
        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);

        // Tạo Security Token Descriptor (mô tả token)
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes), // Thời gian hết hạn
            Issuer = _jwtSettings.Issuer, // Người phát hành
            Audience = _jwtSettings.Audience, // Đối tượng sử dụng
            SigningCredentials = creds
        };

        // Tạo token
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // Chuyển token thành chuỗi string
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64]; // Tạo refresh token dài hơn
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
    // Triển khai phương thức mới
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true, // Nên validate audience
            ValidateIssuer = true,   // Nên validate issuer
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.Secret)),
            ValidateLifetime = false, // QUAN TRỌNG: Không validate thời gian sống của token
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            ClockSkew = TimeSpan.Zero // Không cho phép chênh lệch thời gian
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken securityToken;
        try
        {
            // Validate token (ngoại trừ lifetime) và lấy principal
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            // Kiểm tra xem token có đúng thuật toán không (tùy chọn nhưng tăng bảo mật)
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token: Algorithm mismatch.");

            return principal;
        }
        catch (SecurityTokenException stEx)
        {
            // Log lỗi nếu cần
            // _logger.LogWarning(stEx, "Invalid token received for refresh.");
            return null;
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "Error validating expired token.");
            return null;
        }
    }
}