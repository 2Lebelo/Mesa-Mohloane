using Mesa_Mohloane_Backend.Models.DTOs;

namespace Mesa_Mohloane_Backend.Services.Interfaces;

public interface IAuditLogService
{
    Task<PagedResultDto<AuditLogDto>> GetAllAsync(int page, int pageSize);
    Task<PagedResultDto<AuditLogDto>> GetByEntityAsync(string entityName, Guid entityId, int page, int pageSize);
    Task<PagedResultDto<AuditLogDto>> GetByUserAsync(Guid userId, int page, int pageSize);
}
