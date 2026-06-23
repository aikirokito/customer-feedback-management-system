using CFMS.Domain.Common;
using CFMS.Domain.Enums;

namespace CFMS.Domain.Entities;

/// <summary>
/// Core feedback submission entity. Created by a Customer; processed by internal staff.
/// </summary>
public class Feedback : SoftDeletableEntity
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int? Rating { get; set;}

    public FeedbackCategory Category { get; set; }

    public FeedbackStatus Status { get; set; } = FeedbackStatus.New;

    public FeedbackPriority Priority { get; set; } = FeedbackPriority.Medium;

    public Guid SubmittedByUserId { get; set; }

    /// <summary>Nullable — assigned after internal triage.</summary>
    public Guid? AssignedToUserId { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }

    public DateTime? ClosedAtUtc { get; set; }

    // Navigation
    public User SubmittedByUser { get; set; } = null!;
    public User? AssignedToUser { get; set; }
    public ICollection<FeedbackAttachment> Attachments { get; set; } = new List<FeedbackAttachment>();
    public ICollection<FeedbackResponse> Responses { get; set; } = new List<FeedbackResponse>();
    public ICollection<FeedbackComment> Comments { get; set; } = new List<FeedbackComment>();
    public ICollection<FeedbackAssignment> AssignmentHistory { get; set; } = new List<FeedbackAssignment>();
    public ICollection<FeedbackStatusHistory> StatusHistory { get; set; } = new List<FeedbackStatusHistory>();
}
