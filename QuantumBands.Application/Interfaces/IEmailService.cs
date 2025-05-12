// QuantumBands.Application/Interfaces/IEmailService.cs
using System.Threading.Tasks;

namespace QuantumBands.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlMessage, CancellationToken cancellationToken = default);
}
