using CFMS.Application.DTOs.Categories;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CFMS.Application.Common.Models;

namespace CFMS.Api.Controllers;

[Authorize]
[Tags("Feedback Categories")]
public class CategoriesController : BaseController
{
    private readonly IFeedbackCategoryService _categoryService;

    public CategoriesController(IFeedbackCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActive(CancellationToken ct)
        => OkResponse(await _categoryService.GetActiveAsync(ct));

    [HttpGet("~/api/admin/categories")]
    [Authorize(Roles = RoleNames.SystemAdmin)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => OkResponse(await _categoryService.GetAllAsync(ct));

    [HttpPost("~/api/admin/categories")]
    [Authorize(Roles = RoleNames.SystemAdmin)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        var result = await _categoryService.CreateAsync(request, CurrentUserId, ct);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<CategoryDto>.Ok(result));
    }

    [HttpPatch("~/api/admin/categories/{id:guid}")]
    [Authorize(Roles = RoleNames.SystemAdmin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
        => OkResponse(await _categoryService.UpdateAsync(id, request, CurrentUserId, ct), "Category updated.");
}
