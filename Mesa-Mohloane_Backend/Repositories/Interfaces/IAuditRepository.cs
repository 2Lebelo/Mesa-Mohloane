using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Repositories.Interfaces;

public interface IAuditRepository
{
    // Overload 1 — full Guid-based signature
    Task LogAsync(string action, string entityName, Guid? entityId, Guid actorUserId,
        string? oldValuesJson = null, string? newValuesJson = null,
        string? ipAddress = null, string? notes = null);

    // Overload 2 — string entityId + string performer (used by AuthService)
    Task LogAsync(string action, string entityName, string entityId,
        string performedBy, string? details = null);

    // Overload 3 — Guid entityId + string performer + nullable Guid userId (used by UserService)
    Task LogAsync(string action, string entityName, Guid entityId,
        string performedBy, Guid? userId, string? details = null);

    // Query methods
    Task<IEnumerable<AuditLog>> GetAllAsync(int page, int pageSize);
    Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 50);
    Task<IEnumerable<AuditLog>> GetByUserAsync(Guid userId, int page, int pageSize);
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, Guid entityId, int page, int pageSize);
    Task<IEnumerable<AuditLog>> GetByActionTypeAsync(AuditActionType actionType, int page, int pageSize);
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<int> GetCountByUserAsync(Guid userId);
    Task<int> GetCountByEntityAsync(string entityName, Guid entityId);
}