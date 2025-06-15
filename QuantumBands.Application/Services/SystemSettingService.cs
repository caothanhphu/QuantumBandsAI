using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.SystemSettings.Commands.CreateSystemSetting;
using QuantumBands.Application.Features.Admin.SystemSettings.Commands.UpdateSystemSetting;
using QuantumBands.Application.Features.Admin.SystemSettings.Dtos;
using QuantumBands.Application.Features.Admin.SystemSettings.Queries;
using QuantumBands.Application.Interfaces;
using QuantumBands.Application.Interfaces.Repositories;
using QuantumBands.Domain.Entities;
using System.Globalization;

namespace QuantumBands.Application.Services
{
    public class SystemSettingService : ISystemSettingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SystemSettingService> _logger;

        public SystemSettingService(IUnitOfWork unitOfWork, ILogger<SystemSettingService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<(PaginatedList<SystemSettingDto> settings, string? error)> GetSystemSettingsAsync(
            GetSystemSettingsQuery query, CancellationToken cancellationToken = default)
        {
            try
            {
                var settingsQuery = _unitOfWork.SystemSettings.Query()
                    .Include(s => s.UpdatedByUser)
                    .AsNoTracking();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(query.SearchTerm))
                {
                    var searchTerm = query.SearchTerm.ToLower();
                    settingsQuery = settingsQuery.Where(s => 
                        s.SettingKey.ToLower().Contains(searchTerm) ||
                        (s.Description != null && s.Description.ToLower().Contains(searchTerm)));
                }

                // Apply filters
                if (query.IsEditableByAdmin.HasValue)
                {
                    settingsQuery = settingsQuery.Where(s => s.IsEditableByAdmin == query.IsEditableByAdmin.Value);
                }

                if (!string.IsNullOrWhiteSpace(query.DataType))
                {
                    settingsQuery = settingsQuery.Where(s => s.SettingDataType.ToLower() == query.DataType.ToLower());
                }

                // Apply sorting
                settingsQuery = query.SortBy.ToLowerInvariant() switch
                {
                    "settingkey" => query.SortOrder.ToLowerInvariant() == "desc" 
                        ? settingsQuery.OrderByDescending(s => s.SettingKey)
                        : settingsQuery.OrderBy(s => s.SettingKey),
                    "settingdatatype" => query.SortOrder.ToLowerInvariant() == "desc"
                        ? settingsQuery.OrderByDescending(s => s.SettingDataType)
                        : settingsQuery.OrderBy(s => s.SettingDataType),
                    "lastupdatedat" => query.SortOrder.ToLowerInvariant() == "desc"
                        ? settingsQuery.OrderByDescending(s => s.LastUpdatedAt)
                        : settingsQuery.OrderBy(s => s.LastUpdatedAt),
                    "iseditablebyadmin" => query.SortOrder.ToLowerInvariant() == "desc"
                        ? settingsQuery.OrderByDescending(s => s.IsEditableByAdmin)
                        : settingsQuery.OrderBy(s => s.IsEditableByAdmin),
                    _ => settingsQuery.OrderBy(s => s.SettingKey)
                };

                var paginatedSettings = await PaginatedList<SystemSetting>.CreateAsync(
                    settingsQuery, query.ValidatedPageNumber, query.ValidatedPageSize, cancellationToken);

                var settingDtos = paginatedSettings.Items.Select(MapToDto).ToList();

                var result = new PaginatedList<SystemSettingDto>(
                    settingDtos, paginatedSettings.TotalCount, 
                    paginatedSettings.PageNumber, paginatedSettings.PageSize);

                return (result, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system settings");
                return (new PaginatedList<SystemSettingDto>(new List<SystemSettingDto>(), 0, 1, 10), 
                    "An error occurred while retrieving system settings");
            }
        }

        public async Task<(SystemSettingDto? setting, string? error)> GetSystemSettingByIdAsync(
            int settingId, CancellationToken cancellationToken = default)
        {
            try
            {
                var setting = await _unitOfWork.SystemSettings.Query()
                    .Include(s => s.UpdatedByUser)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SettingId == settingId, cancellationToken);

                if (setting == null)
                {
                    return (null, "System setting not found");
                }

                return (MapToDto(setting), null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system setting with ID {SettingId}", settingId);
                return (null, "An error occurred while retrieving the system setting");
            }
        }

        public async Task<(SystemSettingDto? setting, string? error)> GetSystemSettingByKeyAsync(
            string settingKey, CancellationToken cancellationToken = default)
        {
            try
            {
                var setting = await _unitOfWork.SystemSettings.Query()
                    .Include(s => s.UpdatedByUser)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SettingKey == settingKey, cancellationToken);

                if (setting == null)
                {
                    return (null, "System setting not found");
                }

                return (MapToDto(setting), null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system setting with key {SettingKey}", settingKey);
                return (null, "An error occurred while retrieving the system setting");
            }
        }

        public async Task<(SystemSettingDto? setting, string? error)> CreateSystemSettingAsync(
            CreateSystemSettingRequest request, int currentUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate data type and value
                if (!ValidateValueForDataType(request.SettingValue, request.SettingDataType))
                {
                    return (null, $"Setting value '{request.SettingValue}' is not valid for data type '{request.SettingDataType}'");
                }

                // Check if setting key already exists
                var existingSetting = await _unitOfWork.SystemSettings.GetSettingByKeyAsync(request.SettingKey, cancellationToken);
                if (existingSetting != null)
                {
                    return (null, "A setting with this key already exists");
                }

                var newSetting = new SystemSetting
                {
                    SettingKey = request.SettingKey,
                    SettingValue = request.SettingValue,
                    SettingDataType = request.SettingDataType.ToLowerInvariant(),
                    Description = request.Description,
                    IsEditableByAdmin = request.IsEditableByAdmin,
                    LastUpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = currentUserId
                };

                await _unitOfWork.SystemSettings.AddAsync(newSetting);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("System setting '{SettingKey}' created by user {UserId}", 
                    request.SettingKey, currentUserId);

                // Reload with user info
                var createdSetting = await _unitOfWork.SystemSettings.Query()
                    .Include(s => s.UpdatedByUser)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SettingId == newSetting.SettingId, cancellationToken);

                return (MapToDto(createdSetting!), null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system setting with key {SettingKey}", request.SettingKey);
                return (null, "An error occurred while creating the system setting");
            }
        }

        public async Task<(SystemSettingDto? setting, string? error)> UpdateSystemSettingAsync(
            int settingId, UpdateSystemSettingRequest request, int currentUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                var setting = await _unitOfWork.SystemSettings.GetByIdAsync(settingId);
                if (setting == null)
                {
                    return (null, "System setting not found");
                }

                if (!setting.IsEditableByAdmin)
                {
                    return (null, "This system setting is not editable");
                }

                // Validate data type and value
                if (!ValidateValueForDataType(request.SettingValue, setting.SettingDataType))
                {
                    return (null, $"Setting value '{request.SettingValue}' is not valid for data type '{setting.SettingDataType}'");
                }

                setting.SettingValue = request.SettingValue;
                setting.Description = request.Description;
                setting.IsEditableByAdmin = request.IsEditableByAdmin;
                setting.LastUpdatedAt = DateTime.UtcNow;
                setting.UpdatedByUserId = currentUserId;

                _unitOfWork.SystemSettings.Update(setting);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("System setting '{SettingKey}' updated by user {UserId}", 
                    setting.SettingKey, currentUserId);

                // Reload with user info
                var updatedSetting = await _unitOfWork.SystemSettings.Query()
                    .Include(s => s.UpdatedByUser)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SettingId == settingId, cancellationToken);

                return (MapToDto(updatedSetting!), null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system setting with ID {SettingId}", settingId);
                return (null, "An error occurred while updating the system setting");
            }
        }

        public async Task<(bool success, string? error)> DeleteSystemSettingAsync(
            int settingId, CancellationToken cancellationToken = default)
        {
            try
            {
                var setting = await _unitOfWork.SystemSettings.GetByIdAsync(settingId);
                if (setting == null)
                {
                    return (false, "System setting not found");
                }

                if (!setting.IsEditableByAdmin)
                {
                    return (false, "This system setting cannot be deleted");
                }

                _unitOfWork.SystemSettings.Remove(setting);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("System setting '{SettingKey}' deleted", setting.SettingKey);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting system setting with ID {SettingId}", settingId);
                return (false, "An error occurred while deleting the system setting");
            }
        }

        public async Task<(string? value, string? error)> GetSettingValueAsync(
            string settingKey, CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await _unitOfWork.SystemSettings.GetSettingValueAsync(settingKey, cancellationToken);
                return (value, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving setting value for key {SettingKey}", settingKey);
                return (null, "An error occurred while retrieving the setting value");
            }
        }

        private SystemSettingDto MapToDto(SystemSetting setting)
        {
            return new SystemSettingDto
            {
                SettingId = setting.SettingId,
                SettingKey = setting.SettingKey,
                SettingValue = setting.SettingValue,
                SettingDataType = setting.SettingDataType,
                Description = setting.Description,
                IsEditableByAdmin = setting.IsEditableByAdmin,
                LastUpdatedAt = setting.LastUpdatedAt,
                UpdatedByUserId = setting.UpdatedByUserId,
                UpdatedByUsername = setting.UpdatedByUser?.Username
            };
        }

        private bool ValidateValueForDataType(string settingValue, string dataType)
        {
            if (string.IsNullOrEmpty(dataType) || string.IsNullOrEmpty(settingValue))
                return false;

            return dataType.ToLowerInvariant() switch
            {
                "string" => true, // Any string is valid
                "int" => int.TryParse(settingValue, out _),
                "decimal" => decimal.TryParse(settingValue, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
                "boolean" => bool.TryParse(settingValue, out _) || 
                            settingValue.Equals("true", StringComparison.OrdinalIgnoreCase) || 
                            settingValue.Equals("false", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
    }
}