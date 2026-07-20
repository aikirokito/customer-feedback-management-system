using CFMS.Domain.Entities;

namespace CFMS.Application.Common.Interfaces;

public interface IFeedbackCategoryRepository : IRepository<FeedbackCategoryEntity>
{
    Task<IEnumerable<FeedbackCategoryEntity>> GetActiveAsync(CancellationToken ct = default);
    Task<FeedbackCategoryEntity?> GetByNameAsync(string name, CancellationToken ct = default);
}
