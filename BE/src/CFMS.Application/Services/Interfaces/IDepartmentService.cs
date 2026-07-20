using CFMS.Application.DTOs.Departments;

namespace CFMS.Application.Services.Interfaces;

public interface IDepartmentService
{
    Task<IEnumerable<DepartmentDto>> GetActiveAsync(CancellationToken ct = default);
    Task<IEnumerable<DepartmentDto>> GetAllAsync(CancellationToken ct = default);
    Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentRequest request, Guid actorUserId, CancellationToken ct = default);
}
