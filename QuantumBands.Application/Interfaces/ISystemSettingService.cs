using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.SystemSettings.Commands.CreateSystemSetting;
using QuantumBands.Application.Features.Admin.SystemSettings.Commands.UpdateSystemSetting;
using QuantumBands.Application.Features.Admin.SystemSettings.Dtos;
using QuantumBands.Application.Features.Admin.SystemSettings.Queries;

namespace QuantumBands.Application.Interfaces
{
    public interface ISystemSettingService
    {
        Task<(PaginatedList<SystemSettingDto> settings, string? error)> GetSystemSettingsAsync(
            GetSystemSettingsQuery query, CancellationToken cancellationToken = default);
        
        Task<(SystemSettingDto? setting, string? error)> GetSystemSettingByIdAsync(
            int settingId, CancellationToken cancellationToken = default);
        
        Task<(SystemSettingDto? setting, string? error)> GetSystemSettingByKeyAsync(
            string settingKey, CancellationToken cancellationToken = default);
        
        Task<(SystemSettingDto? setting, string? error)> CreateSystemSettingAsync(
            CreateSystemSettingRequest request, int currentUserId, CancellationToken cancellationToken = default);
        
        Task<(SystemSettingDto? setting, string? error)> UpdateSystemSettingAsync(
            int settingId, UpdateSystemSettingRequest request, int currentUserId, CancellationToken cancellationToken = default);
        
        Task<(bool success, string? error)> DeleteSystemSettingAsync(
            int settingId, CancellationToken cancellationToken = default);
        
        Task<(string? value, string? error)> GetSettingValueAsync(
            string settingKey, CancellationToken cancellationToken = default);
    }
}