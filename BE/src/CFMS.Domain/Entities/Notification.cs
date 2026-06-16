using CFMS.Domain.Common;
using CFMS.Domain.Enums;

namespace CFMS.Domain.Entities;

/// <summary>
/// System or user-triggered notification delivered to a specific user.
/// </summary>
public class Notification : BaseEntity
{
    public Guid UserId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime? ReadAtUtc { get; set; }

    /// <summary>Optional deep-link to the related entity (e.g. feedback ID).</summary>
    public Guid? RelatedEntityId { get; set; }

    public string? RelatedEntityType { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
