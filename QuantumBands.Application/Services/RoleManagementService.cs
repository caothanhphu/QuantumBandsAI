// QuantumBands.Application/Services/RoleManagementService.cs
using QuantumBands.Application.Interfaces;
using QuantumBands.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace QuantumBands.Application.Services;

public interface IRoleManagementService
{
    Task<IEnumerable<UserRole>> GetAllRolesAsync();
    Task<UserRole?> GetRoleByIdAsync(int id);
    Task AddRoleAsync(string roleName);
}

public class RoleManagementService : IRoleManagementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoleManagementService> _logger;

    public RoleManagementService(IUnitOfWork unitOfWork, ILogger<RoleManagementService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<UserRole>> GetAllRolesAsync()
    {
        _logger.LogInformation("Fetching all roles.");
        return await _unitOfWork.UserRoles.GetAllAsync();
    }

    public async Task<UserRole?> GetRoleByIdAsync(int id)
    {
        _logger.LogInformation("Fetching role by id {RoleId}.", id);
        return await _unitOfWork.UserRoles.GetByIdAsync(id);
    }

    public async Task AddRoleAsync(string roleName)
    {
        _logger.LogInformation("Attempting to add new role: {RoleName}", roleName);
        var existingRole = await _unitOfWork.UserRoles.GetRoleByNameAsync(roleName);
        if (existingRole != null)
        {
            _logger.LogWarning("Role {RoleName} already exists.", roleName);
            throw new InvalidOperationException($"Role '{roleName}' already exists.");
        }

        var newRole = new UserRole { RoleName = roleName }; // Namespace entity UserRole cần đúng
        await _unitOfWork.UserRoles.AddAsync(newRole);
        await _unitOfWork.CompleteAsync(); // Lưu thay đổi
        _logger.LogInformation("Role {RoleName} added successfully with ID {RoleId}.", newRole.RoleName, newRole.RoleId);
    }
}