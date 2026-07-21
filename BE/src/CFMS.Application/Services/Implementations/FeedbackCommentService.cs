using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.DTOs.Comments;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;

namespace CFMS.Application.Services.Implementations;

public class FeedbackCommentService : IFeedbackCommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;

    public FeedbackCommentService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        INotificationService notificationService,
        IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
    }

    public async Task<CommentDto> CreateCommentAsync(CreateCommentRequest request, Guid authorUserId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(request.FeedbackId, ct)
            ?? throw new NotFoundException(nameof(Feedback), request.FeedbackId);
        var author = await GetUserAsync(authorUserId, ct);
        EnsureCanComment(author, feedback, authorUserId);

        if (feedback.Status is FeedbackStatus.Closed or FeedbackStatus.Cancelled)
        {
            throw new BusinessRuleException("Comments cannot be added to closed or rejected feedback.");
        }

        FeedbackComment? parentComment = null;
        if (request.ParentCommentId.HasValue)
        {
            parentComment = await _unitOfWork.Feedbacks.GetCommentByIdAsync(request.ParentCommentId.Value, ct)
                ?? throw new NotFoundException(nameof(FeedbackComment), request.ParentCommentId.Value);

            if (parentComment.FeedbackId != request.FeedbackId)
            {
                throw new BusinessRuleException("Parent comment must belong to the same feedback.");
            }
        }

        var comment = new FeedbackComment
        {
            FeedbackId = request.FeedbackId,
            AuthorUserId = authorUserId,
            Content = request.Content.Trim(),
            ParentCommentId = parentComment?.Id,
            ParentComment = parentComment,
            AuthorUser = author
        };

        feedback.Comments.Add(comment);
        feedback.UpdatedAtUtc = DateTime.UtcNow;
        _unitOfWork.Feedbacks.Update(feedback);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(authorUserId, AuditAction.Create, nameof(FeedbackComment), comment.Id, null, $"Comment added to feedback {feedback.Id}", null, ct);

        if (author.Role == UserRole.Customer && feedback.AssignedToUserId.HasValue)
        {
            await SafeNotifyAsync(feedback.AssignedToUserId.Value, NotificationType.FeedbackCommentAdded, "Customer replied", $"Customer replied on feedback '{feedback.Title}'.", feedback.Id, nameof(Feedback), ct);
        }
        else if (author.Role != UserRole.Customer)
        {
            await SafeNotifyAsync(feedback.SubmittedByUserId, NotificationType.FeedbackCommentAdded, "New comment", $"A new comment was added to your feedback '{feedback.Title}'.", feedback.Id, nameof(Feedback), ct);
        }

        return _mapper.Map<CommentDto>(comment);
    }

    public async Task<CommentDto> UpdateCommentAsync(Guid feedbackId, Guid commentId, UpdateCommentRequest request, Guid requestingUserId, CancellationToken ct = default)
    {
        var comment = await _unitOfWork.Feedbacks.GetCommentByIdAsync(commentId, ct)
            ?? throw new NotFoundException(nameof(FeedbackComment), commentId);

        if (comment.FeedbackId != feedbackId)
        {
            throw new NotFoundException(nameof(FeedbackComment), commentId);
        }

        var user = await GetUserAsync(requestingUserId, ct);
        EnsureCanComment(user, comment.Feedback, requestingUserId);

        if (user.Role != UserRole.SystemAdmin && comment.Feedback.Status is FeedbackStatus.Closed or FeedbackStatus.Cancelled)
        {
            throw new BusinessRuleException("Comments on closed or rejected feedback cannot be changed.");
        }

        if (comment.AuthorUserId != requestingUserId && user.Role != UserRole.SystemAdmin)
        {
            throw new ForbiddenException("Only the comment author or a system admin can update this comment.");
        }

        comment.Content = request.Content.Trim();
        comment.UpdatedAtUtc = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(requestingUserId, AuditAction.Update, nameof(FeedbackComment), comment.Id, null, "Comment updated", null, ct);
        return _mapper.Map<CommentDto>(comment);
    }

    public async Task DeleteCommentAsync(Guid feedbackId, Guid commentId, Guid requestingUserId, CancellationToken ct = default)
    {
        var comment = await _unitOfWork.Feedbacks.GetCommentByIdAsync(commentId, ct)
            ?? throw new NotFoundException(nameof(FeedbackComment), commentId);

        if (comment.FeedbackId != feedbackId)
        {
            throw new NotFoundException(nameof(FeedbackComment), commentId);
        }

        var user = await GetUserAsync(requestingUserId, ct);
        EnsureCanComment(user, comment.Feedback, requestingUserId);

        if (user.Role != UserRole.SystemAdmin && comment.Feedback.Status is FeedbackStatus.Closed or FeedbackStatus.Cancelled)
        {
            throw new BusinessRuleException("Comments on closed or rejected feedback cannot be deleted.");
        }

        if (comment.AuthorUserId != requestingUserId && user.Role != UserRole.SystemAdmin)
        {
            throw new ForbiddenException("Only the comment author or a system admin can delete this comment.");
        }

        comment.IsDeleted = true;
        comment.DeletedAtUtc = DateTime.UtcNow;
        comment.DeletedByUserId = requestingUserId;
        comment.UpdatedAtUtc = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(requestingUserId, AuditAction.Delete, nameof(FeedbackComment), comment.Id, null, "Comment deleted", null, ct);
    }

    public async Task<IEnumerable<CommentDto>> GetCommentsForFeedbackAsync(Guid feedbackId, Guid requestingUserId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(feedbackId, ct)
            ?? throw new NotFoundException(nameof(Feedback), feedbackId);
        var user = await GetUserAsync(requestingUserId, ct);
        EnsureCanComment(user, feedback, requestingUserId);

        return _mapper.Map<IEnumerable<CommentDto>>(feedback.Comments
            .Where(c => !c.IsDeleted && c.ParentCommentId == null)
            .OrderBy(c => c.CreatedAtUtc));
    }

    private async Task<User> GetUserAsync(Guid userId, CancellationToken ct)
        => await _unitOfWork.Users.GetByIdAsync(userId, ct)
            ?? throw new UnauthorizedException("Authenticated user was not found.");

    private static void EnsureCanComment(User user, Feedback feedback, Guid userId)
    {
        if (user.Role == UserRole.Customer && feedback.SubmittedByUserId != userId)
            throw new ForbiddenException("Customers can only comment on their own feedback.");
        if (user.Role == UserRole.SupportStaff && feedback.AssignedToUserId != userId)
            throw new ForbiddenException("Support staff can only comment on assigned feedback.");
        if (user.Role == UserRole.DepartmentManager &&
            (!user.DepartmentId.HasValue || feedback.DepartmentId != user.DepartmentId))
            throw new ForbiddenException("Department Managers can only comment on feedback in their department.");
    }

    private async Task SafeNotifyAsync(Guid userId, NotificationType type, string title, string message, Guid? entityId, string? entityType, CancellationToken ct)
    {
        try
        {
            await _notificationService.SendNotificationAsync(userId, type, title, message, entityId, entityType, ct);
        }
        catch
        {
            // Notification failures are non-blocking for comments.
        }
    }
}
