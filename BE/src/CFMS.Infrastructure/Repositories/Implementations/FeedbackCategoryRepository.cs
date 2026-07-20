using CFMS.Application.Common.Interfaces;
using CFMS.Domain.Entities;
using CFMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CFMS.Infrastructure.Repositories.Implementations;

public class FeedbackCategoryRepository : Repository<FeedbackCategoryEntity>, IFeedbackCategoryRepository
{
    public FeedbackCategoryRepository(AppDbContext context) : base(context) { }

    public override async Task<FeedbackCategoryEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.Include(c => c.Department).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IEnumerable<FeedbackCategoryEntity>> GetActiveAsync(CancellationToken ct = default)
        => await _dbSet
            .Where(c => c.IsActive && (c.DepartmentId == null || c.Department!.IsActive))
            .Include(c => c.Department)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<FeedbackCategoryEntity?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.Department)
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.Trim().ToLower(), ct);

    public override async Task<IEnumerable<FeedbackCategoryEntity>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.Include(c => c.Department).OrderBy(c => c.Name).ToListAsync(ct);
}
