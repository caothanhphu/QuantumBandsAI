using System;

namespace QuantumBands.Application.Features.Admin.SystemSettings.Dtos
{
    public class SystemSettingDto
    {
        public int SettingId { get; set; }
        public string SettingKey { get; set; } = null!;
        public string SettingValue { get; set; } = null!;
        public string SettingDataType { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsEditableByAdmin { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public int? UpdatedByUserId { get; set; }
        public string? UpdatedByUsername { get; set; }
    }
}