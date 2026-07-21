namespace CFMS.Domain.Enums;

public enum FeedbackStatus
{
    /// <summary>Khách hàng vừa gửi phản hồi, chưa được phân công.</summary>
    Submitted = 1,

    /// <summary>Manager đã phân công phản hồi cho một nhân viên.</summary>
    Assigned = 2,

    /// <summary>Nhân viên đang xử lý phản hồi.</summary>
    InProgress = 3,

    /// <summary>Nhân viên đã đưa ra câu trả lời hoặc giải pháp.</summary>
    Resolved = 4,

    /// <summary>Manager đã kiểm tra và đóng phản hồi.</summary>
    Closed = 5,

    /// <summary>Khách hàng hủy phản hồi trước khi được phân công.</summary>
    Cancelled = 6
}
