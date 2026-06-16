using CFMS.Application.DTOs.Notifications;

namespace CFMS.Application.Services.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetNotificationsAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<NotificationDto>> GetUnreadNotificationsAsync(Guid userId, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
    Task SendNotificationAsync(Guid targetUserId, Domain.Enums.NotificationType type, string title, string message, Guid? relatedEntityId = null, string? relatedEntityType = null, CancellationToken ct = default);
}
