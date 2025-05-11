// QuantumBands.API/Controllers/RolesController.cs
using Microsoft.AspNetCore.Mvc;
using QuantumBands.Application.Services; // Namespace của IRoleManagementService
using QuantumBands.Domain.Entities; // Namespace của UserRole

namespace QuantumBands.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRoleManagementService _roleManagementService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IRoleManagementService roleManagementService, ILogger<RolesController> logger)
    {
        _roleManagementService = roleManagementService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await _roleManagementService.GetAllRolesAsync();
        return Ok(roles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoleById(int id)
    {
        var role = await _roleManagementService.GetRoleByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }
        return Ok(role);
    }

    public class CreateRoleRequest
    {
        public required string RoleName { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.RoleName))
        {
            return BadRequest("Role name is required.");
        }
        try
        {
            await _roleManagementService.AddRoleAsync(request.RoleName);
            // Lấy lại role vừa tạo để trả về (hoặc trả về CreatedAtAction)
            var newRole = await _roleManagementService.GetAllRolesAsync(); // Tạm lấy hết để tìm
            var createdRole = newRole.FirstOrDefault(r => r.RoleName == request.RoleName);
            if (createdRole != null)
            {
                return CreatedAtAction(nameof(GetRoleById), new { id = createdRole.RoleId }, createdRole);
            }
            return Ok("Role created, but could not retrieve it immediately."); // Fallback
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role {RoleName}", request.RoleName);
            return StatusCode(500, "An error occurred while creating the role.");
        }
    }
}