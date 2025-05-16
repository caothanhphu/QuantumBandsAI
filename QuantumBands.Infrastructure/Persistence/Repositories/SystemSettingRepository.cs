// QuantumBands.Infrastructure/Persistence/Repositories/SystemSettingRepository.cs
using Microsoft.EntityFrameworkCore;
using QuantumBands.Application.Interfaces.Repositories;
using QuantumBands.Domain.Entities;
using QuantumBands.Infrastructure.Persistence.DataContext; // Your DbContext
using System.Threading.Tasks;
using System.Threading;

namespace QuantumBands.Infrastructure.Persistence.Repositories;

public class SystemSettingRepository : GenericRepository<SystemSetting>, ISystemSettingRepository
{
    public SystemSettingRepository(FinixAIDbContext context) : base(context)
    {
    }

    public async Task<SystemSetting?> GetSettingByKeyAsync(string settingKey, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.SettingKey == settingKey, cancellationToken);
    }

    public async Task<string?> GetSettingValueAsync(string settingKey, CancellationToken cancellationToken = default)
    {
        var setting = await _dbSet.AsNoTracking() // No tracking needed for read-only value
                                .FirstOrDefaultAsync(s => s.SettingKey == settingKey, cancellationToken);
        return setting?.SettingValue;
    }
}