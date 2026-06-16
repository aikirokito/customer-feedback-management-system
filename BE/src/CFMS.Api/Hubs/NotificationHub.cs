using CFMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CFMS.Api.Hubs;

/// <summary>
/// SignalR hub for authenticated, user-specific notification delivery.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    public const string NotificationCreatedEvent = "notification.created";
    public const string NotificationReadEvent = "notification.read";
    public const string UnreadCountChangedEvent = "notification.unreadCountChanged";
    public const string FeedbackAssignedEvent = "feedback.assigned";
    public const string FeedbackStatusChangedEvent = "feedback.statusChanged";
    public const string FeedbackResponseAddedEvent = "feedback.responseAdded";
    public const string FeedbackCommentAddedEvent = "feedback.commentAdded";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(AppClaimTypes.UserId)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        await base.OnConnectedAsync();
    }
}
