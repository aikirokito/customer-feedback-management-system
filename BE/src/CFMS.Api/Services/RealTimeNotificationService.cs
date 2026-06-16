using CFMS.Api.Hubs;
using CFMS.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CFMS.Api.Services;

public class RealTimeNotificationService : IRealTimeNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public RealTimeNotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationToUserAsync(Guid userId, object notification, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"user-{userId}").SendAsync(NotificationHub.NotificationCreatedEvent, notification, cancellationToken: ct);
    }
}
