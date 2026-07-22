using CFMS.Domain.Enums;

namespace CFMS.Application.DTOs.Feedback;

public class CreateFeedbackRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public int? Rating { get; set; }
}

public class UpdateFeedbackRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public int? Rating { get; set; }
}


public class UpdateFeedbackPriorityRequest
{
    public FeedbackPriority Priority { get; set; }
}

public class ChangeFeedbackStatusRequest
{
    public FeedbackStatus NewStatus { get; set; }
    public string? Reason { get; set; }
}

/// <summary>Dữ liệu xác nhận của Manager khi đóng một phản hồi đã RESOLVED.</summary>
public class CloseFeedbackRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class FeedbackFilterRequest
{
    public FeedbackStatus? Status { get; set; }
    public Guid? CategoryId { get; set; }
    public FeedbackPriority? Priority { get; set; }
    public Guid? SubmittedByUserId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
