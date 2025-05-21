// QuantumBands.Application/Services/UserService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // Để lấy Issuer từ config
using Microsoft.Extensions.Logging;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.Users.Commands.UpdateUserStatus;
using QuantumBands.Application.Features.Admin.Users.Commands.UpdateUserRole;
using QuantumBands.Application.Features.Admin.Users.Dtos;
using QuantumBands.Application.Features.Admin.Users.Queries;
using QuantumBands.Application.Features.Authentication;
using QuantumBands.Application.Features.Users.Commands.ChangePassword; // For Include
using QuantumBands.Application.Features.Users.Commands.Disable2FA;
using QuantumBands.Application.Features.Users.Commands.Enable2FA;
using QuantumBands.Application.Features.Users.Commands.Setup2FA;
using QuantumBands.Application.Features.Users.Commands.UpdateProfile;
using QuantumBands.Application.Features.Users.Commands.Verify2FA;
using QuantumBands.Application.Interfaces;
using QuantumBands.Domain.Entities;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;
    private readonly ITwoFactorAuthService _twoFactorAuthService; // Inject
    private readonly IConfiguration _configuration; // Inject

    public UserService(
        IUnitOfWork unitOfWork,
        ILogger<UserService> logger,
        ITwoFactorAuthService twoFactorAuthService, // Thêm vào constructor
        IConfiguration configuration) // Thêm vào constructor
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _twoFactorAuthService = twoFactorAuthService; // Gán
        _configuration = configuration; // Gán
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
    public async Task<(Setup2FAResponse? Response, string? ErrorMessage)> Setup2FAAsync(ClaimsPrincipal currentUser, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromPrincipal(currentUser);
        if (!userId.HasValue) return (null, "User not authenticated.");

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
        if (user == null) return (null, "User not found.");

        if (user.TwoFactorEnabled)
        {
            return (null, "Two-Factor Authentication is already enabled.");
        }

        string issuer = _configuration["JwtSettings:Issuer"] ?? "QuantumBandsAI"; // Lấy Issuer từ config hoặc dùng mặc định
        var (sharedKey, authenticatorUri) = _twoFactorAuthService.GenerateSetupInfo(issuer, user.Email, user.Username);

        // Lưu tạm sharedKey vào user để verify ở bước Enable.
        // Trong thực tế, bạn có thể muốn mã hóa key này trước khi lưu, ngay cả khi là tạm thời.
        // Hoặc chỉ lưu một phần hash của nó để xác nhận mà không lưu key gốc cho đến khi enable.
        // Để đơn giản, ví dụ này lưu trực tiếp (NHƯNG NÊN MÃ HÓA TRONG PRODUCTION)
        user.TwoFactorSecretKey = sharedKey; // Cần đảm bảo User entity có trường này
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("2FA setup initiated for UserID {UserId}. Shared key stored temporarily.", userId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save temporary 2FA secret for UserID {UserId}", userId.Value);
            return (null, "Error initiating 2FA setup.");
        }

        return (new Setup2FAResponse { SharedKey = sharedKey, AuthenticatorUri = authenticatorUri }, null);
    }

    public async Task<(bool Success, string Message, IEnumerable<string>? RecoveryCodes)> Enable2FAAsync(ClaimsPrincipal currentUser, Enable2FARequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromPrincipal(currentUser);
        if (!userId.HasValue) return (false, "User not authenticated.", null);

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
        if (user == null) return (false, "User not found.", null);

        if (user.TwoFactorEnabled) return (true, "2FA is already enabled.", null); // Đã bật rồi thì thôi

        if (string.IsNullOrEmpty(user.TwoFactorSecretKey))
        {
            return (false, "2FA setup process not initiated or secret key is missing. Please start setup again.", null);
        }

        bool isValidCode = _twoFactorAuthService.VerifyCode(user.TwoFactorSecretKey, request.VerificationCode);

        if (!isValidCode)
        {
            return (false, "Invalid verification code.", null);
        }

        // Mã hợp lệ, kích hoạt 2FA
        user.TwoFactorEnabled = true;
        // TwoFactorSecretKey đã được lưu ở bước setup, đảm bảo nó được mã hóa nếu cần
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("2FA enabled successfully for UserID {UserId}.", userId.Value);

            // TODO: Tạo và trả về Recovery Codes.
            // Việc tạo và lưu trữ recovery codes là một phần quan trọng nhưng phức tạp hơn.
            // Tạm thời trả về null.
            var recoveryCodes = new List<string> { "RECOVERY-CODE-1", "RECOVERY-CODE-2" }; // Placeholder
            return (true, "Two-Factor Authentication enabled successfully.", recoveryCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling 2FA for UserID {UserId}", userId.Value);
            return (false, "An error occurred while enabling 2FA.", null);
        }
    }

    public async Task<(bool Success, string Message)> Verify2FACodeAsync(ClaimsPrincipal currentUser, Verify2FARequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromPrincipal(currentUser);
        if (!userId.HasValue) return (false, "User not authenticated.");

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
        if (user == null) return (false, "User not found.");

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecretKey))
        {
            return (false, "2FA is not enabled for this account.");
        }

        bool isValidCode = _twoFactorAuthService.VerifyCode(user.TwoFactorSecretKey, request.VerificationCode);

        if (!isValidCode)
        {
            return (false, "Invalid 2FA verification code.");
        }

        _logger.LogInformation("2FA code verified successfully for UserID {UserId}", userId.Value);
        return (true, "2FA verification successful.");
    }

    public async Task<(bool Success, string Message)> Disable2FAAsync(ClaimsPrincipal currentUser, Disable2FARequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromPrincipal(currentUser);
        if (!userId.HasValue) return (false, "User not authenticated.");

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
        if (user == null) return (false, "User not found.");

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecretKey))
        {
            return (false, "2FA is not currently enabled for this account.");
        }

        // Xác minh mã 2FA hiện tại trước khi vô hiệu hóa
        bool isValidCode = _twoFactorAuthService.VerifyCode(user.TwoFactorSecretKey, request.VerificationCode);
        if (!isValidCode)
        {
            return (false, "Invalid verification code. Cannot disable 2FA.");
        }

        user.TwoFactorEnabled = false;
        user.TwoFactorSecretKey = null; // Xóa khóa bí mật
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("2FA disabled successfully for UserID {UserId}.", userId.Value);
            return (true, "Two-Factor Authentication disabled successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling 2FA for UserID {UserId}", userId.Value);
            return (false, "An error occurred while disabling 2FA.");
        }
    }
    public async Task<PaginatedList<AdminUserViewDto>> GetAdminAllUsersAsync(GetAdminUsersQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Admin fetching all users with query: {@Query}", query);

        var usersQuery = _unitOfWork.Users.Query()
                            .Include(u => u.Role) // To fetch RoleName
                            .Include(u => u.Wallet) // To fetch WalletBalance
                            .AsQueryable(); // Ensure the type remains IQueryable<User>

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string searchTermLower = query.SearchTerm.ToLower();
            usersQuery = usersQuery.Where(u =>
                (u.Username != null && u.Username.ToLower().Contains(searchTermLower)) ||
                (u.Email != null && u.Email.ToLower().Contains(searchTermLower)) ||
                (u.FullName != null && u.FullName.ToLower().Contains(searchTermLower))
            );
        }
        if (query.RoleId.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.RoleId == query.RoleId.Value);
        }
        if (query.IsActive.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.IsActive == query.IsActive.Value);
        }
        if (query.IsEmailVerified.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.IsEmailVerified == query.IsEmailVerified.Value);
        }
        if (query.DateFrom.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.CreatedAt >= query.DateFrom.Value.Date);
        }
        if (query.DateTo.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.CreatedAt < query.DateTo.Value.Date.AddDays(1));
        }

        // Áp dụng Sắp xếp
        bool isDescending = query.SortOrder?.ToLower() == "desc";
        Expression<Func<User, object>> orderByExpression;

        switch (query.SortBy?.ToLowerInvariant())
        {
            case "userid": orderByExpression = u => u.UserId; break;
            case "username": orderByExpression = u => u.Username; break;
            case "email": orderByExpression = u => u.Email; break;
            case "fullname": orderByExpression = u => u.FullName!; break; // Thêm ! nếu FullName có thể null
            case "rolename": orderByExpression = u => u.Role.RoleName; break;
            case "isactive": orderByExpression = u => u.IsActive; break;
            case "isemailverified": orderByExpression = u => u.IsEmailVerified; break;
            case "createdat": default: orderByExpression = u => u.CreatedAt; break;
        }

        usersQuery = isDescending
            ? usersQuery.OrderByDescending(orderByExpression)
            : usersQuery.OrderBy(orderByExpression);

        var paginatedUsers = await PaginatedList<User>.CreateAsync(
            usersQuery,
            query.ValidatedPageNumber,
            query.ValidatedPageSize,
            cancellationToken);

        var dtos = paginatedUsers.Items.Select(u => new AdminUserViewDto
        {
            UserId = u.UserId,
            Username = u.Username,
            Email = u.Email,
            FullName = u.FullName,
            RoleName = u.Role?.RoleName ?? "N/A", // Xử lý trường hợp Role có thể null
            IsActive = u.IsActive,
            IsEmailVerified = u.IsEmailVerified,
            CreatedAt = u.CreatedAt,
            WalletBalance = u.Wallet?.Balance, // Lấy balance, có thể null nếu không có wallet
            WalletCurrency = u.Wallet?.CurrencyCode // Lấy currency, có thể null
        }).ToList();

        return new PaginatedList<AdminUserViewDto>(
            dtos,
            paginatedUsers.TotalCount,
            paginatedUsers.PageNumber,
            paginatedUsers.PageSize);
    }

    public async Task<(AdminUserViewDto? User, string? ErrorMessage)> UpdateUserStatusByAdminAsync(int userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Admin attempting to update status for UserID: {UserId} to IsActive: {IsActive}", userId, request.IsActive);

        var user = await _unitOfWork.Users.Query()
                            .Include(u => u.Role)
                            .Include(u => u.Wallet)
                            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("UpdateUserStatusByAdminAsync: User with ID {UserId} not found.", userId);
            return (null, "User not found.");
        }

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Status updated successfully for UserID: {UserId}. New IsActive: {IsActive}", userId, request.IsActive);

            var updatedUserDto = new AdminUserViewDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                RoleName = user.Role?.RoleName ?? "N/A",
                IsActive = user.IsActive,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt,
                WalletBalance = user.Wallet?.Balance,
                WalletCurrency = user.Wallet?.CurrencyCode
            };
            return (updatedUserDto, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for UserID: {UserId}", userId);
            return (null, "An error occurred while updating user status.");
        }
    }

    public async Task<(AdminUserViewDto? User, string? ErrorMessage)> UpdateUserRoleByAdminAsync(int userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Admin attempting to update role for UserID: {UserId} to RoleID: {RoleId}", userId, request.RoleId);

        var user = await _unitOfWork.Users.Query()
                            .Include(u => u.Wallet) // Nạp Wallet để trả về DTO đầy đủ
                            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("UpdateUserRoleByAdminAsync: User with ID {UserId} not found.", userId);
            return (null, "User not found.");
        }

        var newRole = await _unitOfWork.UserRoles.GetByIdAsync(request.RoleId); // Giả sử UserRoles repository có GetByIdAsync
        if (newRole == null)
        {
            _logger.LogWarning("UpdateUserRoleByAdminAsync: Role with ID {RoleId} not found.", request.RoleId);
            return (null, "Invalid Role ID provided.");
        }

        user.RoleId = request.RoleId;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        try
        {
            await _unitOfWork.CompleteAsync(cancellationToken);
            _logger.LogInformation("Role updated successfully for UserID: {UserId}. New RoleID: {RoleId}", userId, request.RoleId);

            // Nạp lại Role để lấy RoleName cho DTO
            user.Role = newRole; // Gán trực tiếp đối tượng Role đã lấy được

            var updatedUserDto = new AdminUserViewDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                RoleName = user.Role.RoleName, // Bây giờ sẽ có RoleName
                IsActive = user.IsActive,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt,
                WalletBalance = user.Wallet?.Balance,
                WalletCurrency = user.Wallet?.CurrencyCode
            };
            return (updatedUserDto, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role for UserID: {UserId}", userId);
            return (null, "An error occurred while updating user role.");
        }
    }
}