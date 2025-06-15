namespace QuantumBands.Application.Features.Admin.SystemSettings.Commands.CreateSystemSetting
{
    public class CreateSystemSettingRequest
    {
        public string SettingKey { get; set; } = null!;
        public string SettingValue { get; set; } = null!;
        public string SettingDataType { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsEditableByAdmin { get; set; } = true;
    }
}