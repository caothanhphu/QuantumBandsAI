// QuantumBands.Application/Interfaces/IUserRoleRepository.cs
using QuantumBands.Domain.Entities; // Giả sử UserRole entity ở đây

namespace QuantumBands.Application.Interfaces;

public interface IUserRoleRepository : IGenericRepository<UserRole>
{
    Task<UserRole?> GetRoleByNameAsync(string roleName);
}