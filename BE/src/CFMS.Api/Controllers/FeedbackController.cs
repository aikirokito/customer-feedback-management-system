using CFMS.Application.Common.Models;
using CFMS.Application.DTOs.Feedback;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CFMS.Api.Controllers;

/// <summary>
/// Feedback CRUD and status management endpoints.
/// </summary>
[Authorize]
[Tags("Feedback")]
public class FeedbackController : BaseController
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetFeedbacks([FromQuery] FeedbackFilterRequest filter, CancellationToken ct)
    {
        var result = await _feedbackService.GetFeedbacksAsync(filter, CurrentUserId, ct);
        return OkResponse(result);
    }

    [HttpGet("my")]
    [Authorize(Roles = RoleNames.Customer)]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetMyFeedbacks([FromQuery] FeedbackFilterRequest filter, CancellationToken ct)
    {
        var result = await _feedbackService.GetFeedbacksAsync(filter, CurrentUserId, ct);
        return OkResponse(result);
    }

    [HttpGet("{id:guid}", Name = "GetFeedbackById")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetFeedbackById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _feedbackService.GetFeedbackByIdAsync(id, CurrentUserId, ct);
        return OkResponse(result);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Customer)]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackRequest request, CancellationToken ct)
    {
        var result = await _feedbackService.CreateFeedbackAsync(request, CurrentUserId, ct);
        return CreatedResponse("GetFeedbackById", new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.Customer)]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> UpdateFeedback([FromRoute] Guid id, [FromBody] UpdateFeedbackRequest request, CancellationToken ct)
    {
        var result = await _feedbackService.UpdateFeedbackAsync(id, request, CurrentUserId, ct);
        return OkResponse(result, "Feedback updated.");
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = $"{RoleNames.SupportStaff},{RoleNames.DepartmentManager}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ChangeStatus([FromRoute] Guid id, [FromBody] ChangeFeedbackStatusRequest request, CancellationToken ct)
    {
        await _feedbackService.ChangeStatusAsync(id, request, CurrentUserId, ct);
        return NoContentResponse();
    }

    /// <summary>Danh sách phản hồi được giao cho Staff đang đăng nhập.</summary>
    [HttpGet("~/api/staff/feedback")]
    [Authorize(Roles = RoleNames.SupportStaff)]
    public async Task<IActionResult> GetAssignedFeedback([FromQuery] FeedbackFilterRequest filter, CancellationToken ct)
    {
        var result = await _feedbackService.GetFeedbacksAsync(filter, CurrentUserId, ct);
        return OkResponse(result);
    }

    /// <summary>Manager xem, tìm kiếm và lọc toàn bộ phản hồi thuộc phạm vi quản lý.</summary>
    [HttpGet("~/api/manager/feedback")]
    [Authorize(Roles = RoleNames.DepartmentManager)]
    public async Task<IActionResult> GetManagerFeedback([FromQuery] FeedbackFilterRequest filter, CancellationToken ct)
    {
        var result = await _feedbackService.GetFeedbacksAsync(filter, CurrentUserId, ct);
        return OkResponse(result);
    }

    /// <summary>Customer hủy phản hồi của mình khi phản hồi vẫn còn SUBMITTED.</summary>
    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = RoleNames.Customer)]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> CancelFeedback([FromRoute] Guid id, CancellationToken ct)
    {
        await _feedbackService.CancelFeedbackAsync(id, CurrentUserId, ct);
        return NoContentResponse();
    }

    /// <summary>Staff bắt đầu xử lý phản hồi đã được giao: ASSIGNED → IN_PROGRESS.</summary>
    [HttpPatch("~/api/staff/feedback/{id:guid}/start")]
    [Authorize(Roles = RoleNames.SupportStaff)]
    public async Task<IActionResult> StartProcessing([FromRoute] Guid id, CancellationToken ct)
    {
        await _feedbackService.ChangeStatusAsync(id,
            new ChangeFeedbackStatusRequest { NewStatus = CFMS.Domain.Enums.FeedbackStatus.InProgress }, CurrentUserId, ct);
        return NoContentResponse();
    }

    /// <summary>Staff đánh dấu đã giải quyết: IN_PROGRESS → RESOLVED.</summary>
    [HttpPatch("~/api/staff/feedback/{id:guid}/resolve")]
    [Authorize(Roles = RoleNames.SupportStaff)]
    public async Task<IActionResult> ResolveFeedback([FromRoute] Guid id, CancellationToken ct)
    {
        await _feedbackService.ChangeStatusAsync(id,
            new ChangeFeedbackStatusRequest { NewStatus = CFMS.Domain.Enums.FeedbackStatus.Resolved }, CurrentUserId, ct);
        return NoContentResponse();
    }

    /// <summary>Manager xác minh và đóng phản hồi đã giải quyết.</summary>
    [HttpPatch("~/api/manager/feedback/{id:guid}/close")]
    [Authorize(Roles = RoleNames.DepartmentManager)]
    public async Task<IActionResult> CloseFeedback([FromRoute] Guid id, [FromBody] CloseFeedbackRequest request, CancellationToken ct)
    {
        await _feedbackService.ChangeStatusAsync(id,
            new ChangeFeedbackStatusRequest { NewStatus = CFMS.Domain.Enums.FeedbackStatus.Closed, Reason = request.Reason }, CurrentUserId, ct);
        return NoContentResponse();
    }

    [HttpPatch("{id:guid}/priority")]
    [Authorize(Roles = $"{RoleNames.SupportStaff},{RoleNames.DepartmentManager}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdatePriority([FromRoute] Guid id, [FromBody] UpdateFeedbackPriorityRequest request, CancellationToken ct)
    {
        var result = await _feedbackService.UpdatePriorityAsync(id, request, CurrentUserId, ct);
        return OkResponse(result, "Feedback priority updated.");
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> DeleteFeedback([FromRoute] Guid id, CancellationToken ct)
    {
        await _feedbackService.DeleteFeedbackAsync(id, CurrentUserId, ct);
        return NoContentResponse();
    }

}
