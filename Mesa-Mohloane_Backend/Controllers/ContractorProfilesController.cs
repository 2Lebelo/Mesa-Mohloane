using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Services.Interfaces;
using System.Security.Claims;
using AppClaimTypes = Mesa_Mohloane_Backend.Helpers.ClaimTypes;

namespace Mesa_Mohloane_Backend.Controllers;

[ApiController]
[Route("api/contractor-profiles")]
[Authorize]
public class ContractorProfilesController(IContractorProfileService profiles) : ControllerBase
{
    private readonly IContractorProfileService _profiles = profiles;
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(AppClaimTypes.UserId)!);

    // Contractor: register company profile after account creation
    [HttpPost]
    [Authorize(Roles = "Contractor")]
    public async Task<IActionResult> Create([FromBody] ContractorProfileCreateDto dto)
    {
        // Force the UserId to come from the JWT — never trust the body
        var dtoWithCorrectUser = dto with { UserId = CurrentUserId };
        var result = await _profiles.CreateAsync(dtoWithCorrectUser);
        return result.Success
            ? CreatedAtAction(nameof(GetMine), result.Data)
            : BadRequest(new { error = result.Error });
    }

    // Contractor: update own profile
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Contractor")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ContractorProfileUpdateDto dto)
    {
        var result = await _profiles.UpdateAsync(id, dto, CurrentUserId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    // Admin: approve a contractor profile
    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _profiles.ApproveAsync(id, CurrentUserId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    // Contractor: view own profile
    [HttpGet("me")]
    [Authorize(Roles = "Contractor")]
    public async Task<IActionResult> GetMine()
    {
        var result = await _profiles.GetByUserIdAsync(CurrentUserId);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    // Admin / Inspector: view any profile by id
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Administrator,Inspector")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _profiles.GetByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    // Public to authenticated users — contractors need to see each other's profiles
    [HttpGet]
    [Authorize(Roles = "Administrator,Inspector,Contractor")]
    public async Task<IActionResult> GetAllApproved(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _profiles.GetAllApprovedAsync(page, pageSize);
        return Ok(result);
    }
}