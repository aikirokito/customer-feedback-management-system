using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.DTOs.Departments;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;

namespace CFMS.Application.Services.Implementations;

public class DepartmentService : IDepartmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuditLogService _auditLogService;

    public DepartmentService(IUnitOfWork unitOfWork, IMapper mapper, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _auditLogService = auditLogService;
    }

    public async Task<IEnumerable<DepartmentDto>> GetActiveAsync(CancellationToken ct = default)
    {
        var departments = await _unitOfWork.Departments.FindAsync(d => d.IsActive, ct);
        return _mapper.Map<IEnumerable<DepartmentDto>>(departments.OrderBy(d => d.Name));
    }

    public async Task<IEnumerable<DepartmentDto>> GetAllAsync(CancellationToken ct = default)
    {
        var departments = await _unitOfWork.Departments.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<DepartmentDto>>(departments.OrderBy(d => d.Name));
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        await EnsureUniqueNameAsync(name, null, ct);

        var department = new Department
        {
            Name = name,
            Description = NormalizeOptional(request.Description),
            IsActive = true
        };

        await _unitOfWork.Departments.AddAsync(department, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await _auditLogService.LogAsync(
            actorUserId,
            AuditAction.Create,
            nameof(Department),
            department.Id,
            null,
            $"Name={department.Name};IsActive={department.IsActive}",
            null,
            ct);

        return _mapper.Map<DepartmentDto>(department);
    }

    public async Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var department = await _unitOfWork.Departments.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Department), id);

        var oldValues = $"Name={department.Name};Description={department.Description};IsActive={department.IsActive}";

        if (request.Name != null)
        {
            var name = request.Name.Trim();
            await EnsureUniqueNameAsync(name, id, ct);
            department.Name = name;
        }

        if (request.ClearDescription)
        {
            department.Description = null;
        }
        else if (request.Description != null)
        {
            department.Description = NormalizeOptional(request.Description);
        }

        if (request.IsActive.HasValue)
        {
            if (!request.IsActive.Value && department.IsActive)
            {
                var hasActiveUsers = await _unitOfWork.Users.AnyAsync(
                    user => user.DepartmentId == department.Id && user.Status == UserStatus.Active,
                    ct);
                var hasActiveCategories = await _unitOfWork.FeedbackCategories.AnyAsync(
                    category => category.DepartmentId == department.Id && category.IsActive,
                    ct);

                if (hasActiveUsers || hasActiveCategories)
                {
                    throw new BusinessRuleException("Move or disable active users and categories before disabling this department.");
                }
            }

            department.IsActive = request.IsActive.Value;
        }

        _unitOfWork.Departments.Update(department);
        await _unitOfWork.SaveChangesAsync(ct);
        await _auditLogService.LogAsync(
            actorUserId,
            AuditAction.Update,
            nameof(Department),
            department.Id,
            oldValues,
            $"Name={department.Name};Description={department.Description};IsActive={department.IsActive}",
            null,
            ct);

        return _mapper.Map<DepartmentDto>(department);
    }

    private async Task EnsureUniqueNameAsync(string name, Guid? excludedId, CancellationToken ct)
    {
        var normalizedName = name.ToLowerInvariant();
        var exists = await _unitOfWork.Departments.AnyAsync(
            department => department.Name.ToLower() == normalizedName &&
                          (!excludedId.HasValue || department.Id != excludedId.Value),
            ct);

        if (exists)
        {
            throw new ConflictException($"Department name '{name}' already exists.");
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
