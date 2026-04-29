using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Services.Interfaces;
using System.Security.Claims;
using AppClaimTypes = Mesa_Mohloane_Backend.Helpers.ClaimTypes;

namespace Mesa_Mohloane_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Route("api/tender-applications")]
[Authorize]
public class TenderApplicationsController(ITenderService tenders) : ControllerBase
{
    private readonly ITenderService _tenders = tenders;
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(AppClaimTypes.UserId)!);

    // Contractor: submit a tender
    [HttpPost]
    [Authorize(Roles = "Contractor")]
    public async Task<IActionResult> Submit([FromBody] TenderApplicationCreateDto dto)
    {
        var result = await _tenders.SubmitAsync(CurrentUserId, dto);
        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { error = result.Error });
    }

    // Contractor: update own submitted tender
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Contractor")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TenderApplicationUpdateDto dto)
    {
        var result = await _tenders.UpdateAsync(id, CurrentUserId, dto);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    // Contractor: withdraw own tender
    [HttpPatch("{id:guid}/withdraw")]
    [Authorize(Roles = "Contractor")]
    public async Task<IActionResult> Withdraw(Guid id)
    {
        var result = await _tenders.WithdrawAsync(id, CurrentUserId);
        return result.Success ? Ok(new { message = "Tender withdrawn." }) : BadRequest(new { error = result.Error });
    }

    // Admin: run the evaluation algorithm for all tenders on an incident
    [HttpPost("incidents/{incidentId:guid}/evaluate")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Evaluate(Guid incidentId)
    {
        var result = await _tenders.EvaluateAndRankAsync(incidentId, CurrentUserId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    // Admin / Inspector: view all ranked tenders for an incident
    [HttpGet("incidents/{incidentId:guid}")]
    [Authorize(Roles = "Administrator,Inspector")]
    public async Task<IActionResult> GetByIncident(Guid incidentId)
    {
        var result = await _tenders.GetByIncidentAsync(incidentId);
        return Ok(result);
    }

    // Contractor: view own tender history
    [HttpGet("my")]
    [Authorize(Roles = "Contractor")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _tenders.GetByContractorAsync(CurrentUserId, page, pageSize);
        return Ok(result);
    }

    // Shared: get single tender by id
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Administrator,Inspector,Contractor")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _tenders.GetByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }
}