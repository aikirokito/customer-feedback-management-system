using CFMS.Domain.Entities;
using CFMS.Domain.Enums;

namespace CFMS.Application.Common.Interfaces;

/// <summary>
/// Feedback-specific repository with paging, filtering, details, and child entity lookups.
/// </summary>
public interface IFeedbackRepository : IRepository<Feedback>
{
    Task<(IEnumerable<Feedback> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        FeedbackStatus? status = null,
        Guid? categoryId = null,
        FeedbackPriority? priority = null,
        Guid? submittedByUserId = null,
        Guid? assignedToUserId = null,
        Guid? departmentId = null,
        string? searchTerm = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default);

    Task<Feedback?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    Task<FeedbackAttachment?> GetAttachmentByIdAsync(Guid attachmentId, CancellationToken ct = default);
    Task<FeedbackResponse?> GetResponseByIdAsync(Guid responseId, CancellationToken ct = default);
    Task<FeedbackComment?> GetCommentByIdAsync(Guid commentId, CancellationToken ct = default);
    void RemoveAttachment(FeedbackAttachment attachment);

    Task<IEnumerable<Feedback>> GetReportFeedbacksAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? categoryId = null,
        FeedbackStatus? status = null,
        Guid? assignedToUserId = null,
        Guid? departmentId = null,
        CancellationToken ct = default);
}
