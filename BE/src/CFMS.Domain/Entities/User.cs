using CFMS.Domain.Common;
using CFMS.Domain.Enums;

namespace CFMS.Domain.Entities;

/// <summary>
/// Represents a registered system user. Supports both local (password) and OAuth logins.
/// A user has exactly one role at any time.
/// </summary>
public class User : SoftDeletableEntity
{
    public string Email { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>Computed full name — not persisted.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    public UserRole Role { get; set; } = UserRole.Customer;

    /// <summary>Account lifecycle status. Persisted as a string enum.</summary>
    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>Convenience helper — derived from Status, not stored.</summary>
    public bool IsActive => Status == UserStatus.Active;

    public bool IsEmailVerified { get; set; } = false;

    /// <summary>Populated when the user authenticated via Google OAuth.</summary>
    public string? GoogleSubject { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    // Navigation
    public ICollection<Feedback> SubmittedFeedbacks { get; set; } = new List<Feedback>();
    public ICollection<FeedbackAssignment> Assignments { get; set; } = new List<FeedbackAssignment>();
    public ICollection<FeedbackResponse> Responses { get; set; } = new List<FeedbackResponse>();
    public ICollection<FeedbackComment> Comments { get; set; } = new List<FeedbackComment>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
