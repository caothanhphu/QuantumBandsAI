// QuantumBands.Application/Services/UserService.cs
using QuantumBands.Application.Features.Authentication;
using QuantumBands.Application.Features.Users.Commands.UpdateProfile;
using QuantumBands.Application.Interfaces;
using QuantumBands.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using QuantumBands.Application.Features.Users.Commands.ChangePassword; // For Include

namespace QuantumBands.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    private int? GetUserIdFromPrincipal(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }
        var userIdString = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "uid" || c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        
        if (int.TryParse(userIdString, out var userId))
        {
            return userId;
        }
        return null;
    }

    public async Task<(UserProfileDto? Profile, string? ErrorMessage)> GetUserProfileAsync(ClaimsPrincipal currentUser, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromPrincipal(currentUser);
        if (!userId.HasValue)
        {
            _logger.LogWarning("GetUserProfileAsync: User is not authenticated or UserId claim is missing.");
            return (null, "User not authenticated or identity is invalid.");
        }

        _logger.LogInformation("Fetching profile for authenticated UserID: {UserId}", userId.Value);

        var user = await _unitOfWork.Users.Query()
                                    .Include(u => u.Role)
                                    .FirstOrDefaultAsync(u => u.UserId == userId.Value, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("GetUserProfileAsync: User with ID {UserId} not found in database, though authenticated.", userId.Value);
            return (null, "User profile not found.");
        }

        if (user.Role == null)
        {
            _logger.LogError("GetUserProfileAsync: User {Username} (ID: {UserId}) does not have a valid Role associated.", user.Username, user.UserId);
            return (null, "User role configuration error.");
        }

        var userProfileDto = new UserProfileDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            RoleName = user.Role.RoleName,
            IsEmailVerified = user.IsEmailVerified,
            TwoFactorEnabled = user.TwoFactorEnabled,
            CreatedAt = user.CreatedAt
        };

        return (userProfileDto, null);
    }

    public async Task<(UserProfileDto? Profile, string? ErrorMessage)> UpdateUserProfileAsync(ClaimsPrincipal currentUser, UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromPrincipal(currentUser);
        if (!userId.HasValue)
        {
            _logger.LogWarning("UpdateUserProfileAsync: User is not authenticated or UserId claim is missing.");
            return (null, "User not authenticated or identity is invalid.");
        }

        _logger.LogInformation("Attempting to update profile for UserID: {UserId}", userId.Value);

        var user = await _unitOfWork.Users.Query()
                                    .Include(u => u.Role) // Include Role để trả về UserProfileDto hoàn chỉnh
                                    .FirstOrDefaultAsync(u => u.UserId == userId.Value, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("UpdateUserProfileAsync: User with ID {UserId} not found in database.", userId.Value);
            return (null, "User not found.");
        }

        bool profileUpdated = false;

        // Chỉ cập nhật FullName nếu nó được cung cấp trong request và khác giá trị hiện tại
        // (string.IsNullOrEmpty cho phép xóa FullName nếu người dùng muốn)
        if (request.FullName != user.FullName)
        {
            user.FullName = request.FullName; // Có thể là null
            profileUpdated = true;
            _logger.LogDebug("UserID {UserId}: FullName updated to '{FullName}'.", userId.Value, request.FullName);
        }

        // Thêm logic cập nhật các trường khác ở đây nếu cần
        // Ví dụ:
        // if (request.AvatarUrl != null && request.AvatarUrl != user.AvatarUrl)
        // {
        //     user.AvatarUrl = request.AvatarUrl;
        //     profileUpdated = true;
        // }

        if (profileUpdated)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            try
            {
                await _unitOfWork.CompleteAsync(cancellationToken);
                _logger.LogInformation("Profile updated successfully for UserID: {UserId}", userId.Value);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while updating profile for UserID: {UserId}", userId.Value);
                return (null, "Could not update profile due to a concurrency conflict. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for UserID: {UserId}", userId.Value);
                return (null, "An error occurred while updating profile.");
            }
        }
        else
        {
            _logger.LogInformation("No changes detected for UserID: {UserId} profile.", userId.Value);
        }

        // Trả về thông tin hồ sơ đã cập nhật (hoặc chưa thay đổi)
        // Đảm bảo Role được nạp
        if (user.Role == null)
        {
            _logger.LogError("UpdateUserProfileAsync: User {Username} (ID: {UserId}) does not have a valid Role associated after update attempt.", user.Username, user.UserId);
            return (null, "User role configuration error after update attempt.");
        }

        var updatedProfileDto = new UserProfileDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            RoleName = user.Role.RoleName,
            IsEmailVerified = user.IsEmailVerified,
            TwoFactorEnabled = user.TwoFactorEnabled,
            CreatedAt = user.CreatedAt
        };

        return (updatedProfileDto, null);
    }
    public async Task<(bool Success, string Message)> ChangePasswordAsync(ClaimsPrincipal currentUser, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromPrincipal(currentUser);
        if (!userId.HasValue)
        {
            _logger.LogWarning("ChangePasswordAsync: User is not authenticated or UserId claim is missing.");
            return (false, "User not authenticated or identity is invalid.");
        }

        _logger.LogInformation("Attempting to change password for UserID: {UserId}", userId.Value);

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value); // Không cần Include Role ở đây
        if (user == null)
        {
            _logger.LogWarning("ChangePasswordAsync: User with ID {UserId} not found in database.", userId.Value);
            return (false, "User not found."); // Hoặc "Invalid operation"
        }

        // 1. Xác minh mật khẩu hiện tại
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("ChangePasswordAsync: Invalid current password for UserID: {UserId}", userId.Value);
            return (false, "Incorrect current password.");
        }

        // (Validator đã kiểm tra NewPassword và ConfirmNewPassword khớp nhau,
        // và NewPassword khác CurrentPassword)

        // 2. Hash mật khẩu mới
        string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        // 3. Cập nhật mật khẩu và thời gian
        user.PasswordHash = newPasswordHash;
        user.UpdatedAt = DateTime.UtcNow;
        // (Tùy chọn) Có thể muốn cập nhật RefreshToken ở đây để vô hiệu hóa các session cũ,
        // hoặc thêm một trường như 'PasswordChangedAt' để kiểm tra khi validate JWT.
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;

        _unitOfWork.Users.Update(user);
        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Password changed successfully for UserID: {UserId}", userId.Value);
            return (true, "Password changed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for UserID: {UserId}", userId.Value);
            return (false, "An error occurred while changing password.");
        }
    }
}