// QuantumBands.API/Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace QuantumBands.API.Controllers;

[Authorize(Roles = "Admin")] // Chỉ Admin mới truy cập được controller này
[ApiController]
[Route("api/v1/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ILogger<AdminController> _logger;

    public AdminController(ILogger<AdminController> logger)
    {
        _logger = logger;
    }

    [HttpGet("dashboard-summary")]
    public IActionResult GetDashboardSummary()
    {
        var adminUsername = User.FindFirstValue(ClaimTypes.Name) ?? "Admin User";
        _logger.LogInformation("Admin {AdminUsername} accessed dashboard summary.", adminUsername);
        return Ok(new { Message = $"Welcome to the Admin Dashboard, {adminUsername}!", TotalUsers = 150, PendingApprovals = 5 });
    }

    [HttpPost("system-maintenance")]
    public IActionResult PerformMaintenance()
    {
        _logger.LogInformation("Admin performing system maintenance.");
        // Logic thực hiện bảo trì hệ thống
        return Ok(new { Message = "System maintenance initiated successfully by Admin." });
    }
}