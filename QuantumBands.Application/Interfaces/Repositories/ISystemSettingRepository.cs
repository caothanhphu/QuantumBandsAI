// QuantumBands.Application/Interfaces/Repositories/ISystemSettingRepository.cs
using QuantumBands.Domain.Entities; // Assuming SystemSetting entity is here
using System.Threading.Tasks;
using System.Threading;

namespace QuantumBands.Application.Interfaces.Repositories;

public interface ISystemSettingRepository : IGenericRepository<SystemSetting> // Kế thừa từ IGenericRepository nếu có
{
    Task<string?> GetSettingValueAsync(string settingKey, CancellationToken cancellationToken = default);
    Task<SystemSetting?> GetSettingByKeyAsync(string settingKey, CancellationToken cancellationToken = default);
}