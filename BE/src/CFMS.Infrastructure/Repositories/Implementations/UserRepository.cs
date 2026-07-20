using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using CFMS.Application.Common.Interfaces;
using CFMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CFMS.Infrastructure.Repositories.Implementations;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    private IQueryable<User> ExistingUsers => _dbSet.Where(user => !user.IsDeleted);

    public override async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await ExistingUsers.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id, ct);

    public override async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default)
        => await ExistingUsers.ToListAsync(ct);

    public override async Task<IEnumerable<User>> FindAsync(Expression<Func<User, bool>> predicate, CancellationToken ct = default)
        => await ExistingUsers.Where(predicate).ToListAsync(ct);

    public override async Task<User?> FirstOrDefaultAsync(Expression<Func<User, bool>> predicate, CancellationToken ct = default)
        => await ExistingUsers.FirstOrDefaultAsync(predicate, ct);

    public override async Task<bool> AnyAsync(Expression<Func<User, bool>> predicate, CancellationToken ct = default)
        => await ExistingUsers.AnyAsync(predicate, ct);

    public override async Task<int> CountAsync(Expression<Func<User, bool>>? predicate = null, CancellationToken ct = default)
        => predicate == null
            ? await ExistingUsers.CountAsync(ct)
            : await ExistingUsers.CountAsync(predicate, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await ExistingUsers.Include(u => u.Department).FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<User?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken ct = default)
        => await ExistingUsers.FirstOrDefaultAsync(u => u.GoogleSubject == googleSubject, ct);

    public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role, CancellationToken ct = default)
        => await ExistingUsers.Include(u => u.Department).Where(u => u.Role == role && u.Status == UserStatus.Active).ToListAsync(ct);

    public async Task<bool> IsEmailTakenAsync(string email, CancellationToken ct = default)
        // Deleted accounts still reserve their unique email address.
        => await _dbSet.AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        UserRole? role = null,
        string? searchTerm = null,
        CancellationToken ct = default)
    {
        var query = ExistingUsers;

        if (role.HasValue) query = query.Where(u => u.Role == role.Value);
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLowerInvariant();
            query = query.Where(u => u.Email.ToLower().Contains(searchLower) ||
                                     u.FirstName.ToLower().Contains(searchLower) ||
                                     u.LastName.ToLower().Contains(searchLower) ||
                                     (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(searchLower)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .Include(u => u.Department)
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
