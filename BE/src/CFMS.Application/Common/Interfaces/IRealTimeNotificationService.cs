namespace CFMS.Application.Common.Interfaces;

public interface IRealTimeNotificationService
{
    Task SendNotificationToUserAsync(Guid userId, object notification, CancellationToken ct = default);
}
