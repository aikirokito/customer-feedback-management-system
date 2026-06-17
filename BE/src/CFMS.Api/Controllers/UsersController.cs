using AutoMapper;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.DTOs.Users;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Constants;
using CFMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CFMS.Api.Controllers;

/// <summary>
/// User management. Admins manage all users; users manage their own profile.
/// </summary>
[Authorize]
[Tags("Users")]
public class UsersController : BaseController
{
    private readonly IUserService _userService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UsersController(IUserService userService, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _userService = userService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    [HttpGet]
    [Route("~/api/admin/users")]
    [Authorize(Roles = RoleNames.SystemAdmin)]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] UserRole? role = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _userService.GetUsersAsync(page, pageSize, role, search, ct);
        return OkResponse(result);
    }

    [HttpGet("{id:guid}", Name = "GetUserById")]
    [Authorize(Roles = RoleNames.SystemAdmin)]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUserById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _userService.GetUserByIdAsync(id, ct);
        return OkResponse(result);
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDetailDto), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var result = await _userService.GetProfileAsync(CurrentUserId, ct);
        return OkResponse(result);
    }

    [HttpPut("me")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var result = await _userService.UpdateUserAsync(CurrentUserId, request, ct);
        return OkResponse(result, "Profile updated.");
    }

    [HttpPatch("{id:guid}/role")]
    [Route("~/api/admin/users/{id:guid}/role")]
    [Authorize(Roles = RoleNames.SystemAdmin)]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> UpdateRole([FromRoute] Guid id, [FromBody] UpdateUserRoleRequest request, CancellationToken ct)
    {
        await _userService.UpdateUserRoleAsync(id, request, ct);
        return NoContentResponse();
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = RoleNames.SystemAdmin)]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeactivateUser([FromRoute] Guid id, CancellationToken ct)
    {
        await _userService.DeactivateUserAsync(id, ct);
        return NoContentResponse();
    }

    [HttpPatch("{id:guid}/reactivate")]
    [Authorize(Roles = RoleNames.SystemAdmin)]
    [ProducesResponseType(204)]
    public async Task<IActionResult> ReactivateUser([FromRoute] Guid id, CancellationToken ct)
    {
        await _userService.ReactivateUserAsync(id, ct);
        return NoContentResponse();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleNames.SystemAdmin)]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteUser([FromRoute] Guid id, CancellationToken ct)
    {
        await _userService.DeleteUserAsync(id, ct);
        return NoContentResponse();
    }

    /// <summary>
    /// Admin alias: toggle user active status via { "isActive": true/false }.
    /// </summary>
    [HttpPatch]
    [Route("~/api/admin/users/{id:guid}/status")]
    [Authorize(Roles = RoleNames.SystemAdmin)]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateUserStatus([FromRoute] Guid id, [FromBody] UpdateUserStatusRequest request, CancellationToken ct)
    {
        if (request.IsActive)
            await _userService.ReactivateUserAsync(id, ct);
        else
            await _userService.DeactivateUserAsync(id, ct);

        return NoContentResponse();
    }

    /// <summary>
    /// Returns active Support Staff users for assignment dropdowns.
    /// </summary>
    [HttpGet("support-staff")]
    [Authorize(Roles = $"{RoleNames.DepartmentManager},{RoleNames.SystemAdmin}")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetSupportStaff(CancellationToken ct)
    {
        var users = await _unitOfWork.Users.GetByRoleAsync(UserRole.SupportStaff, ct);
        var activeUsers = users.Where(u => u.Status == Domain.Enums.UserStatus.Active);
        
        var dtos = activeUsers.Select(u => new UserListItemDto
        {
            Id = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            AvatarUrl = u.AvatarUrl,
            Role = u.Role,
            IsActive = u.Status == Domain.Enums.UserStatus.Active,
            CreatedAtUtc = u.CreatedAtUtc
        }).ToList();
        
        return OkResponse(dtos);
    }
}
