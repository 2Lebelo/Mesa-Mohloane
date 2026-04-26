using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Services.Interfaces;
using System.Security.Claims;

using AppClaimTypes = Mesa_Mohloane_Backend.Helpers.ClaimTypes;

namespace Mesa_Mohloane_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService auth) : ControllerBase
{
    private readonly IAuthService _auth = auth;

    // Reads the UserId claim embedded in the JWT by JwtHelper
    private Guid CurrentUserId =>
    Guid.Parse(User.FindFirstValue(AppClaimTypes.UserId)!);

    //Public endpoints 

    /// <summary>
    /// Self-registration for Citizens and Contractors only.
    /// Inspector / Auditor accounts are created by an Administrator.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _auth.RegisterAsync(dto);
        return result.Success
            ? Ok(result.Data)
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _auth.LoginAsync(dto);
        return result.Success
            ? Ok(result.Data)
            : Unauthorized(new { error = result.Error });
    }

    // ── Authenticated endpoints ───────────────────────────────────────────────

    /// <summary>Returns the profile of the currently authenticated user.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var result = await _auth.GetProfileAsync(CurrentUserId);
        return result.Success
            ? Ok(result.Data)
            : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Updates the authenticated user's own profile (name and phone only).
    /// Role and active-status changes are admin-only operations.
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UserUpdateDto dto)
    {
        var result = await _auth.UpdateProfileAsync(CurrentUserId, dto);
        return result.Success
            ? Ok(new { message = "Profile updated successfully." })
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var result = await _auth.ChangePasswordAsync(CurrentUserId, dto);
        return result.Success
            ? Ok(new { message = "Password changed successfully." })
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// JWTs are stateless — logout is handled client-side by discarding the token.
    /// This endpoint exists for a consistent API surface and can later be extended
    /// to support a token blacklist / revocation strategy.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout() =>
        Ok(new { message = "Logged out successfully." });
}