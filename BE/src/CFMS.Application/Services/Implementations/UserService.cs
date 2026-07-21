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
    // System Admin user management
    // -------------------------------------------------------------------------

    public async Task<PagedResult<UserListItemDto>> GetUsersAsync(int page, int pageSize, UserRole? role = null, string? searchTerm = null, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
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

        var oldValues = $"FirstName={user.FirstName};LastName={user.LastName};PhoneNumber={user.PhoneNumber}";
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(
            id,
            AuditAction.Update,
            nameof(Domain.Entities.User),
            user.Id,
            oldValues,
            $"FirstName={user.FirstName};LastName={user.LastName};PhoneNumber={user.PhoneNumber}",
            null,
            ct);

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

        if (user.Role == request.Role &&
            (!request.DepartmentId.HasValue || request.DepartmentId == user.DepartmentId))
        {
            return;
        }

        var oldRole = user.Role;
        var oldDepartmentId = user.DepartmentId;

        var requestedDepartmentId = request.Role is UserRole.SupportStaff or UserRole.DepartmentManager
            ? request.DepartmentId ?? user.DepartmentId
            : null;
        if (user.Role == UserRole.SupportStaff &&
            (request.Role != UserRole.SupportStaff || requestedDepartmentId != user.DepartmentId))
        {
            await EnsureNoActiveAssignmentsAsync(user.Id, ct);
        }

        if (request.Role is UserRole.SupportStaff or UserRole.DepartmentManager)
        {
            var departmentId = requestedDepartmentId
                ?? throw new BusinessRuleException("Support Staff and Department Managers must belong to a department.");
            var department = await _unitOfWork.Departments.GetByIdAsync(departmentId, ct)
                ?? throw new NotFoundException(nameof(Domain.Entities.Department), departmentId);
            if (!department.IsActive)
            {
                throw new BusinessRuleException("Disabled departments cannot be assigned to users.");
            }
            user.DepartmentId = departmentId;
        }
        else
        {
            user.DepartmentId = null;
        }

        user.Role = request.Role;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(
            actorUserId,
            AuditAction.Update,
            nameof(Domain.Entities.User),
            user.Id,
            $"Role={oldRole};DepartmentId={oldDepartmentId}",
            $"Role={user.Role};DepartmentId={user.DepartmentId}",
            null,
        ct);
    }

    public async Task DeactivateUserAsync(Guid id, Guid actorUserId, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), id);
        
        if (id == actorUserId && user.Role == UserRole.SystemAdmin)
        {
            throw new BusinessRuleException("System Admin cannot disable their own active admin account.");
        }

        if (user.Status == UserStatus.Disabled)
        {
            return;
        }

        if (user.Role == UserRole.SupportStaff)
        {
            await EnsureNoActiveAssignmentsAsync(user.Id, ct);
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

    public async Task DeleteUserAsync(Guid id, Guid actorUserId, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), id);

        if (id == actorUserId)
        {
            throw new BusinessRuleException("System Admin cannot delete their own account.");
        }

        if (user.Role == UserRole.SupportStaff)
        {
            await EnsureNoActiveAssignmentsAsync(user.Id, ct);
        }

        await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(id, ct);

        user.IsDeleted = true;
        user.DeletedAtUtc = DateTime.UtcNow;
        user.DeletedByUserId = actorUserId;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(
            actorUserId,
            AuditAction.Delete,
            nameof(Domain.Entities.User),
            user.Id,
            null,
            $"Deleted user {user.Email}",
            null,
            ct);
    }

    private async Task EnsureNoActiveAssignmentsAsync(Guid userId, CancellationToken ct)
    {
        var hasActiveAssignments = await _unitOfWork.Feedbacks.AnyAsync(
            feedback => feedback.AssignedToUserId == userId &&
                        feedback.Status != FeedbackStatus.Resolved &&
                        feedback.Status != FeedbackStatus.Cancelled &&
                        feedback.Status != FeedbackStatus.Closed,
            ct);

        if (hasActiveAssignments)
        {
            throw new BusinessRuleException("Reassign or unassign this staff member's active feedback before changing or deleting the account.");
        }
    }
}
