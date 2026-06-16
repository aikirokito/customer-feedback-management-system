using CFMS.Application.DTOs.Comments;
using CFMS.Domain.Enums;

namespace CFMS.Application.DTOs.Feedback;

public class FeedbackListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public FeedbackCategory Category { get; set; }
    public FeedbackStatus Status { get; set; }
    public FeedbackPriority Priority { get; set; }
    public string SubmittedByUserName { get; set; } = string.Empty;
    public string? AssignedToUserName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class FeedbackDetailDto : FeedbackListItemDto
{
    public string Description { get; set; } = string.Empty;
    public Guid SubmittedByUserId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public IEnumerable<FeedbackAttachmentDto> Attachments { get; set; } = new List<FeedbackAttachmentDto>();
    public IEnumerable<FeedbackResponseDto> Responses { get; set; } = new List<FeedbackResponseDto>();
    public IEnumerable<FeedbackStatusHistoryDto> StatusHistory { get; set; } = new List<FeedbackStatusHistoryDto>();
    public IEnumerable<CommentDto> Comments { get; set; } = new List<CommentDto>();
}

public class FeedbackAttachmentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; }
}

public class FeedbackResponseDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public string RespondedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class FeedbackStatusHistoryDto
{
    public FeedbackStatus FromStatus { get; set; }
    public FeedbackStatus ToStatus { get; set; }
    public string ChangedByUserName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}
