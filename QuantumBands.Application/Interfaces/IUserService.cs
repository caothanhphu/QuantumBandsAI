// QuantumBands.Application/Interfaces/IUserService.cs
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.Users.Commands.UpdateUserRole;
using QuantumBands.Application.Features.Admin.Users.Commands.UpdateUserStatus;
using QuantumBands.Application.Features.Admin.Users.Dtos;
using QuantumBands.Application.Features.Admin.Users.Queries;
using QuantumBands.Application.Features.Authentication; // For UserProfileDto
using QuantumBands.Application.Features.Users.Commands.ChangePassword;
using QuantumBands.Application.Features.Users.Commands.Disable2FA;
using QuantumBands.Application.Features.Users.Commands.Enable2FA;
using QuantumBands.Application.Features.Users.Commands.Setup2FA;
using QuantumBands.Application.Features.Users.Commands.UpdateProfile; // For UpdateUserProfileRequest
using QuantumBands.Application.Features.Users.Commands.Verify2FA;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

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
    Task<PaginatedList<AdminUserViewDto>> GetAdminAllUsersAsync(GetAdminUsersQuery query, CancellationToken cancellationToken = default);
    Task<(AdminUserViewDto? User, string? ErrorMessage)> UpdateUserStatusByAdminAsync(int userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default);
    Task<(AdminUserViewDto? User, string? ErrorMessage)> UpdateUserRoleByAdminAsync(int userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default);

}