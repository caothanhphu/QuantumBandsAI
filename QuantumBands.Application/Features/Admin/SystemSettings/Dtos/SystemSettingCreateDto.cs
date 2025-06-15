namespace QuantumBands.Application.Features.Admin.SystemSettings.Dtos
{
    public class SystemSettingCreateDto
    {
        public string SettingKey { get; set; } = null!;
        public string SettingValue { get; set; } = null!;
        public string SettingDataType { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsEditableByAdmin { get; set; } = true;
    }
}