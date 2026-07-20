using CFMS.Application.DTOs.Categories;

namespace CFMS.Application.Services.Interfaces;

public interface IFeedbackCategoryService
{
    Task<IEnumerable<CategoryDto>> GetActiveAsync(CancellationToken ct = default);
    Task<IEnumerable<CategoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, Guid actorUserId, CancellationToken ct = default);
}
