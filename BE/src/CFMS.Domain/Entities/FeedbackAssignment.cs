using CFMS.Domain.Common;

namespace CFMS.Domain.Entities;

/// <summary>
/// Tracks every assignment action on a feedback ticket (full assignment history).
/// </summary>
public class FeedbackAssignment : BaseEntity
{
    public Guid FeedbackId { get; set; }

    public Guid AssignedToUserId { get; set; }

    public Guid AssignedByUserId { get; set; }

    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public Feedback Feedback { get; set; } = null!;
    public User AssignedToUser { get; set; } = null!;
    public User AssignedByUser { get; set; } = null!;
}
