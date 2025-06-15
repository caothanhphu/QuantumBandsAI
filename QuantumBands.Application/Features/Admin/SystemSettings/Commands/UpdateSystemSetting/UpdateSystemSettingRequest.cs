namespace QuantumBands.Application.Features.Admin.SystemSettings.Commands.UpdateSystemSetting
{
    public class UpdateSystemSettingRequest
    {
        public string SettingValue { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsEditableByAdmin { get; set; }
    }
}