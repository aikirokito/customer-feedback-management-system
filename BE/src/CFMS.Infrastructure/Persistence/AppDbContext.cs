using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CFMS.Infrastructure.Persistence;

/// <summary>
/// Main EF Core DbContext. All entity sets and global query filters are configured here.
/// Migrations target: PostgreSQL via Npgsql.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<FeedbackAttachment> FeedbackAttachments => Set<FeedbackAttachment>();
    public DbSet<FeedbackResponse> FeedbackResponses => Set<FeedbackResponse>();
    public DbSet<FeedbackComment> FeedbackComments => Set<FeedbackComment>();
    public DbSet<FeedbackAssignment> FeedbackAssignments => Set<FeedbackAssignment>();
    public DbSet<FeedbackStatusHistory> FeedbackStatusHistories => Set<FeedbackStatusHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration classes from this assembly automatically
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filters for soft delete
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Feedback>().HasQueryFilter(f => !f.IsDeleted);
        modelBuilder.Entity<FeedbackResponse>().HasQueryFilter(r => !r.IsDeleted);
        modelBuilder.Entity<FeedbackComment>().HasQueryFilter(c => !c.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-update UpdatedAtUtc on every SaveChanges
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Domain.Common.BaseEntity baseEntity)
            {
                baseEntity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
