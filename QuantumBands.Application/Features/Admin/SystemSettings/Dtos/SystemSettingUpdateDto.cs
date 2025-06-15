namespace QuantumBands.Application.Features.Admin.SystemSettings.Dtos
{
    public class SystemSettingUpdateDto
    {
        public string SettingValue { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsEditableByAdmin { get; set; }
    }
}