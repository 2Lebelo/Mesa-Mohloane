using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Services.Interfaces;
using System.Security.Claims;
using AppClaimTypes = Mesa_Mohloane_Backend.Helpers.ClaimTypes;

namespace Mesa_Mohloane_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentsController(IAssignmentService assignments) : ControllerBase
{
    private readonly IAssignmentService _assignments = assignments;
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(AppClaimTypes.UserId)!);

    // Admin: assign the winning contractor to an incident
    [HttpPost("assign")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Assign([FromBody] AssignContractorRequest request)
    {
        var result = await _assignments.AssignContractorAsync(
            request.IncidentId, request.TenderApplicationId, CurrentUserId);
        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { error = result.Error });
    }

    // Contractor: start work
    [HttpPatch("{id:guid}/start")]
    [Authorize(Roles = "Contractor")]
    public async Task<IActionResult> Start(Guid id)
    {
        var result = await _assignments.StartWorkAsync(id, CurrentUserId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    // Contractor: submit work completion report
    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Contractor")]
    public async Task<IActionResult> SubmitCompletion(
        Guid id, [FromBody] WorkCompletionCreateDto dto)
    {
        var result = await _assignments.SubmitWorkCompletionAsync(id, CurrentUserId, dto);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    // Citizen: acknowledge that work is done satisfactorily
    [HttpPatch("{id:guid}/acknowledge")]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> Acknowledge(Guid id)
    {
        var result = await _assignments.AcknowledgeCompletionAsync(id, CurrentUserId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    // Admin: approve completion and trigger payment eligibility
    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _assignments.ApproveCompletionAsync(id, CurrentUserId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Administrator,Inspector,Contractor,Citizen")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _assignments.GetByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpGet("incidents/{incidentId:guid}")]
    [Authorize(Roles = "Administrator,Inspector,Citizen")]
    public async Task<IActionResult> GetByIncident(Guid incidentId)
    {
        var result = await _assignments.GetByIncidentAsync(incidentId);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }
}

// Inline request record — avoids creating a separate DTO file for a simple admin action
public record AssignContractorRequest(Guid IncidentId, Guid TenderApplicationId);