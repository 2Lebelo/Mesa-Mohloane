using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Mesa_Mohloane_Backend.Services.Interfaces;

namespace Mesa_Mohloane_Backend.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditRepository _auditRepo;

    public AuditLogService(IAuditRepository auditRepo)
        => _auditRepo = auditRepo;

    public async Task<PagedResultDto<AuditLogDto>> GetAllAsync(int page, int pageSize)
    {
        var items = await _auditRepo.GetAllAsync(page, pageSize);
        var total = await _auditRepo.GetTotalCountAsync();

        return new PagedResultDto<AuditLogDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResultDto<AuditLogDto>> GetByEntityAsync(
        string entityName, Guid entityId, int page, int pageSize)
    {
        var items = await _auditRepo.GetByEntityAsync(entityName, entityId, page, pageSize);
        var total = await _auditRepo.GetCountByEntityAsync(entityName, entityId);

        return new PagedResultDto<AuditLogDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResultDto<AuditLogDto>> GetByUserAsync(
        Guid userId, int page, int pageSize)
    {
        var items = await _auditRepo.GetByUserAsync(userId, page, pageSize);
        var total = await _auditRepo.GetCountByUserAsync(userId);

        return new PagedResultDto<AuditLogDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static AuditLogDto MapToDto(Models.Entities.AuditLog log) => new(
        log.Id,
        log.ActorUserId,
        log.ActionType,
        log.EntityName,
        log.EntityId,
        log.OldValuesJson,
        log.NewValuesJson,
        log.IpAddress,
        log.ActionAt,
        log.Notes);
}
