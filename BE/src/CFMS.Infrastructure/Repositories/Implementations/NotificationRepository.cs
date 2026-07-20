using CFMS.Domain.Entities;
using CFMS.Infrastructure.Persistence;
using CFMS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CFMS.Infrastructure.Repositories.Implementations;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _dbSet.Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        // Keep changes tracked so the service controls the transaction boundary.
        var notifications = await _dbSet.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        foreach (var n in notifications)
        {
            n.IsRead = true;
            n.ReadAtUtc = DateTime.UtcNow;
        }
    }
}
