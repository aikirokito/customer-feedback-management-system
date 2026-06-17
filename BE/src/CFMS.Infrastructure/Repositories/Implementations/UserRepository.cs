using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using CFMS.Application.Common.Interfaces;
using CFMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CFMS.Infrastructure.Repositories.Implementations;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<User?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.GoogleSubject == googleSubject, ct);

    public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role, CancellationToken ct = default)
        => await _dbSet.Where(u => u.Role == role && u.Status == UserStatus.Active).ToListAsync(ct);

    public async Task<bool> IsEmailTakenAsync(string email, CancellationToken ct = default)
        => await _dbSet.AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        UserRole? role = null,
        string? searchTerm = null,
        CancellationToken ct = default)
    {
        var query = _dbSet.AsQueryable();

        if (role.HasValue) query = query.Where(u => u.Role == role.Value);
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLowerInvariant();
            query = query.Where(u => u.Email.Contains(searchLower) ||
                                     u.FirstName.Contains(searchLower) ||
                                     u.LastName.Contains(searchLower) ||
                                     (u.PhoneNumber != null && u.PhoneNumber.Contains(searchLower)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
