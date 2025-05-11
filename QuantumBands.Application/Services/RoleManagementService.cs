// QuantumBands.Application/Services/RoleManagementService.cs
using QuantumBands.Application.Interfaces;
using QuantumBands.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic; // Cho IEnumerable
using System.Threading.Tasks; // Cho Task
using System; // Cho TimeSpan
namespace QuantumBands.Application.Services;

public interface IRoleManagementService
{
    Task<IEnumerable<UserRole>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<UserRole?> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddRoleAsync(string roleName, CancellationToken cancellationToken = default); // Thêm CancellationToken
}

public class RoleManagementService : IRoleManagementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoleManagementService> _logger;
    private readonly ICachingService _cachingService; // Inject caching service
    private const string AllRolesCacheKey = "all_user_roles"; // Định nghĩa cache key

    public RoleManagementService(
        IUnitOfWork unitOfWork,
        ILogger<RoleManagementService> logger,
        ICachingService cachingService) // Thêm vào constructor
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cachingService = cachingService; // Gán
    }

    public async Task<IEnumerable<UserRole>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to fetch all roles.");

        // Sử dụng GetOrCreateAsync để đơn giản hóa logic
        var roles = await _cachingService.GetOrCreateAsync<IEnumerable<UserRole>>(
            AllRolesCacheKey,
            async () => {
                _logger.LogInformation("Cache miss for {CacheKey}. Fetching roles from database.", AllRolesCacheKey);
                return await _unitOfWork.UserRoles.GetAllAsync(); // Factory function để lấy dữ liệu từ DB
            },
            absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(30), // Cache trong 30 phút
            cancellationToken: cancellationToken
        );

        return roles ?? new List<UserRole>(); // Trả về danh sách rỗng nếu null
    }


    public async Task<UserRole?> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching role by id {RoleId}.", id);
        // Ví dụ: Cache từng role riêng lẻ (có thể không hiệu quả bằng cache cả list nếu list nhỏ)
        string roleByIdCacheKey = $"role_id_{id}";

        var role = await _cachingService.GetOrCreateAsync<UserRole?>(
            roleByIdCacheKey,
            async () => {
                _logger.LogInformation("Cache miss for {CacheKey}. Fetching role by ID from database.", roleByIdCacheKey);
                return await _unitOfWork.UserRoles.GetByIdAsync(id);
            },
            absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(30),
            cancellationToken: cancellationToken);

        return role;
    }


    public async Task AddRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to add new role: {RoleName}", roleName);
        var existingRole = await _unitOfWork.UserRoles.GetRoleByNameAsync(roleName);
        if (existingRole != null)
        {
            _logger.LogWarning("Role {RoleName} already exists.", roleName);
            throw new InvalidOperationException($"Role '{roleName}' already exists.");
        }

        var newRole = new UserRole { RoleName = roleName };
        await _unitOfWork.UserRoles.AddAsync(newRole);
        await _unitOfWork.CompleteAsync();
        _logger.LogInformation("Role {RoleName} added successfully with ID {RoleId}.", newRole.RoleName, newRole.RoleId);

        // --- Cache Invalidation ---
        // Xóa cache chứa danh sách tất cả các roles vì nó đã thay đổi
        _logger.LogInformation("Invalidating cache for key: {CacheKey}", AllRolesCacheKey);
        await _cachingService.RemoveAsync(AllRolesCacheKey, cancellationToken);
        // --- Kết thúc Cache Invalidation ---
    }

}