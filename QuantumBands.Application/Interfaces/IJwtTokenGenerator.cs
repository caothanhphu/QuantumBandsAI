// QuantumBands.Application/Interfaces/IJwtTokenGenerator.cs
using QuantumBands.Domain.Entities; // For User entity

namespace QuantumBands.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateJwtToken(User user, string roleName);
    string GenerateRefreshToken();
}