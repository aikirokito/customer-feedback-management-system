using CFMS.Application.DTOs.Comments;
using CFMS.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CFMS.Api.Controllers;

/// <summary>
/// Visible discussion comments on feedback.
/// </summary>
[Authorize]
[Tags("Feedback Comments")]
[Route("~/api/feedback/{feedbackId:guid}/comments")]
public class CommentsController : BaseController
{
    private readonly IFeedbackCommentService _commentService;

    public CommentsController(IFeedbackCommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetComments([FromRoute] Guid feedbackId, CancellationToken ct)
    {
        var result = await _commentService.GetCommentsForFeedbackAsync(feedbackId, CurrentUserId, ct);
        return OkResponse(result);
    }

    [HttpPost]
    [ProducesResponseType(201)]
    public async Task<IActionResult> CreateComment([FromRoute] Guid feedbackId, [FromBody] CreateCommentRequest request, CancellationToken ct)
    {
        request.FeedbackId = feedbackId;
        var result = await _commentService.CreateCommentAsync(request, CurrentUserId, ct);
        return CreatedResponse("GetFeedbackById", new { id = feedbackId }, result);
    }

    [HttpPut("{commentId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> UpdateComment([FromRoute] Guid feedbackId, [FromRoute] Guid commentId, [FromBody] UpdateCommentRequest request, CancellationToken ct)
    {
        var result = await _commentService.UpdateCommentAsync(feedbackId, commentId, request, CurrentUserId, ct);
        return OkResponse(result, "Comment updated.");
    }

    [HttpDelete("{commentId:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteComment([FromRoute] Guid feedbackId, [FromRoute] Guid commentId, CancellationToken ct)
    {
        await _commentService.DeleteCommentAsync(feedbackId, commentId, CurrentUserId, ct);
        return NoContentResponse();
    }
}
