namespace Mesa_Mohloane_Frontend.Dtos;

public sealed record NotificationDto(
    Guid Id,
    Guid UserId,
    int Type,
    string Title,
    string Message,
    string? RelatedEntityName,
    Guid? RelatedEntityId,
    bool IsRead,
    DateTime SentAt,
    DateTime? ReadAt);
