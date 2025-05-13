// QuantumBands.Application/Interfaces/IAuthService.cs
using QuantumBands.Application.Features.Authentication.Commands.RegisterUser;
using QuantumBands.Application.Features.Authentication; // For UserDto
using System.Threading.Tasks;
using System.Threading;
using QuantumBands.Application.Features.Authentication.Commands.ResendVerificationEmail;
using QuantumBands.Application.Features.Authentication.Commands.VerifyEmail;
using QuantumBands.Application.Features.Authentication.Commands.Login; // Thêm using
using QuantumBands.Application.Features.Authentication.Commands.RefreshToken; // Thêm using

namespace QuantumBands.Application.Interfaces;

public interface IAuthService
{
    Task<(UserDto? User, string? ErrorMessage)> RegisterUserAsync(RegisterUserCommand command, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ResendVerificationEmailAsync(ResendVerificationEmailRequest request, CancellationToken cancellationToken = default);
    Task<(LoginResponse? Response, string? ErrorMessage)> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    // Thêm phương thức RefreshToken
    Task<(LoginResponse? Response, string? ErrorMessage)> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

}
