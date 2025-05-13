// QuantumBands.Application/Interfaces/IJwtTokenGenerator.cs
using QuantumBands.Domain.Entities;
using System.Security.Claims; // Thêm using này

namespace QuantumBands.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateJwtToken(User user, string roleName);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token); // Phương thức mới
}