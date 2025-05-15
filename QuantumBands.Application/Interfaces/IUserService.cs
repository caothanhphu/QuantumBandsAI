// QuantumBands.Application/Interfaces/IUserService.cs
using QuantumBands.Application.Features.Authentication; // For UserProfileDto
using QuantumBands.Application.Features.Users.Commands.UpdateProfile; // For UpdateUserProfileRequest
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;
using QuantumBands.Application.Features.Users.Commands.ChangePassword;
using QuantumBands.Application.Features.Users.Commands.Setup2FA;
using QuantumBands.Application.Features.Users.Commands.Enable2FA;
using QuantumBands.Application.Features.Users.Commands.Verify2FA;
using QuantumBands.Application.Features.Users.Commands.Disable2FA;

namespace QuantumBands.Application.Interfaces;

public interface IUserService
{
    Task<(UserProfileDto? Profile, string? ErrorMessage)> GetUserProfileAsync(ClaimsPrincipal currentUser, CancellationToken cancellationToken = default);
    Task<(UserProfileDto? Profile, string? ErrorMessage)> UpdateUserProfileAsync(ClaimsPrincipal currentUser, UpdateUserProfileRequest request, CancellationToken cancellationToken = default);
    // Các phương thức khác liên quan đến user có thể được thêm vào đây sau
    Task<(bool Success, string Message)> ChangePasswordAsync(ClaimsPrincipal currentUser, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    // Thêm các phương thức 2FA
    Task<(Setup2FAResponse? Response, string? ErrorMessage)> Setup2FAAsync(ClaimsPrincipal currentUser, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, IEnumerable<string>? RecoveryCodes)> Enable2FAAsync(ClaimsPrincipal currentUser, Enable2FARequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> Verify2FACodeAsync(ClaimsPrincipal currentUser, Verify2FARequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> Disable2FAAsync(ClaimsPrincipal currentUser, Disable2FARequest request, CancellationToken cancellationToken = default);

}