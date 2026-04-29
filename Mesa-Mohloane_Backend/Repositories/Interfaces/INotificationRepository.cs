using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Repositories.Interfaces;

public interface INotificationRepository
{
    Task<IEnumerable<Notification>> GetByUserAsync(Guid userId, int page, int pageSize, bool unreadOnly);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<Guid> CreateAsync(Notification notification);
    Task MarkAsReadAsync(Guid notificationId, Guid userId);
    Task MarkAllAsReadAsync(Guid userId);
}