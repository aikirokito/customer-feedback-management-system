using CFMS.Domain.Enums;

namespace CFMS.Application.DTOs.Feedback;

public class CreateFeedbackRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FeedbackCategory Category { get; set; }
    public int? Rating { get; set;}
}

public class UpdateFeedbackRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FeedbackCategory Category { get; set; }
    public FeedbackPriority Priority { get; set; }
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

public class FeedbackFilterRequest
{
    public FeedbackStatus? Status { get; set; }
    public FeedbackCategory? Category { get; set; }
    public FeedbackPriority? Priority { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
