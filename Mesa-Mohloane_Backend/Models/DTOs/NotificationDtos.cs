using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Models.DTOs;

public record NotificationDto(
    Guid Id,
    Guid UserId,
    NotificationType Type,
    string Title,
    string Message,
    string? RelatedEntityName,
    Guid? RelatedEntityId,
    bool IsRead,
    DateTime SentAt,
    DateTime? ReadAt);
