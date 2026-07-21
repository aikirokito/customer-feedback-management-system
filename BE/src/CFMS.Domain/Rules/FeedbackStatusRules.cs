using CFMS.Domain.Enums;

namespace CFMS.Domain.Rules;

/// <summary>
/// Quy định luồng trạng thái V1. Mọi thay đổi trạng thái phải đi qua lớp này
/// để tránh mỗi controller tự áp dụng một quy tắc khác nhau.
/// </summary>
public static class FeedbackStatusRules
{
    private static readonly Dictionary<FeedbackStatus, HashSet<FeedbackStatus>> AllowedTransitions = new()
    {
        [FeedbackStatus.Submitted] = new() { FeedbackStatus.Assigned, FeedbackStatus.Cancelled },
        [FeedbackStatus.Assigned] = new() { FeedbackStatus.InProgress },
        [FeedbackStatus.InProgress] = new() { FeedbackStatus.Resolved },
        [FeedbackStatus.Resolved] = new() { FeedbackStatus.Closed },
        [FeedbackStatus.Closed] = new(),
        [FeedbackStatus.Cancelled] = new()
    };

    /// <summary>Trả về true khi bước chuyển trạng thái đúng luồng nghiệp vụ V1.</summary>
    public static bool IsTransitionAllowed(FeedbackStatus from, FeedbackStatus to)
        => AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    /// <summary>Lấy danh sách trạng thái có thể chuyển tới để hiển thị trên UI.</summary>
    public static IReadOnlySet<FeedbackStatus> GetAllowedTransitions(FeedbackStatus from)
        => AllowedTransitions.TryGetValue(from, out var allowed) ? allowed : new HashSet<FeedbackStatus>();

    /// <summary>Manager phải ghi lý do/xác nhận khi đóng phản hồi.</summary>
    public static bool RequiresReason(FeedbackStatus status)
        => status is FeedbackStatus.Closed;
}
