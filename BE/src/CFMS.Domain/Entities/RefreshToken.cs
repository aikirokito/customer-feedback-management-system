using CFMS.Domain.Common;

namespace CFMS.Domain.Entities;

/// <summary>
/// Stores JWT refresh tokens. One user can have multiple active refresh tokens (e.g. multiple devices).
/// Token rotation is performed on each refresh — old tokens are revoked and replaced.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }

    /// <summary>The raw token value (opaque string). Stored as plain text — tokens are long-lived
    /// but high-entropy, so plain storage is acceptable for this design. Hash if stricter needed.</summary>
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public bool IsRevoked { get; set; } = false;

    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>The token that replaced this one during rotation.</summary>
    public string? ReplacedByToken { get; set; }

    /// <summary>IP address from which the token was originally issued.</summary>
    public string? CreatedByIp { get; set; }

    /// <summary>IP address from which the token was revoked.</summary>
    public string? RevokedByIp { get; set; }

    // --- Computed helpers (not persisted) ---

    /// <summary>True if the token has passed its expiry time.</summary>
    public bool IsExpired => ExpiresAtUtc < DateTime.UtcNow;

    /// <summary>True if the token can still be used to obtain a new access token.</summary>
    public bool IsActive => !IsRevoked && !IsExpired;

    // Navigation
    public User User { get; set; } = null!;
}
