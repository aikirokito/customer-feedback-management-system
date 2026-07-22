using CFMS.Application.DTOs.Responses;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CFMS.Api.Controllers;

/// <summary>
/// Manage official responses to feedback tickets.
/// </summary>
[Authorize]
[Tags("Feedback Responses")]
[Route("~/api/feedback/{feedbackId:guid}/responses")]
public class ResponsesController : BaseController
{
    private readonly IFeedbackResponseService _responseService;

    public ResponsesController(IFeedbackResponseService responseService)
    {
        _responseService = responseService;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetResponses([FromRoute] Guid feedbackId, CancellationToken ct)
    {
        var result = await _responseService.GetResponsesForFeedbackAsync(feedbackId, CurrentUserId, ct);
        return OkResponse(result);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.SupportStaff)]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateResponse([FromRoute] Guid feedbackId, [FromBody] CreateResponseRequest request, CancellationToken ct)
    {
        request.FeedbackId = feedbackId;
        var result = await _responseService.CreateResponseAsync(request, CurrentUserId, ct);
        return CreatedResponse("GetFeedbackById", new { id = feedbackId }, result);
    }

    [HttpPut("{responseId:guid}")]
    [Authorize(Roles = RoleNames.SupportStaff)]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> UpdateResponse([FromRoute] Guid feedbackId, [FromRoute] Guid responseId, [FromBody] UpdateResponseRequest request, CancellationToken ct)
    {
        var result = await _responseService.UpdateResponseAsync(feedbackId, responseId, request, CurrentUserId, ct);
        return OkResponse(result, "Response updated.");
    }

    [HttpDelete("{responseId:guid}")]
    [Authorize(Roles = RoleNames.SupportStaff)]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteResponse([FromRoute] Guid feedbackId, [FromRoute] Guid responseId, CancellationToken ct)
    {
        await _responseService.DeleteResponseAsync(feedbackId, responseId, CurrentUserId, ct);
        return NoContentResponse();
    }
}
