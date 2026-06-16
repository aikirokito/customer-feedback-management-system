using CFMS.Domain.Common;

namespace CFMS.Domain.Entities;

/// <summary>
/// File attachment linked to a feedback. Stored in Supabase Storage; only metadata persisted in DB.
/// </summary>
public class FeedbackAttachment : BaseEntity
{
    public Guid FeedbackId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string StorageKey { get; set; } = string.Empty; // path in Supabase bucket

    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public Guid UploadedByUserId { get; set; }

    // Navigation
    public Feedback Feedback { get; set; } = null!;
    public User UploadedByUser { get; set; } = null!;
}
