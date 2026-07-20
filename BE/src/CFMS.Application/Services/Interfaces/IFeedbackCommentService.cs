using CFMS.Application.DTOs.Comments;

namespace CFMS.Application.Services.Interfaces;

public interface IFeedbackCommentService
{
    Task<CommentDto> CreateCommentAsync(CreateCommentRequest request, Guid authorUserId, CancellationToken ct = default);
    Task<CommentDto> UpdateCommentAsync(Guid feedbackId, Guid commentId, UpdateCommentRequest request, Guid requestingUserId, CancellationToken ct = default);
    Task DeleteCommentAsync(Guid feedbackId, Guid commentId, Guid requestingUserId, CancellationToken ct = default);
    Task<IEnumerable<CommentDto>> GetCommentsForFeedbackAsync(Guid feedbackId, Guid requestingUserId, CancellationToken ct = default);
}
