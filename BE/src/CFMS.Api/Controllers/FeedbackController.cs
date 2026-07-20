using CFMS.Application.Common.Models;
using CFMS.Application.DTOs.Feedback;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CFMS.Api.Controllers;

/// <summary>
/// Feedback CRUD, status management, and file attachment endpoints.
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
    [Authorize(Roles = $"{RoleNames.SupportStaff},{RoleNames.DepartmentManager},{RoleNames.SystemAdmin}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> UpdateFeedback([FromRoute] Guid id, [FromBody] UpdateFeedbackRequest request, CancellationToken ct)
    {
        var result = await _feedbackService.UpdateFeedbackAsync(id, request, CurrentUserId, ct);
        return OkResponse(result, "Feedback updated.");
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = $"{RoleNames.SupportStaff},{RoleNames.DepartmentManager},{RoleNames.SystemAdmin}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ChangeStatus([FromRoute] Guid id, [FromBody] ChangeFeedbackStatusRequest request, CancellationToken ct)
    {
        await _feedbackService.ChangeStatusAsync(id, request, CurrentUserId, ct);
        return NoContentResponse();
    }

    [HttpPatch("{id:guid}/priority")]
    [Authorize(Roles = $"{RoleNames.SupportStaff},{RoleNames.DepartmentManager},{RoleNames.SystemAdmin}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdatePriority([FromRoute] Guid id, [FromBody] UpdateFeedbackPriorityRequest request, CancellationToken ct)
    {
        var result = await _feedbackService.UpdatePriorityAsync(id, request, CurrentUserId, ct);
        return OkResponse(result, "Feedback priority updated.");
    }

    [HttpPatch("{id:guid}/rating")]
    [Authorize(Roles = RoleNames.Customer)]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> RateFeedback([FromRoute] Guid id, [FromBody] RateFeedbackRequest request, CancellationToken ct)
    {
        var result = await _feedbackService.RateFeedbackAsync(id, request, CurrentUserId, ct);
        return OkResponse(result, "Feedback rating updated.");
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> DeleteFeedback([FromRoute] Guid id, CancellationToken ct)
    {
        await _feedbackService.DeleteFeedbackAsync(id, CurrentUserId, ct);
        return NoContentResponse();
    }

    [HttpPost("{id:guid}/attachments")]
    [ProducesResponseType(typeof(FeedbackAttachmentDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UploadAttachment([FromRoute] Guid id, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var fileInput = new UploadedFileInput
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            Content = stream
        };

        var result = await _feedbackService.UploadAttachmentAsync(id, fileInput, CurrentUserId, ct);
        return CreatedResponse("GetFeedbackById", new { id }, result);
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteAttachment([FromRoute] Guid id, [FromRoute] Guid attachmentId, CancellationToken ct)
    {
        await _feedbackService.DeleteAttachmentAsync(id, attachmentId, CurrentUserId, ct);
        return NoContentResponse();
    }
}
