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
public class InvoicesController(IInvoiceService invoices) : ControllerBase
{
    private readonly IInvoiceService _invoices = invoices;
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(AppClaimTypes.UserId)!);

    // Contractor: submit final invoice
    [HttpPost]
    [Authorize(Roles = "Contractor")]
    public async Task<IActionResult> Submit([FromBody] InvoiceCreateDto dto)
    {
        var result = await _invoices.SubmitAsync(CurrentUserId, dto);
        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { error = result.Error });
    }

    // Admin: validate invoice (clears Submitted/Flagged → Validated)
    [HttpPatch("{id:guid}/validate")]
    [Authorize(Roles = "Administrator,Inspector")]
    public async Task<IActionResult> Validate(Guid id, [FromBody] ValidateInvoiceRequest request)
    {
        var result = await _invoices.ValidateAsync(id, CurrentUserId, request.Remarks);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    // Admin: approve validated invoice (enables payment)
    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _invoices.ApproveAsync(id, CurrentUserId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    // Admin: reject invoice
    [HttpPatch("{id:guid}/reject")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectInvoiceRequest request)
    {
        var result = await _invoices.RejectAsync(id, CurrentUserId, request.Reason);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    // Citizen: acknowledge approved invoice
    [HttpPatch("{id:guid}/acknowledge")]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> Acknowledge(Guid id)
    {
        var result = await _invoices.AcknowledgeAsync(id, CurrentUserId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Administrator,Inspector,Contractor,Citizen")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _invoices.GetByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpGet("assignments/{assignmentId:guid}")]
    [Authorize(Roles = "Administrator,Inspector,Contractor,Citizen")]
    public async Task<IActionResult> GetByAssignment(Guid assignmentId)
    {
        var result = await _invoices.GetByAssignmentAsync(assignmentId);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpGet("my")]
    [Authorize(Roles = "Contractor")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _invoices.GetByContractorAsync(CurrentUserId, page, pageSize);
        return Ok(result);
    }

    // Admin / Auditor: view all flagged invoices (variance > 10%)
    [HttpGet("flagged")]
    [Authorize(Roles = "Administrator,Inspector")]
    public async Task<IActionResult> GetFlagged(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _invoices.GetFlaggedAsync(page, pageSize);
        return Ok(result);
    }
}

public record ValidateInvoiceRequest(string? Remarks);
public record RejectInvoiceRequest(string Reason);