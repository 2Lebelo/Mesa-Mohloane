using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Services.Interfaces;

public interface INotificationService
{
    // Internal — called by other services to dispatch notifications
    Task SendAsync(Guid userId, NotificationType type, string title, string message,
        string? relatedEntityName = null, Guid? relatedEntityId = null);

    // User-facing
    Task<PagedResultDto<NotificationDto>> GetMyNotificationsAsync(
        Guid userId, int page, int pageSize, bool unreadOnly);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<ServiceResult> MarkAsReadAsync(Guid notificationId, Guid userId);
    Task<ServiceResult> MarkAllAsReadAsync(Guid userId);
}