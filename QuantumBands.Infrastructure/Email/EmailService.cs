// QuantumBands.Infrastructure/Email/EmailService.cs
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MimeKit;
using QuantumBands.Application.Interfaces;
using System.Threading.Tasks;
using System.Threading;

namespace QuantumBands.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> emailSettingsOptions, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettingsOptions.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            emailMessage.To.Add(new MailboxAddress("", toEmail)); // Tên người nhận có thể để trống
            emailMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlMessage
            };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Đối với localhost và các server test không dùng SSL/TLS
            // Nếu UseTls là true, bạn sẽ cần SecureSocketOptions.StartTls hoặc StartTlsWhenAvailable
            // Dựa trên cấu hình SMTP_USE_TLS: "false"
            SecureSocketOptions socketOptions = _emailSettings.UseTls ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None;

            _logger.LogInformation("Attempting to connect to SMTP server {Host}:{Port} with UseTls={UseTls}",
                                   _emailSettings.SmtpHost, _emailSettings.SmtpPort, _emailSettings.UseTls);

            await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, socketOptions, cancellationToken);
            _logger.LogInformation("Connected to SMTP server.");

            if (!string.IsNullOrEmpty(_emailSettings.SmtpUsername) && !string.IsNullOrEmpty(_emailSettings.SmtpPassword))
            {
                _logger.LogInformation("Attempting to authenticate with username {Username}", _emailSettings.SmtpUsername);
                await client.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword, cancellationToken);
                _logger.LogInformation("Authenticated successfully.");
            }

            _logger.LogInformation("Sending email to {ToEmail} with subject {Subject}", toEmail, subject);
            await client.SendAsync(emailMessage, cancellationToken);
            _logger.LogInformation("Email sent successfully to {ToEmail}.", toEmail);

            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}. Subject: {Subject}", toEmail, subject);
            // Không throw exception ở đây để việc gửi mail lỗi không làm dừng quy trình đăng ký
            // nhưng có thể xem xét việc throw một custom exception nếu việc gửi mail là bắt buộc
        }
    }
}
