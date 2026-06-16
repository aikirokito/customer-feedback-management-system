using CFMS.Domain.Common;

namespace CFMS.Domain.Entities;

/// <summary>
/// Threaded comment on a feedback. Customers and staff use this for the visible discussion thread.
/// </summary>
public class FeedbackComment : SoftDeletableEntity
{
    public Guid FeedbackId { get; set; }

    public Guid AuthorUserId { get; set; }

    public string Content { get; set; } = string.Empty;

    /// <summary>Allows simple reply threading — null means top-level comment.</summary>
    public Guid? ParentCommentId { get; set; }

    // Navigation
    public Feedback Feedback { get; set; } = null!;
    public User AuthorUser { get; set; } = null!;
    public FeedbackComment? ParentComment { get; set; }
    public ICollection<FeedbackComment> Replies { get; set; } = new List<FeedbackComment>();
}
