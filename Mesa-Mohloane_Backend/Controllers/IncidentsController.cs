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
public class IncidentsController(IIncidentService incidents) : ControllerBase
{
    private readonly IIncidentService _incidents = incidents;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(AppClaimTypes.UserId)!);

    // Citizen: create incident with photos 
    /// <summary>
    /// Creates a new incident. Photos are uploaded via multipart/form-data.
    /// CitizenId is taken from the JWT token — not the request body.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Citizen")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
        [FromForm] IncidentCreateDto dto,
        [FromForm] IList<IFormFile> photos)
    {
        var result = await _incidents.CreateAsync(CurrentUserId, dto, photos ?? new List<IFormFile>());
        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { error = result.Error });
    }

    // Citizen: update own pending incident
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> Update(Guid id, [FromBody] IncidentUpdateDto dto)
    {
        var result = await _incidents.UpdateAsync(id, CurrentUserId, dto);
        return result.Success
            ? Ok(result.Data)
            : BadRequest(new { error = result.Error });
    }

    // Citizen: delete own pending incident
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _incidents.DeleteAsync(id, CurrentUserId);
        return result.Success
            ? NoContent()
            : BadRequest(new { error = result.Error });
    }

    // Citizen: add more photos to a pending incident
    [HttpPost("{id:guid}/photos")]
    [Authorize(Roles = "Citizen")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddPhotos(
        Guid id, [FromForm] IList<IFormFile> photos)
    {
        if (photos is null || photos.Count == 0)
            return BadRequest(new { error = "At least one photo is required." });

        var result = await _incidents.AddPhotosAsync(id, CurrentUserId, photos);
        return result.Success
            ? Ok(result.Data)
            : BadRequest(new { error = result.Error });
    }

    // Citizen: delete a single photo from a pending incident
    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> DeletePhoto(Guid id, Guid photoId)
    {
        var result = await _incidents.DeletePhotoAsync(id, photoId, CurrentUserId);
        return result.Success
            ? NoContent()
            : BadRequest(new { error = result.Error });
    }

    // Shared: get a single incident 
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Citizen,Administrator,Inspector")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _incidents.GetByIdAsync(id);
        return result.Success
            ? Ok(result.Data)
            : NotFound(new { error = result.Error });
    }

    // Citizen: own incidents only
    [HttpGet("my")]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _incidents.GetByCitizenAsync(CurrentUserId, page, pageSize);
        return Ok(result);
    }

    // Admin / Inspector: all incidents with filters
    [HttpGet]
    [Authorize(Roles = "Administrator,Inspector")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] IncidentStatus? status = null,
        [FromQuery] string? search = null)
    {
        var result = await _incidents.GetAllAsync(page, pageSize, status, search);
        return Ok(result);
    }

    // Admin: verify a submitted incident 
    [HttpPatch("{id:guid}/verify")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Verify(Guid id)
    {
        var result = await _incidents.VerifyAsync(id, CurrentUserId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    // Admin: publish verified incident so contractors can bid 
    [HttpPatch("{id:guid}/publish")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Publish(Guid id)
    {
        var result = await _incidents.PublishForBiddingAsync(id, CurrentUserId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }
}