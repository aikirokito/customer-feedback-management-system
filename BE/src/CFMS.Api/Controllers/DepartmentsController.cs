using CFMS.Application.DTOs.Departments;
using CFMS.Domain.Constants;
using CFMS.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CFMS.Application.Common.Models;

namespace CFMS.Api.Controllers;

[Authorize]
[Tags("Departments")]
public class DepartmentsController : BaseController
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    [Authorize(Roles = $"{RoleNames.DepartmentManager},{RoleNames.SystemAdmin}")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
        => OkResponse(await _departmentService.GetActiveAsync(ct));

    [HttpGet("~/api/admin/departments")]
    [Authorize(Roles = RoleNames.SystemAdmin)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => OkResponse(await _departmentService.GetAllAsync(ct));

    [HttpPost("~/api/admin/departments")]
    [Authorize(Roles = RoleNames.SystemAdmin)]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request, CancellationToken ct)
    {
        var result = await _departmentService.CreateAsync(request, CurrentUserId, ct);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<DepartmentDto>.Ok(result));
    }

    [HttpPatch("~/api/admin/departments/{id:guid}")]
    [Authorize(Roles = RoleNames.SystemAdmin)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateDepartmentRequest request, CancellationToken ct)
        => OkResponse(await _departmentService.UpdateAsync(id, request, CurrentUserId, ct), "Department updated.");
}
