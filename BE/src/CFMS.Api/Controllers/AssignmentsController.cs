using CFMS.Application.DTOs.Assignments;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CFMS.Api.Controllers;

/// <summary>
/// Feedback assignment management. Manager and Admin only.
/// </summary>
[Authorize]
[Tags("Feedback Assignments")]
public class AssignmentsController : BaseController
{
    private readonly IFeedbackAssignmentService _assignmentService;

    public AssignmentsController(IFeedbackAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpPatch("~/api/feedback/{feedbackId:guid}/assign")]
    [Authorize(Roles = $"{RoleNames.DepartmentManager},{RoleNames.SystemAdmin}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> AssignFeedback([FromRoute] Guid feedbackId, [FromBody] AssignFeedbackRequest request, CancellationToken ct)
    {
        request.FeedbackId = feedbackId;
        var result = await _assignmentService.AssignFeedbackAsync(request, CurrentUserId, ct);
        return OkResponse(result, "Feedback assigned.");
    }

    [HttpPatch("~/api/feedback/{feedbackId:guid}/reassign")]
    [Authorize(Roles = $"{RoleNames.DepartmentManager},{RoleNames.SystemAdmin}")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ReassignFeedback([FromRoute] Guid feedbackId, [FromBody] AssignFeedbackRequest request, CancellationToken ct)
    {
        request.FeedbackId = feedbackId;
        var result = await _assignmentService.AssignFeedbackAsync(request, CurrentUserId, ct);
        return OkResponse(result, "Feedback reassigned.");
    }

    [HttpGet("~/api/feedback/{feedbackId:guid}/assignments")]
    [Authorize(Roles = $"{RoleNames.SupportStaff},{RoleNames.DepartmentManager},{RoleNames.SystemAdmin}")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetAssignmentHistory([FromRoute] Guid feedbackId, CancellationToken ct)
    {
        var result = await _assignmentService.GetAssignmentHistoryAsync(feedbackId, CurrentUserId, ct);
        return OkResponse(result);
    }

    [HttpDelete("~/api/feedback/{feedbackId:guid}/assignments")]
    [Authorize(Roles = $"{RoleNames.DepartmentManager},{RoleNames.SystemAdmin}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> UnassignFeedback([FromRoute] Guid feedbackId, CancellationToken ct)
    {
        await _assignmentService.UnassignFeedbackAsync(feedbackId, CurrentUserId, ct);
        return NoContentResponse();
    }
}
