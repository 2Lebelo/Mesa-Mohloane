using Microsoft.AspNetCore.Mvc;
using Mesa_Mohloane_Backend.Data;
using Mesa_Mohloane_Backend.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Mesa_Mohloane_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController(MesaMohloaneDbContext db) : ControllerBase
{
    private readonly MesaMohloaneDbContext _db = db;

    // Public — no auth required.
    // Returns only self-registerable roles (Citizen and Contractor).
    // Inspector and Administrator are intentionally excluded.
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicRoles()
    {
        var roles = await _db.Roles
            .Where(r => r.Name == "Citizen" || r.Name == "Contractor")
            .Select(r => new RoleDto(r.Id, r.Name, r.Description))
            .ToListAsync();

        return Ok(roles);
    }
}