using CFMS.Application.Common.Models;
using CFMS.Application.DTOs.Users;
using CFMS.Domain.Enums;

namespace CFMS.Application.Services.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserListItemDto>> GetUsersAsync(int page, int pageSize, UserRole? role = null, string? searchTerm = null, CancellationToken ct = default);
    Task<UserDetailDto> GetUserByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserDetailDto> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);
    Task UpdateUserRoleAsync(Guid id, UpdateUserRoleRequest request, CancellationToken ct = default);
    Task DeactivateUserAsync(Guid id, CancellationToken ct = default);
    Task ReactivateUserAsync(Guid id, CancellationToken ct = default);
    Task DeleteUserAsync(Guid id, CancellationToken ct = default);
    Task<UserDetailDto> GetProfileAsync(Guid userId, CancellationToken ct = default);
}
