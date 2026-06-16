using CFMS.Domain.Enums;

namespace CFMS.Domain.Rules;

/// <summary>
/// Encodes the SRS-defined feedback status lifecycle.
/// </summary>
public static class FeedbackStatusRules
{
    private static readonly Dictionary<FeedbackStatus, HashSet<FeedbackStatus>> AllowedTransitions = new()
    {
        [FeedbackStatus.New] = new() { FeedbackStatus.Assigned, FeedbackStatus.Rejected },
        [FeedbackStatus.Assigned] = new() { FeedbackStatus.InProgress, FeedbackStatus.Rejected },
        [FeedbackStatus.InProgress] = new() { FeedbackStatus.WaitingForCustomer, FeedbackStatus.Resolved, FeedbackStatus.Rejected },
        [FeedbackStatus.WaitingForCustomer] = new() { FeedbackStatus.InProgress, FeedbackStatus.Resolved, FeedbackStatus.Closed },
        [FeedbackStatus.Resolved] = new() { FeedbackStatus.Closed, FeedbackStatus.InProgress },
        [FeedbackStatus.Rejected] = new() { FeedbackStatus.Closed },
        [FeedbackStatus.Closed] = new()
    };

    public static bool IsTransitionAllowed(FeedbackStatus from, FeedbackStatus to)
        => AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static IReadOnlySet<FeedbackStatus> GetAllowedTransitions(FeedbackStatus from)
        => AllowedTransitions.TryGetValue(from, out var allowed) ? allowed : new HashSet<FeedbackStatus>();

    public static bool RequiresReason(FeedbackStatus status)
        => status is FeedbackStatus.Rejected or FeedbackStatus.Closed;
}
