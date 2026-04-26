using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Services.Interfaces;

namespace Mesa_Mohloane_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]   // All user-management is admin-only
public class UsersController(IUserService users) : ControllerBase
{
    private readonly IUserService _users = users;

    // Collection endpoints

    /// <summary>
    /// Returns a paginated, filterable list of all users.
    /// Query params: page, pageSize, search, roleId, isActive
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? roleId = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _users.GetAllAsync(page, pageSize, search, roleId, isActive);
        return Ok(result);
    }

    /// <summary>Returns all users with the Contractor role.</summary>
    [HttpGet("contractors")]
    public async Task<IActionResult> GetContractors(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _users.GetContractorsAsync(page, pageSize, search);
        return Ok(result);
    }

    /// <summary>Returns all users with the Citizen role.</summary>
    [HttpGet("citizens")]
    public async Task<IActionResult> GetCitizens(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _users.GetCitizensAsync(page, pageSize, search);
        return Ok(result);
    }

    /// <summary>Returns all users with the Administrator role.</summary>
    [HttpGet("administrators")]
    public async Task<IActionResult> GetAdministrators(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _users.GetAdministratorsAsync(page, pageSize, search);
        return Ok(result);
    }

    /// <summary>Returns all users with the Auditor / Inspector role.</summary>
    [HttpGet("auditors")]
    public async Task<IActionResult> GetAuditors(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _users.GetAuditorsAsync(page, pageSize, search);
        return Ok(result);
    }

    // Single-resource endpoints

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _users.GetByIdAsync(id);
        return result.Success
            ? Ok(result.Data)
            : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a user account on behalf of another person — the primary use case
    /// here is provisioning Inspector / Auditor accounts, which cannot self-register.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
    {
        var result = await _users.CreateAsync(dto);
        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UserUpdateDto dto)
    {
        var result = await _users.UpdateAsync(id, dto);
        return result.Success
            ? Ok(result.Data)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Permanently deletes a user.
    /// Blocked if the user has active incidents or tender applications.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _users.DeleteAsync(id);
        return result.Success
            ? NoContent()
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Toggles a user's IsActive flag (activate / deactivate).
    /// Prefer this over hard-delete whenever the user has history.
    /// </summary>
    [HttpPatch("{id:guid}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var result = await _users.ToggleActiveAsync(id);
        return result.Success
            ? Ok(new { message = "User status updated successfully." })
            : BadRequest(new { error = result.Error });
    }
}