using CFMS.Domain.Common;
using CFMS.Domain.Enums;

namespace CFMS.Domain.Entities;

/// <summary>
/// Immutable audit trail of every status change on a feedback ticket.
/// </summary>
public class FeedbackStatusHistory : BaseEntity
{
    public Guid FeedbackId { get; set; }

    public FeedbackStatus FromStatus { get; set; }

    public FeedbackStatus ToStatus { get; set; }

    public Guid ChangedByUserId { get; set; }

    public string? Reason { get; set; }

    // Navigation
    public Feedback Feedback { get; set; } = null!;
    public User ChangedByUser { get; set; } = null!;
}
