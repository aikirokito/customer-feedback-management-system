using CFMS.Domain.Common;

namespace CFMS.Domain.Entities;

/// <summary>
/// Official response from Support Staff or Manager to a feedback ticket.
/// Visible to the customer.
/// </summary>
public class FeedbackResponse : SoftDeletableEntity
{
    public Guid FeedbackId { get; set; }

    public Guid RespondedByUserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public bool IsInternal { get; set; } = false; // internal note vs public reply

    // Navigation
    public Feedback Feedback { get; set; } = null!;
    public User RespondedByUser { get; set; } = null!;
}
