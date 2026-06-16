using AutoMapper;
using CFMS.Application.DTOs.Notifications;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Entities;
using CFMS.Application.Common.Interfaces;
using CFMS.Domain.Enums;

namespace CFMS.Application.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IRealTimeNotificationService _realTimeNotificationService;

    public NotificationService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRealTimeNotificationService realTimeNotificationService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _realTimeNotificationService = realTimeNotificationService;
    }

    public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync(Guid userId, CancellationToken ct = default)
    {
        var notifications = await _unitOfWork.Notifications.FindAsync(n => n.UserId == userId, ct);
        var ordered = notifications.OrderByDescending(n => n.CreatedAtUtc);
        return _mapper.Map<IEnumerable<NotificationDto>>(ordered);
    }

    public async Task<IEnumerable<NotificationDto>> GetUnreadNotificationsAsync(Guid userId, CancellationToken ct = default)
    {
        var notifications = await _unitOfWork.Notifications.GetUnreadByUserIdAsync(userId, ct);
        return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId, ct);
        if (notification == null)
        {
            throw new CFMS.Application.Common.Exceptions.NotFoundException(nameof(Notification), notificationId);
        }

        if (notification.UserId != userId)
        {
            throw new CFMS.Application.Common.Exceptions.ForbiddenException("You do not own this notification.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            _unitOfWork.Notifications.Update(notification);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        await _unitOfWork.Notifications.MarkAllAsReadAsync(userId, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task SendNotificationAsync(
        Guid targetUserId,
        NotificationType type,
        string title,
        string message,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken ct = default)
    {
        var notification = new Notification
        {
            UserId = targetUserId,
            Type = type,
            Title = title,
            Message = message,
            IsRead = false,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType
        };

        await _unitOfWork.Notifications.AddAsync(notification, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = _mapper.Map<NotificationDto>(notification);
        await _realTimeNotificationService.SendNotificationToUserAsync(targetUserId, dto, ct);
    }
}
