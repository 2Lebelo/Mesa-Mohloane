using Mesa_Mohloane_Backend.Helpers;
using Mesa_Mohloane_Backend.Models.DTOs;
using Mesa_Mohloane_Backend.Models.Entities;
using Mesa_Mohloane_Backend.Repositories.Interfaces;
using Mesa_Mohloane_Backend.Services.Interfaces;

namespace Mesa_Mohloane_Backend.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifRepo;

    public NotificationService(INotificationRepository notifRepo)
        => _notifRepo = notifRepo;

    // Called internally by other services — fire-and-forget safe
    public async Task SendAsync(
        Guid userId, NotificationType type, string title, string message,
        string? relatedEntityName = null, Guid? relatedEntityId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityName = relatedEntityName,
            RelatedEntityId = relatedEntityId,
            IsRead = false,
            SentAt = DateTime.UtcNow
        };

        await _notifRepo.CreateAsync(notification);
    }

    public async Task<PagedResultDto<NotificationDto>> GetMyNotificationsAsync(
        Guid userId, int page, int pageSize, bool unreadOnly)
    {
        var items = await _notifRepo.GetByUserAsync(userId, page, pageSize, unreadOnly);
        var total = await _notifRepo.GetUnreadCountAsync(userId);

        return new PagedResultDto<NotificationDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
        => await _notifRepo.GetUnreadCountAsync(userId);

    public async Task<ServiceResult> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        await _notifRepo.MarkAsReadAsync(notificationId, userId);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> MarkAllAsReadAsync(Guid userId)
    {
        await _notifRepo.MarkAllAsReadAsync(userId);
        return ServiceResult.Ok();
    }

    private static NotificationDto MapToDto(Notification n) => new(
        n.Id, n.UserId, n.Type, n.Title, n.Message,
        n.RelatedEntityName, n.RelatedEntityId,
        n.IsRead, n.SentAt, n.ReadAt);
}