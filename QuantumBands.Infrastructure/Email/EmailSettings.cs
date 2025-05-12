// QuantumBands.Infrastructure/Email/EmailSettings.cs
namespace QuantumBands.Infrastructure.Email;

public class EmailSettings
{
    public const string SectionName = "EmailSettings"; // Tên section trong appsettings

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool UseTls { get; set; } = false; // Mặc định là false theo yêu cầu
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "FinixAI Platform"; // Tên người gửi mặc định
}
