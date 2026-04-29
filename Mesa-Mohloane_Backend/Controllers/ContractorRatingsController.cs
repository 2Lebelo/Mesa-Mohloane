using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Services.Interfaces;
using System.Security.Claims;
using AppClaimTypes = Mesa_Mohloane_Backend.Helpers.ClaimTypes;

namespace Mesa_Mohloane_Backend.Controllers;

[ApiController]
[Route("api/contractor-ratings")]
[Authorize]
public class ContractorRatingsController(IContractorRatingService ratings) : ControllerBase
{
    private readonly IContractorRatingService _ratings = ratings;
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(AppClaimTypes.UserId)!);

    // Citizen: rate a contractor after job is closed
    [HttpPost]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> Rate([FromBody] ContractorRatingCreateDto dto)
    {
        var result = await _ratings.RateAsync(CurrentUserId, dto);
        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _ratings.GetByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpGet("assignments/{assignmentId:guid}")]
    public async Task<IActionResult> GetByAssignment(Guid assignmentId)
    {
        var result = await _ratings.GetByAssignmentAsync(assignmentId);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpGet("contractors/{contractorId:guid}")]
    public async Task<IActionResult> GetByContractor(
        Guid contractorId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _ratings.GetByContractorAsync(contractorId, page, pageSize);
        return Ok(result);
    }
}