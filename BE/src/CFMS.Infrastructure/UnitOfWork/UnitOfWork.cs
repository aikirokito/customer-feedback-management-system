using CFMS.Infrastructure.Persistence;
using CFMS.Infrastructure.Repositories.Implementations;
using CFMS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace CFMS.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    public IUserRepository Users { get; }
    public IFeedbackRepository Feedbacks { get; }
    public IRefreshTokenRepository RefreshTokens { get; }
    public INotificationRepository Notifications { get; }
    public IAuditLogRepository AuditLogs { get; }
    public IFeedbackCategoryRepository FeedbackCategories { get; }
    public IRepository<CFMS.Domain.Entities.Department> Departments { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Users = new UserRepository(context);
        Feedbacks = new FeedbackRepository(context);
        RefreshTokens = new RefreshTokenRepository(context);
        Notifications = new NotificationRepository(context);
        AuditLogs = new AuditLogRepository(context);
        FeedbackCategories = new FeedbackCategoryRepository(context);
        Departments = new Repository<CFMS.Domain.Entities.Department>(context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _currentTransaction = await _context.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.CommitAsync(ct);
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync(ct);
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
    }
}
