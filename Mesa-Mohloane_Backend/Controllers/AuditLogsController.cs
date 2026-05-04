using Mesa_Mohloane_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mesa_Mohloane_Backend.Controllers;

[ApiController]
[Route("api/auditlogs")]
[Authorize(Roles = "Administrator,Auditor")]
public class AuditLogsController(IAuditLogService auditLogs) : ControllerBase
{
    private readonly IAuditLogService _auditLogs = auditLogs;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _auditLogs.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("entity/{entityType}/{entityId:guid}")]
    public async Task<IActionResult> GetByEntity(
        string entityType,
        Guid entityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _auditLogs.GetByEntityAsync(entityType, entityId, page, pageSize);
        return Ok(result);
    }
}
