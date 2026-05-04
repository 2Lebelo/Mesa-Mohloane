using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Models.Entities;
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

    [HttpPost("assign")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Assign([FromBody] AssignContractorRequest request)
    {
        var result = await _assignments.AssignContractorAsync(
            request.IncidentId,
            request.TenderApplicationId,
            CurrentUserId);

        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/start")]
    [Authorize(Roles = "Contractor")]
    public async Task<IActionResult> Start(Guid id)
    {
        var result = await _assignments.StartWorkAsync(id, CurrentUserId);

        return result.Success
            ? Ok(result.Data)
            : BadRequest(new { error = result.Error });
    }

    // Existing JSON endpoint — preserved for old clients.
    [HttpPost("{id:guid}/complete")]
    [Consumes("application/json")]
    [Authorize(Roles = "Contractor")]
    public async Task<IActionResult> SubmitCompletionJson(
        Guid id,
        [FromBody] WorkCompletionCreateDto dto)
    {
        var result = await _assignments.SubmitWorkCompletionAsync(
            id,
            CurrentUserId,
            dto);

        return result.Success
            ? Ok(result.Data)
            : BadRequest(new { error = result.Error });
    }

    // New multipart endpoint — used by MVC form/file upload.
    [HttpPost("{id:guid}/complete")]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = "Contractor")]
    public async Task<IActionResult> SubmitCompletionWithEvidence(
        Guid id,
        [FromForm] WorkCompletionFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CompletionSummary))
            return BadRequest(new { error = "Completion summary is required." });

        if (dto.CompletionEvidenceFile is null || dto.CompletionEvidenceFile.Length == 0)
            return BadRequest(new { error = "Completion evidence photo is required." });

        var result = await _assignments.SubmitWorkCompletionWithEvidenceAsync(
            id,
            CurrentUserId,
            dto.CompletionSummary,
            dto.CompletionEvidenceFile);

        return result.Success
            ? Ok(result.Data)
            : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/acknowledge")]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> Acknowledge(Guid id)
    {
        var result = await _assignments.AcknowledgeCompletionAsync(id, CurrentUserId);

        return result.Success
            ? Ok(result.Data)
            : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _assignments.ApproveCompletionAsync(id, CurrentUserId);

        return result.Success
            ? Ok(result.Data)
            : BadRequest(new { error = result.Error });
    }

    [HttpGet("my")]
    [Authorize(Roles = "Contractor")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _assignments.GetByContractorAsync(CurrentUserId, page, pageSize);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Administrator,Inspector")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] AssignmentStatus? status = null)
    {
        var result = await _assignments.GetAllAsync(page, pageSize, status);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Administrator,Inspector,Contractor,Citizen")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _assignments.GetByIdAsync(id);

        return result.Success
            ? Ok(result.Data)
            : NotFound(new { error = result.Error });
    }

    [HttpGet("incidents/{incidentId:guid}")]
    [Authorize(Roles = "Administrator,Inspector,Citizen")]
    public async Task<IActionResult> GetByIncident(Guid incidentId)
    {
        var result = await _assignments.GetByIncidentAsync(incidentId);

        return result.Success
            ? Ok(result.Data)
            : NotFound(new { error = result.Error });
    }
}

public record AssignContractorRequest(Guid IncidentId, Guid TenderApplicationId);