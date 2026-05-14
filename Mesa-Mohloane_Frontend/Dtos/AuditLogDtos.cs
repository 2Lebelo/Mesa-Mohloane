namespace Mesa_Mohloane_Frontend.Dtos;

public sealed record AuditLogDto(
    Guid Id,
    Guid? ActorUserId,
    string ActionType,
    string EntityName,
    Guid? EntityId,
    string? OldValuesJson,
    string? NewValuesJson,
    string? IpAddress,
    DateTime ActionAt,
    string? Notes);
