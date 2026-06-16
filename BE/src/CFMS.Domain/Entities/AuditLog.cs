using CFMS.Domain.Common;
using CFMS.Domain.Enums;

namespace CFMS.Domain.Entities;

/// <summary>
/// System-wide audit log. Tracks all significant user actions.
/// Immutable — never updated or deleted.
/// </summary>
public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; } // null for system actions

    public AuditAction Action { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    /// <summary>JSON snapshot of the entity before the change.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of the entity after the change.</summary>
    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    // Navigation
    public User? User { get; set; }
}
