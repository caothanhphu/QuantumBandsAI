// QuantumBands.Application/Interfaces/IUserService.cs
using QuantumBands.Application.Features.Authentication; // For UserProfileDto
using QuantumBands.Application.Features.Users.Commands.UpdateProfile; // For UpdateUserProfileRequest
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;
using QuantumBands.Application.Features.Users.Commands.ChangePassword;

namespace QuantumBands.Application.Interfaces;

public interface IUserService
{
    Task<(UserProfileDto? Profile, string? ErrorMessage)> GetUserProfileAsync(ClaimsPrincipal currentUser, CancellationToken cancellationToken = default);
    Task<(UserProfileDto? Profile, string? ErrorMessage)> UpdateUserProfileAsync(ClaimsPrincipal currentUser, UpdateUserProfileRequest request, CancellationToken cancellationToken = default);
    // Các phương thức khác liên quan đến user có thể được thêm vào đây sau
    Task<(bool Success, string Message)> ChangePasswordAsync(ClaimsPrincipal currentUser, ChangePasswordRequest request, CancellationToken cancellationToken = default);

}