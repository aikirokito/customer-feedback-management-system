using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.DTOs.Categories;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;

namespace CFMS.Application.Services.Implementations;

public class FeedbackCategoryService : IFeedbackCategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuditLogService _auditLogService;

    public FeedbackCategoryService(IUnitOfWork unitOfWork, IMapper mapper, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _auditLogService = auditLogService;
    }

    public async Task<IEnumerable<CategoryDto>> GetActiveAsync(CancellationToken ct = default)
        => _mapper.Map<IEnumerable<CategoryDto>>(await _unitOfWork.FeedbackCategories.GetActiveAsync(ct));

    public async Task<IEnumerable<CategoryDto>> GetAllAsync(CancellationToken ct = default)
        => _mapper.Map<IEnumerable<CategoryDto>>(await _unitOfWork.FeedbackCategories.GetAllAsync(ct));

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var normalizedName = request.Name.Trim();
        if (await _unitOfWork.FeedbackCategories.GetByNameAsync(normalizedName, ct) != null)
        {
            throw new ConflictException($"Category '{normalizedName}' already exists.");
        }

        var department = await GetActiveDepartmentAsync(request.DepartmentId, ct);

        var category = new FeedbackCategoryEntity
        {
            Name = normalizedName,
            Description = request.Description?.Trim(),
            DepartmentId = request.DepartmentId,
            Department = department,
            IsActive = true
        };

        await _unitOfWork.FeedbackCategories.AddAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await _auditLogService.LogAsync(actorUserId, AuditAction.Create, nameof(FeedbackCategoryEntity), category.Id, null,
            $"Name={category.Name};IsActive=True;DepartmentId={category.DepartmentId}", null, ct);

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var category = await _unitOfWork.FeedbackCategories.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(FeedbackCategoryEntity), id);
        var oldValues = $"Name={category.Name};Description={category.Description};IsActive={category.IsActive};DepartmentId={category.DepartmentId}";

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var existing = await _unitOfWork.FeedbackCategories.GetByNameAsync(request.Name, ct);
            if (existing != null && existing.Id != id)
            {
                throw new ConflictException($"Category '{request.Name.Trim()}' already exists.");
            }
            category.Name = request.Name.Trim();
        }

        if (request.Description != null)
        {
            category.Description = request.Description.Trim();
        }

        if (request.IsActive.HasValue)
        {
            category.IsActive = request.IsActive.Value;
        }

        if (request.ClearDepartment)
        {
            category.DepartmentId = null;
            category.Department = null;
        }
        else if (request.DepartmentId.HasValue)
        {
            var department = await GetActiveDepartmentAsync(request.DepartmentId, ct);
            category.DepartmentId = request.DepartmentId;
            category.Department = department;
        }

        _unitOfWork.FeedbackCategories.Update(category);
        await _unitOfWork.SaveChangesAsync(ct);
        await _auditLogService.LogAsync(actorUserId, AuditAction.Update, nameof(FeedbackCategoryEntity), category.Id, oldValues,
            $"Name={category.Name};Description={category.Description};IsActive={category.IsActive};DepartmentId={category.DepartmentId}", null, ct);

        return _mapper.Map<CategoryDto>(category);
    }

    private async Task<Department?> GetActiveDepartmentAsync(Guid? departmentId, CancellationToken ct)
    {
        if (!departmentId.HasValue)
        {
            return null;
        }

        var department = await _unitOfWork.Departments.GetByIdAsync(departmentId.Value, ct)
            ?? throw new NotFoundException(nameof(Department), departmentId.Value);
        if (!department.IsActive)
        {
            throw new BusinessRuleException("Disabled departments cannot be assigned to a category.");
        }

        return department;
    }
}
