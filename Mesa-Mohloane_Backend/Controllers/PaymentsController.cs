using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Services.Interfaces;
using System.Security.Claims;
using AppClaimTypes = Mesa_Mohloane_Backend.Helpers.ClaimTypes;

namespace Mesa_Mohloane_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class PaymentsController(IPaymentService payments) : ControllerBase
{
    private readonly IPaymentService _payments = payments;
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(AppClaimTypes.UserId)!);

    // Admin: initiate payment after all guards pass
    [HttpPost]
    public async Task<IActionResult> Initiate([FromBody] PaymentCreateDto dto)
    {
        var result = await _payments.InitiateAsync(CurrentUserId, dto);
        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _payments.ApproveAsync(id, CurrentUserId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/disburse")]
    public async Task<IActionResult> Disburse(Guid id)
    {
        var result = await _payments.DisburseAsync(id, CurrentUserId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/fail")]
    public async Task<IActionResult> MarkFailed(Guid id, [FromBody] FailPaymentRequest request)
    {
        var result = await _payments.MarkFailedAsync(id, CurrentUserId, request.Reason);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _payments.GetByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpGet("invoices/{invoiceId:guid}")]
    public async Task<IActionResult> GetByInvoice(Guid invoiceId)
    {
        var result = await _payments.GetByInvoiceAsync(invoiceId);
        return result.Success ? Ok(result.Data) : NotFound(new { error = result.Error });
    }
}

public record FailPaymentRequest(string Reason);