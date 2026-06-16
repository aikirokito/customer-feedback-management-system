namespace CFMS.Application.Common.Interfaces;

/// <summary>
/// Unit of Work: wraps all repositories and exposes a single SaveChangesAsync.
/// Ensures all repository operations in one request commit atomically.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IFeedbackRepository Feedbacks { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    INotificationRepository Notifications { get; }
    IAuditLogRepository AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
