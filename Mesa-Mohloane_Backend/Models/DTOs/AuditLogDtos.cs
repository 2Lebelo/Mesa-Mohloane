using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Models.DTOs;

public record AuditLogDto(
    Guid Id,
    Guid ActorUserId,
    AuditActionType ActionType,
    string EntityName,
    Guid? EntityId,
    string? OldValuesJson,
    string? NewValuesJson,
    string? IpAddress,
    DateTime ActionAt,
    string? Notes);
