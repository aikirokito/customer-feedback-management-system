using CFMS.Domain.Entities;
using CFMS.Domain.Enums;

namespace CFMS.Application.Common.Interfaces;

/// <summary>
/// User-specific repository with domain-level query methods.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken ct = default);
    Task<IEnumerable<User>> GetByRoleAsync(UserRole role, CancellationToken ct = default);
    Task<bool> IsEmailTakenAsync(string email, CancellationToken ct = default);
    Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        UserRole? role = null,
        string? searchTerm = null,
        CancellationToken ct = default);
}
