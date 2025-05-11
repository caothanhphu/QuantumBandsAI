// QuantumBands.Infrastructure/Persistence/Repositories/UserRoleRepository.cs
using Microsoft.EntityFrameworkCore;
using QuantumBands.Application.Interfaces;
using QuantumBands.Domain.Entities;
using QuantumBands.Infrastructure.Persistence.DataContext;

namespace QuantumBands.Infrastructure.Persistence.Repositories;

public class UserRoleRepository : GenericRepository<UserRole>, IUserRoleRepository
{
    public UserRoleRepository(FinixAIDbContext context) : base(context)
    {
    }

    public async Task<UserRole?> GetRoleByNameAsync(string roleName)
    {
        return await _dbSet.FirstOrDefaultAsync(r => r.RoleName == roleName); // Giả sử UserRole có thuộc tính RoleName
    }
}