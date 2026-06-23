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
    private readonly IAuditLogService _auditLogService;

    public UserService(IUnitOfWork unitOfWork, IMapper mapper, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _auditLogService = auditLogService;
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

    public async Task UpdateUserRoleAsync(Guid id, UpdateUserRoleRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), id);
        if (id == actorUserId && user.Role == UserRole.SystemAdmin && request.Role != UserRole.SystemAdmin)
        {
            throw new BusinessRuleException("System Admin cannot demote their own active admin account.");
        }

        if (user.Role == request.Role)
        {
            return;
        }

        var oldRole = user.Role;

        user.Role = request.Role;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(
            actorUserId,
            AuditAction.Update,
            nameof(Domain.Entities.User),
            user.Id,
            $"Role={oldRole}",
            $"Role={user.Role}",
            null,
        ct);
    }

    public async Task DeactivateUserAsync(Guid id, Guid actorUserId, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), id);
        
        if (id == actorUserId && user.Role == UserRole.SystemAdmin)
        {
            throw new BusinessRuleException("System Admin cannot diasble their own active admin account.");
        }

        if (user.Status == UserStatus.Disabled)
        {
            return;
        }

        var oldStatus = user.Status;

        user.Status = UserStatus.Disabled;

        await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(id, ct);

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(
            actorUserId,
            AuditAction.Update,
            nameof(Domain.Entities.User),
            user.Id,
            $"Status={oldStatus}",
            $"Status={user.Status}",
            null,
        ct);
    }

    public async Task ReactivateUserAsync(Guid id, Guid actorUserId, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), id);
        if (user.Status == UserStatus.Active)
        {
            return;
        }

        var oldStatus = user.Status;

        user.Status = UserStatus.Active;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
        
        await _auditLogService.LogAsync(
            actorUserId,
            AuditAction.Update,
            nameof(Domain.Entities.User),
            user.Id,
            $"Status={oldStatus}",
            $"Status={user.Status}",
            null,
        ct);
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
