// QuantumBands.API/Controllers/RolesController.cs
using Microsoft.AspNetCore.Mvc;
using QuantumBands.Application.Services; // Namespace của IRoleManagementService
using QuantumBands.Application.Features.Roles.Commands.CreateRole;
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

    [HttpPost]
    // Sử dụng [FromBody] để ASP.NET Core biết rằng nó nên đọc CreateRoleCommand từ body của request
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command)
    {
        // FluentValidation.AspNetCore sẽ tự động validate 'command'
        // Nếu không hợp lệ, ModelState.IsValid sẽ là false
        // và [ApiController] attribute sẽ tự động trả về 400 Bad Request
        // nên bạn không cần kiểm tra ModelState.IsValid ở đây một cách tường minh nữa.

        // Tuy nhiên, nếu bạn *không* dùng [ApiController] hoặc muốn xử lý tùy chỉnh, bạn sẽ kiểm tra:
        // if (!ModelState.IsValid)
        // {
        //     return BadRequest(ModelState);
        // }

        try
        {
            // Truyền toàn bộ command hoặc các thuộc tính cần thiết cho service
            // Giả sử RoleManagementService được cập nhật để nhận Description
            await _roleManagementService.AddRoleAsync(command.RoleName); // Cập nhật service nếu cần nhận thêm Description

            var newRole = (await _roleManagementService.GetAllRolesAsync())
                            .FirstOrDefault(r => r.RoleName == command.RoleName);

            if (newRole != null)
            {
                return CreatedAtAction(nameof(GetRoleById), new { id = newRole.RoleId }, newRole);
            }
            _logger.LogWarning("Role {RoleName} was created but could not be retrieved immediately for CreatedAtAction response.", command.RoleName);
            return Ok(new { Message = $"Role '{command.RoleName}' created successfully." }); // Fallback
        }
        catch (InvalidOperationException ex) // Ví dụ: Role đã tồn tại
        {
            _logger.LogWarning(ex, "Conflict creating role {RoleName}", command.RoleName);
            return Conflict(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role {RoleName}", command.RoleName);
            return StatusCode(500, new { Message = "An error occurred while creating the role." });
        }
    }
}