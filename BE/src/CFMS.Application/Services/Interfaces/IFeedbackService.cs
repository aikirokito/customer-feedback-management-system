using CFMS.Application.Common.Models;
using CFMS.Application.DTOs.Feedback;

namespace CFMS.Application.Services.Interfaces;

public interface IFeedbackService
{
    Task<PagedResult<FeedbackListItemDto>> GetFeedbacksAsync(FeedbackFilterRequest filter, Guid requestingUserId, CancellationToken ct = default);
    Task<FeedbackDetailDto> GetFeedbackByIdAsync(Guid id, Guid requestingUserId, CancellationToken ct = default);
    Task<FeedbackDetailDto> CreateFeedbackAsync(CreateFeedbackRequest request, Guid submittedByUserId, CancellationToken ct = default);
    Task<FeedbackDetailDto> UpdateFeedbackAsync(Guid id, UpdateFeedbackRequest request, Guid requestingUserId, CancellationToken ct = default);
    Task<FeedbackDetailDto> UpdatePriorityAsync(Guid id, UpdateFeedbackPriorityRequest request, Guid requestingUserId, CancellationToken ct = default);
    Task ChangeStatusAsync(Guid id, ChangeFeedbackStatusRequest request, Guid requestingUserId, CancellationToken ct = default);
    Task CancelFeedbackAsync(Guid id, Guid customerId, CancellationToken ct = default);
    Task DeleteFeedbackAsync(Guid id, Guid requestingUserId, CancellationToken ct = default);
    Task<FeedbackAttachmentDto> UploadAttachmentAsync(Guid feedbackId, UploadedFileInput file, Guid uploadedByUserId, CancellationToken ct = default);
    Task DeleteAttachmentAsync(Guid feedbackId, Guid attachmentId, Guid requestingUserId, CancellationToken ct = default);
}
