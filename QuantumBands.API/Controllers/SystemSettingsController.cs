using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuantumBands.Application.Common.Models;
using QuantumBands.Application.Features.Admin.SystemSettings.Commands.CreateSystemSetting;
using QuantumBands.Application.Features.Admin.SystemSettings.Commands.DeleteSystemSetting;
using QuantumBands.Application.Features.Admin.SystemSettings.Commands.UpdateSystemSetting;
using QuantumBands.Application.Features.Admin.SystemSettings.Dtos;
using QuantumBands.Application.Features.Admin.SystemSettings.Queries;
using QuantumBands.Application.Interfaces;
using System.Security.Claims;
using FluentValidation;

namespace QuantumBands.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/v1/admin/system-settings")]
    public class SystemSettingsController : ControllerBase
    {
        private readonly ISystemSettingService _systemSettingService;
        private readonly IValidator<GetSystemSettingsQuery> _getSystemSettingsValidator;
        private readonly IValidator<GetSystemSettingByIdQuery> _getByIdValidator;
        private readonly IValidator<GetSystemSettingByKeyQuery> _getByKeyValidator;
        private readonly IValidator<CreateSystemSettingRequest> _createValidator;
        private readonly IValidator<UpdateSystemSettingRequest> _updateValidator;
        private readonly IValidator<DeleteSystemSettingRequest> _deleteValidator;
        private readonly ILogger<SystemSettingsController> _logger;

        public SystemSettingsController(
            ISystemSettingService systemSettingService,
            IValidator<GetSystemSettingsQuery> getSystemSettingsValidator,
            IValidator<GetSystemSettingByIdQuery> getByIdValidator,
            IValidator<GetSystemSettingByKeyQuery> getByKeyValidator,
            IValidator<CreateSystemSettingRequest> createValidator,
            IValidator<UpdateSystemSettingRequest> updateValidator,
            IValidator<DeleteSystemSettingRequest> deleteValidator,
            ILogger<SystemSettingsController> logger)
        {
            _systemSettingService = systemSettingService;
            _getSystemSettingsValidator = getSystemSettingsValidator;
            _getByIdValidator = getByIdValidator;
            _getByKeyValidator = getByKeyValidator;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _deleteValidator = deleteValidator;
            _logger = logger;
        }

        /// <summary>
        /// Get all system settings with pagination and filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedList<SystemSettingDto>>> GetSystemSettings(
            [FromQuery] GetSystemSettingsQuery query, CancellationToken cancellationToken = default)
        {
            var validationResult = await _getSystemSettingsValidator.ValidateAsync(query, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var (settings, error) = await _systemSettingService.GetSystemSettingsAsync(query, cancellationToken);

            if (error != null)
            {
                _logger.LogError("Error retrieving system settings: {Error}", error);
                return StatusCode(500, new { message = error });
            }

            return Ok(settings);
        }

        /// <summary>
        /// Get a system setting by ID
        /// </summary>
        [HttpGet("{settingId:int}")]
        public async Task<ActionResult<SystemSettingDto>> GetSystemSettingById(
            int settingId, CancellationToken cancellationToken = default)
        {
            var query = new GetSystemSettingByIdQuery { SettingId = settingId };
            var validationResult = await _getByIdValidator.ValidateAsync(query, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var (setting, error) = await _systemSettingService.GetSystemSettingByIdAsync(settingId, cancellationToken);

            if (error != null)
            {
                if (error.Contains("not found"))
                {
                    return NotFound(new { message = error });
                }
                _logger.LogError("Error retrieving system setting by ID {SettingId}: {Error}", settingId, error);
                return StatusCode(500, new { message = error });
            }

            return Ok(setting);
        }

        /// <summary>
        /// Get a system setting by key
        /// </summary>
        [HttpGet("key/{settingKey}")]
        public async Task<ActionResult<SystemSettingDto>> GetSystemSettingByKey(
            string settingKey, CancellationToken cancellationToken = default)
        {
            var query = new GetSystemSettingByKeyQuery { SettingKey = settingKey };
            var validationResult = await _getByKeyValidator.ValidateAsync(query, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var (setting, error) = await _systemSettingService.GetSystemSettingByKeyAsync(settingKey, cancellationToken);

            if (error != null)
            {
                if (error.Contains("not found"))
                {
                    return NotFound(new { message = error });
                }
                _logger.LogError("Error retrieving system setting by key {SettingKey}: {Error}", settingKey, error);
                return StatusCode(500, new { message = error });
            }

            return Ok(setting);
        }

        /// <summary>
        /// Create a new system setting
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SystemSettingDto>> CreateSystemSetting(
            [FromBody] CreateSystemSettingRequest request, CancellationToken cancellationToken = default)
        {
            var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var currentUserId = GetCurrentUserId();
            var (setting, error) = await _systemSettingService.CreateSystemSettingAsync(request, currentUserId, cancellationToken);

            if (error != null)
            {
                if (error.Contains("already exists") || error.Contains("not valid"))
                {
                    return Conflict(new { message = error });
                }
                _logger.LogError("Error creating system setting: {Error}", error);
                return StatusCode(500, new { message = error });
            }

            return CreatedAtAction(nameof(GetSystemSettingById), new { settingId = setting!.SettingId }, setting);
        }

        /// <summary>
        /// Update an existing system setting
        /// </summary>
        [HttpPut("{settingId:int}")]
        public async Task<ActionResult<SystemSettingDto>> UpdateSystemSetting(
            int settingId, [FromBody] UpdateSystemSettingRequest request, CancellationToken cancellationToken = default)
        {
            var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var currentUserId = GetCurrentUserId();
            var (setting, error) = await _systemSettingService.UpdateSystemSettingAsync(settingId, request, currentUserId, cancellationToken);

            if (error != null)
            {
                if (error.Contains("not found"))
                {
                    return NotFound(new { message = error });
                }
                if (error.Contains("not editable") || error.Contains("not valid"))
                {
                    return BadRequest(new { message = error });
                }
                _logger.LogError("Error updating system setting {SettingId}: {Error}", settingId, error);
                return StatusCode(500, new { message = error });
            }

            return Ok(setting);
        }

        /// <summary>
        /// Delete a system setting
        /// </summary>
        [HttpDelete("{settingId:int}")]
        public async Task<ActionResult> DeleteSystemSetting(
            int settingId, CancellationToken cancellationToken = default)
        {
            var request = new DeleteSystemSettingRequest { SettingId = settingId };
            var validationResult = await _deleteValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var (success, error) = await _systemSettingService.DeleteSystemSettingAsync(settingId, cancellationToken);

            if (error != null)
            {
                if (error.Contains("not found"))
                {
                    return NotFound(new { message = error });
                }
                if (error.Contains("cannot be deleted"))
                {
                    return BadRequest(new { message = error });
                }
                _logger.LogError("Error deleting system setting {SettingId}: {Error}", settingId, error);
                return StatusCode(500, new { message = error });
            }

            return NoContent();
        }

        /// <summary>
        /// Get the value of a specific system setting by key (utility endpoint)
        /// </summary>
        [HttpGet("value/{settingKey}")]
        public async Task<ActionResult<string>> GetSettingValue(
            string settingKey, CancellationToken cancellationToken = default)
        {
            var query = new GetSystemSettingByKeyQuery { SettingKey = settingKey };
            var validationResult = await _getByKeyValidator.ValidateAsync(query, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var (value, error) = await _systemSettingService.GetSettingValueAsync(settingKey, cancellationToken);

            if (error != null)
            {
                _logger.LogError("Error retrieving setting value for key {SettingKey}: {Error}", settingKey, error);
                return StatusCode(500, new { message = error });
            }

            if (value == null)
            {
                return NotFound(new { message = "Setting not found" });
            }

            return Ok(new { settingKey, value });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("Unable to determine current user ID");
        }
    }
}