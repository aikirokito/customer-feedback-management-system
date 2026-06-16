namespace CFMS.Domain.Common;

/// <summary>
/// Extends BaseEntity with soft-delete capability.
/// </summary>
public abstract class SoftDeletableEntity : BaseEntity
{
    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }
}
