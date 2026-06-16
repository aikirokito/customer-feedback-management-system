using CFMS.Application.DTOs.Feedback;
using CFMS.Application.DTOs.Responses;

namespace CFMS.Application.Services.Interfaces;

public interface IFeedbackResponseService
{
    Task<IEnumerable<FeedbackResponseDto>> GetResponsesForFeedbackAsync(Guid feedbackId, Guid requestingUserId, CancellationToken ct = default);
    Task<FeedbackResponseDto> CreateResponseAsync(CreateResponseRequest request, Guid respondedByUserId, CancellationToken ct = default);
    Task<FeedbackResponseDto> UpdateResponseAsync(Guid responseId, UpdateResponseRequest request, Guid requestingUserId, CancellationToken ct = default);
    Task DeleteResponseAsync(Guid responseId, Guid requestingUserId, CancellationToken ct = default);
}
