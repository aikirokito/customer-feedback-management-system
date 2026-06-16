using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.Common.Models;
using CFMS.Application.DTOs.Users;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Enums;

namespace CFMS.Application.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UserService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    // -------------------------------------------------------------------------
    // Sprint 1: /api/users/me
    // -------------------------------------------------------------------------

    public async Task<UserDetailDto> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), userId);

        return _mapper.Map<UserDetailDto>(user);
    }

    // -------------------------------------------------------------------------
    // Sprint 2: User management (admin/manager only) — not yet implemented
    // -------------------------------------------------------------------------

    public async Task<PagedResult<UserListItemDto>> GetUsersAsync(int page, int pageSize, UserRole? role = null, string? searchTerm = null, CancellationToken ct = default)
    {
        var (items, totalCount) = await _unitOfWork.Users.GetPagedAsync(page, pageSize, role, searchTerm, ct);
        var dtos = _mapper.Map<IEnumerable<UserListItemDto>>(items);
        return PagedResult<UserListItemDto>.Create(dtos, page, pageSize, totalCount);
    }

    public async Task<UserDetailDto> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), id);

        return _mapper.Map<UserDetailDto>(user);
    }

    public async Task<UserDetailDto> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), id);

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        return _mapper.Map<UserDetailDto>(user);
    }

    public async Task UpdateUserRoleAsync(Guid id, UpdateUserRoleRequest request, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), id);

        user.Role = request.Role;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeactivateUserAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), id);

        user.Status = UserStatus.Disabled;

        await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(id, ct);

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ReactivateUserAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), id);

        user.Status = UserStatus.Active;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteUserAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), id);

        user.IsDeleted = true;
        user.DeletedAtUtc = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
