using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.Common.Models;
using CFMS.Application.DTOs.Feedback;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Constants;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using CFMS.Domain.Rules;

namespace CFMS.Application.Services.Implementations;

public class FeedbackService : IFeedbackService
{
    private const string AttachmentBucketName = "cfms-attachments";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ISupabaseStorageService _storageService;

    public FeedbackService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        INotificationService notificationService,
        IAuditLogService auditLogService,
        ISupabaseStorageService storageService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
        _storageService = storageService;
    }

    /// <summary>
    /// Lấy danh sách phản hồi và tự giới hạn dữ liệu theo vai trò người đang đăng nhập:
    /// Customer chỉ thấy phiếu của mình, Staff chỉ thấy phiếu được giao, Manager thấy toàn bộ.
    /// </summary>
    public async Task<PagedResult<FeedbackListItemDto>> GetFeedbacksAsync(FeedbackFilterRequest filter, Guid requestingUserId, CancellationToken ct = default)
    {
        var user = await GetCurrentUserAsync(requestingUserId, ct);
        var page = Math.Max(filter.Page, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        Guid? submittedByUserId = user.Role == UserRole.Customer ? requestingUserId : null;
        Guid? assignedToUserId = user.Role == UserRole.SupportStaff ? requestingUserId : filter.AssignedToUserId;

        var (items, totalCount) = await _unitOfWork.Feedbacks.GetPagedAsync(
            page,
            pageSize,
            filter.Status,
            filter.CategoryId,
            filter.Priority,
            submittedByUserId ?? filter.SubmittedByUserId,
            assignedToUserId,
            null,
            filter.SearchTerm,
            filter.FromDate,
            filter.ToDate,
            ct);

        return PagedResult<FeedbackListItemDto>.Create(_mapper.Map<IEnumerable<FeedbackListItemDto>>(items), page, pageSize, totalCount);
    }

    public async Task<FeedbackDetailDto> GetFeedbackByIdAsync(Guid id, Guid requestingUserId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Feedback), id);

        var user = await GetCurrentUserAsync(requestingUserId, ct);
        EnsureCanViewFeedback(user, feedback, requestingUserId);

        var detail = _mapper.Map<FeedbackDetailDto>(feedback);

        if (user.Role == UserRole.Customer)
        {
            detail.Responses = detail.Responses
                .Where(r => !r.IsInternal)
                .ToList();
        }

        detail.Responses = detail.Responses.OrderBy(response => response.CreatedAtUtc).ToList();
        detail.Comments = detail.Comments
            .Where(comment => !comment.ParentCommentId.HasValue)
            .OrderBy(comment => comment.CreatedAtUtc)
            .ToList();
        detail.StatusHistory = detail.StatusHistory.OrderBy(history => history.ChangedAtUtc).ToList();

        await PopulateAttachmentUrlsAsync(feedback, detail);
        return detail;
    }

    /// <summary>Tạo phản hồi mới ở trạng thái SUBMITTED cho Customer đã xác thực.</summary>
    public async Task<FeedbackDetailDto> CreateFeedbackAsync(CreateFeedbackRequest request, Guid submittedByUserId, CancellationToken ct = default)
    {
        if (request.Rating is null or < 1 or > 5)
        {
            throw new ValidationException(["Rating is required and must be between 1 and 5."]);
        }

        var user = await GetCurrentUserAsync(submittedByUserId, ct);
        if (user.Role != UserRole.Customer)
        {
            throw new ForbiddenException("Only customers can submit feedback.");
        }

        var category = await _unitOfWork.FeedbackCategories.GetByIdAsync(request.CategoryId, ct)
            ?? throw new NotFoundException(nameof(FeedbackCategoryEntity), request.CategoryId);
        if (!category.IsActive)
        {
            throw new BusinessRuleException("The selected feedback category is disabled.");
        }
        if (category.Department != null && !category.Department.IsActive)
        {
            throw new BusinessRuleException("The department responsible for this category is disabled.");
        }

        var feedback = new Feedback
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CategoryId = category.Id,
            Category = category,
            DepartmentId = category.DepartmentId,
            Rating = request.Rating.Value,
            Status = FeedbackStatus.Submitted,
            Priority = FeedbackPriority.Medium,
            SubmittedByUserId = submittedByUserId
        };

        feedback.StatusHistory.Add(new FeedbackStatusHistory
        {
            FeedbackId = feedback.Id,
            FromStatus = FeedbackStatus.Submitted,
            ToStatus = FeedbackStatus.Submitted,
            ChangedByUserId = submittedByUserId,
            Reason = "Feedback created."
        });

        await _unitOfWork.Feedbacks.AddAsync(feedback, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(submittedByUserId, AuditAction.Create, nameof(Feedback), feedback.Id, null, $"Created feedback '{feedback.Title}'", null, ct);
        await SafeNotifyAsync(submittedByUserId, NotificationType.FeedbackSubmitted, "Feedback received", $"Your feedback '{feedback.Title}' has been submitted.", feedback.Id, nameof(Feedback), ct);

        var detail = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(feedback.Id, ct) ?? feedback;
        var result = _mapper.Map<FeedbackDetailDto>(detail);
        result.Responses = result.Responses.Where(response => !response.IsInternal).OrderBy(response => response.CreatedAtUtc).ToList();
        result.Comments = result.Comments.Where(comment => !comment.ParentCommentId.HasValue).OrderBy(comment => comment.CreatedAtUtc).ToList();
        result.StatusHistory = result.StatusHistory.OrderBy(history => history.ChangedAtUtc).ToList();
        await PopulateAttachmentUrlsAsync(detail, result);
        return result;
    }

    /// <summary>
    /// Cập nhật nội dung phản hồi. Customer chỉ được sửa phiếu của chính mình khi còn SUBMITTED;
    /// nhân viên quản lý vẫn phải thỏa điều kiện phạm vi xử lý.
    /// </summary>
    public async Task<FeedbackDetailDto> UpdateFeedbackAsync(Guid id, UpdateFeedbackRequest request, Guid requestingUserId, CancellationToken ct = default)
    {
        if (request.Rating is < 1 or > 5)
        {
            throw new ValidationException(["Rating must be between 1 and 5."]);
        }

        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Feedback), id);
        var user = await GetCurrentUserAsync(requestingUserId, ct);

        if (user.Role == UserRole.Customer)
        {
            if (feedback.SubmittedByUserId != requestingUserId)
                throw new ForbiddenException("Customers can only edit their own feedback.");
            if (feedback.Status != FeedbackStatus.Submitted)
                throw new BusinessRuleException("Customers can only edit feedback while it is SUBMITTED.");
        }

        if (user.Role != UserRole.Customer)
            EnsureCanHandleFeedback(user, feedback, requestingUserId);

        if (feedback.Status is FeedbackStatus.Closed or FeedbackStatus.Cancelled)
        {
            throw new BusinessRuleException("Closed or rejected feedback content cannot be edited.");
        }

        var category = await _unitOfWork.FeedbackCategories.GetByIdAsync(request.CategoryId, ct)
            ?? throw new NotFoundException(nameof(FeedbackCategoryEntity), request.CategoryId);
        if (!category.IsActive && category.Id != feedback.CategoryId)
        {
            throw new BusinessRuleException("Disabled categories cannot be selected for feedback.");
        }

        if (category.Department != null && !category.Department.IsActive && category.Id != feedback.CategoryId)
        {
            throw new BusinessRuleException("Categories in disabled departments cannot be selected for feedback.");
        }

        if (user.Role == UserRole.SupportStaff && category.DepartmentId != user.DepartmentId)
        {
            throw new ForbiddenException("Staff can only move feedback within their own department.");
        }

        if (feedback.AssignedToUserId.HasValue)
        {
            var assigneeDepartmentId = feedback.AssignedToUser?.DepartmentId;
            if (!assigneeDepartmentId.HasValue)
            {
                assigneeDepartmentId = (await _unitOfWork.Users.GetByIdAsync(feedback.AssignedToUserId.Value, ct))?.DepartmentId;
            }

            if (assigneeDepartmentId != category.DepartmentId)
            {
                throw new BusinessRuleException("Unassign or reassign the feedback before moving it to another department.");
            }
        }

        var oldValues = $"Title={feedback.Title};CategoryId={feedback.CategoryId};Priority={feedback.Priority}";
        feedback.Title = request.Title.Trim();
        feedback.Description = request.Description.Trim();
        feedback.CategoryId = category.Id;
        feedback.Category = category;
        feedback.DepartmentId = category.DepartmentId;
        if (request.Rating.HasValue)
            feedback.Rating = request.Rating.Value;
        // Customer không được tự thay đổi mức ưu tiên; Manager/Staff mới có quyền này.
        if (user.Role != UserRole.Customer)
            feedback.Priority = request.Priority;
        feedback.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(requestingUserId, AuditAction.Update, nameof(Feedback), feedback.Id, oldValues, $"Title={feedback.Title};CategoryId={feedback.CategoryId};Priority={feedback.Priority}", null, ct);

        var detail = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(feedback.Id, ct) ?? feedback;
        var result = _mapper.Map<FeedbackDetailDto>(detail);
        await PopulateAttachmentUrlsAsync(detail, result);
        return result;
    }

    public async Task<FeedbackDetailDto> UpdatePriorityAsync(Guid id, UpdateFeedbackPriorityRequest request, Guid requestingUserId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Feedback), id);
        var user = await GetCurrentUserAsync(requestingUserId, ct);
        EnsureCanHandleFeedback(user, feedback, requestingUserId);

        if (feedback.Status is FeedbackStatus.Closed or FeedbackStatus.Cancelled)
        {
            throw new BusinessRuleException("Priority cannot be changed on closed or rejected feedback.");
        }

        var oldPriority = feedback.Priority;
        feedback.Priority = request.Priority;
        feedback.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(requestingUserId, AuditAction.Update, nameof(Feedback), feedback.Id, oldPriority.ToString(), request.Priority.ToString(), null, ct);

        var detail = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(feedback.Id, ct) ?? feedback;
        var result = _mapper.Map<FeedbackDetailDto>(detail);
        await PopulateAttachmentUrlsAsync(detail, result);
        return result;
    }

    /// <summary>Đổi trạng thái và luôn ghi lại lịch sử để đáp ứng BR-18.</summary>
    public async Task ChangeStatusAsync(Guid id, ChangeFeedbackStatusRequest request, Guid requestingUserId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Feedback), id);
        var user = await GetCurrentUserAsync(requestingUserId, ct);

        EnsureCanHandleFeedback(user, feedback, requestingUserId);
        EnsureActorCanPerformTransition(user, feedback, request.NewStatus, requestingUserId);

        if (FeedbackStatusRules.RequiresReason(request.NewStatus) && string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new BusinessRuleException($"A reason is required when moving feedback to {request.NewStatus}.");
        }

        if (!FeedbackStatusRules.IsTransitionAllowed(feedback.Status, request.NewStatus))
        {
            throw new BusinessRuleException($"Cannot transition feedback from {feedback.Status} to {request.NewStatus}.");
        }

        if (request.NewStatus == FeedbackStatus.Assigned && !feedback.AssignedToUserId.HasValue)
        {
            throw new BusinessRuleException("Assign a support staff member before moving feedback to Assigned.");
        }

        if (request.NewStatus == FeedbackStatus.Resolved && !feedback.Responses.Any(response =>
                response.FeedbackId == feedback.Id &&
                !response.IsDeleted &&
                !response.IsInternal &&
                response.RespondedByUser?.Role == UserRole.SupportStaff))
        {
            throw new BusinessRuleException("Feedback cannot be resolved until a Staff response has been saved.");
        }

        var oldStatus = feedback.Status;
        feedback.Status = request.NewStatus;
        feedback.UpdatedAtUtc = DateTime.UtcNow;

        if (request.NewStatus == FeedbackStatus.Resolved)
        {
            feedback.ResolvedAtUtc = DateTime.UtcNow;
        }
        else if (oldStatus == FeedbackStatus.Resolved && request.NewStatus == FeedbackStatus.InProgress)
        {
            feedback.ResolvedAtUtc = null;
        }

        if (request.NewStatus == FeedbackStatus.Closed)
        {
            feedback.ClosedAtUtc = DateTime.UtcNow;
        }

        var statusHistory = new FeedbackStatusHistory
        {
            FeedbackId = feedback.Id,
            FromStatus = oldStatus,
            ToStatus = request.NewStatus,
            ChangedByUserId = requestingUserId,
            Reason = request.Reason
        };
        feedback.StatusHistory.Add(statusHistory);
        await _unitOfWork.Feedbacks.AddStatusHistoryAsync(statusHistory, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(requestingUserId, AuditAction.StatusChange, nameof(Feedback), feedback.Id, oldStatus.ToString(), request.NewStatus.ToString(), null, ct);
        await SafeNotifyAsync(feedback.SubmittedByUserId, NotificationType.FeedbackStatusChanged, "Feedback status updated", $"Your feedback '{feedback.Title}' is now {request.NewStatus}.", feedback.Id, nameof(Feedback), ct);
    }

    /// <summary>
    /// Khóa actor cho từng bước workflow. Hàm này ngăn việc gọi endpoint đổi trạng thái
    /// tổng quát để bỏ qua quyền Staff/Manager được nêu trong BR-13 đến BR-16.
    /// </summary>
    private static void EnsureActorCanPerformTransition(User user, Feedback feedback, FeedbackStatus target, Guid userId)
    {
        if (target == FeedbackStatus.Assigned)
            throw new BusinessRuleException("Use the manager assignment endpoint to assign feedback.");

        if (target == FeedbackStatus.Cancelled)
            throw new BusinessRuleException("Use the customer cancellation endpoint to cancel feedback.");

        if (target is FeedbackStatus.InProgress or FeedbackStatus.Resolved)
        {
            if (user.Role != UserRole.SupportStaff || feedback.AssignedToUserId != userId)
                throw new ForbiddenException("Only the assigned Staff member can start or resolve feedback.");
        }

        if (target == FeedbackStatus.Closed && user.Role != UserRole.DepartmentManager)
            throw new ForbiddenException("Only a Manager can close resolved feedback.");
    }

    /// <summary>
    /// Hủy phản hồi theo BR-12. Chỉ Customer sở hữu phiếu và chỉ khi phiếu còn SUBMITTED.
    /// </summary>
    public async Task CancelFeedbackAsync(Guid id, Guid customerId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Feedback), id);
        var user = await GetCurrentUserAsync(customerId, ct);

        if (user.Role != UserRole.Customer || feedback.SubmittedByUserId != customerId)
            throw new ForbiddenException("Customers can only cancel their own feedback.");
        if (feedback.Status != FeedbackStatus.Submitted)
            throw new BusinessRuleException("Feedback can only be cancelled while it is SUBMITTED.");

        feedback.Status = FeedbackStatus.Cancelled;
        feedback.UpdatedAtUtc = DateTime.UtcNow;
        var statusHistory = new FeedbackStatusHistory
        {
            FeedbackId = feedback.Id,
            FromStatus = FeedbackStatus.Submitted,
            ToStatus = FeedbackStatus.Cancelled,
            ChangedByUserId = customerId,
            Reason = "Cancelled by customer before assignment."
        };
        feedback.StatusHistory.Add(statusHistory);
        await _unitOfWork.Feedbacks.AddStatusHistoryAsync(statusHistory, ct);

        await _unitOfWork.SaveChangesAsync(ct);
        await _auditLogService.LogAsync(customerId, AuditAction.StatusChange, nameof(Feedback), feedback.Id,
            FeedbackStatus.Submitted.ToString(), FeedbackStatus.Cancelled.ToString(), null, ct);
    }

    public async Task DeleteFeedbackAsync(Guid id, Guid requestingUserId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Feedback), id);
        var user = await GetCurrentUserAsync(requestingUserId, ct);

        if (user.Role == UserRole.Customer)
        {
            if (feedback.SubmittedByUserId != requestingUserId)
            {
                throw new ForbiddenException("You cannot delete another customer's feedback.");
            }

            if (feedback.Status != FeedbackStatus.Submitted)
            {
                throw new BusinessRuleException("Customers can only delete feedback while it is still SUBMITTED.");
            }
        }
        else if (user.Role != UserRole.SystemAdmin)
        {
            throw new ForbiddenException("Only the owner customer or a system admin can delete feedback.");
        }

        feedback.IsDeleted = true;
        feedback.DeletedAtUtc = DateTime.UtcNow;
        feedback.DeletedByUserId = requestingUserId;
        feedback.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(requestingUserId, AuditAction.Delete, nameof(Feedback), feedback.Id, null, $"Deleted feedback '{feedback.Title}'", null, ct);
    }

    public async Task<FeedbackAttachmentDto> UploadAttachmentAsync(Guid feedbackId, UploadedFileInput file, Guid uploadedByUserId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(feedbackId, ct)
            ?? throw new NotFoundException(nameof(Feedback), feedbackId);
        var user = await GetCurrentUserAsync(uploadedByUserId, ct);
        EnsureCanViewFeedback(user, feedback, uploadedByUserId);

        if (feedback.Status is FeedbackStatus.Closed or FeedbackStatus.Cancelled)
        {
            throw new BusinessRuleException("Attachments cannot be added to closed or rejected feedback.");
        }

        if (file.Length <= 0)
        {
            throw new BusinessRuleException("Uploaded file is empty.");
        }

        if (file.Length > FeedbackConstants.MaxAttachmentSizeBytes)
        {
            throw new BusinessRuleException($"File size exceeds the {FeedbackConstants.MaxAttachmentSizeBytes / 1024 / 1024} MB limit.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!FeedbackConstants.AllowedAttachmentExtensions.Contains(extension))
        {
            throw new BusinessRuleException($"File extension '{extension}' is not allowed.");
        }

        if (!FeedbackConstants.AllowedAttachmentContentTypes.TryGetValue(extension, out var contentTypes) ||
            !contentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException($"File content type '{file.ContentType}' does not match extension '{extension}'.");
        }

        var originalFileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(originalFileName) || originalFileName.Length > 256)
        {
            throw new BusinessRuleException("Attachment file names must be between 1 and 256 characters.");
        }

        if (file.ContentType.Length > 128)
        {
            throw new BusinessRuleException("Attachment content type is too long.");
        }

        if (feedback.Attachments.Count >= FeedbackConstants.MaxAttachmentsPerFeedback)
        {
            throw new BusinessRuleException($"A feedback record can have at most {FeedbackConstants.MaxAttachmentsPerFeedback} attachments.");
        }

        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var storageKey = $"feedbacks/{feedbackId}/{safeFileName}";
        await _storageService.UploadFileAsync(file.Content, storageKey, file.ContentType, AttachmentBucketName, ct);
        var publicUrl = await _storageService.GetPublicUrlAsync(storageKey, AttachmentBucketName);

        var attachment = new FeedbackAttachment
        {
            FeedbackId = feedbackId,
            FileName = originalFileName,
            StorageKey = storageKey,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            UploadedByUserId = uploadedByUserId
        };

        try
        {
            feedback.Attachments.Add(attachment);
            await _unitOfWork.Feedbacks.AddAttachmentAsync(attachment, ct);
            feedback.UpdatedAtUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch
        {
            try
            {
                await _storageService.DeleteFileAsync(storageKey, AttachmentBucketName, ct);
            }
            catch
            {
                // Preserve the database failure; storage cleanup is best-effort.
            }
            throw;
        }

        await _auditLogService.LogAsync(uploadedByUserId, AuditAction.Create, nameof(FeedbackAttachment), attachment.Id, null, $"Uploaded attachment '{attachment.FileName}'", null, ct);

        var dto = _mapper.Map<FeedbackAttachmentDto>(attachment);
        dto.PublicUrl = publicUrl;
        return dto;
    }

    public async Task DeleteAttachmentAsync(Guid feedbackId, Guid attachmentId, Guid requestingUserId, CancellationToken ct = default)
    {
        var attachment = await _unitOfWork.Feedbacks.GetAttachmentByIdAsync(attachmentId, ct)
            ?? throw new NotFoundException(nameof(FeedbackAttachment), attachmentId);
        var feedback = attachment.Feedback;

        if (attachment.FeedbackId != feedbackId)
        {
            throw new NotFoundException(nameof(FeedbackAttachment), attachmentId);
        }

        var user = await GetCurrentUserAsync(requestingUserId, ct);
        EnsureCanViewFeedback(user, feedback, requestingUserId);

        if (user.Role != UserRole.SystemAdmin && feedback.Status is FeedbackStatus.Closed or FeedbackStatus.Cancelled)
        {
            throw new BusinessRuleException("Attachments on closed or rejected feedback cannot be deleted.");
        }

        if (user.Role != UserRole.SystemAdmin && attachment.UploadedByUserId != requestingUserId)
        {
            throw new ForbiddenException("Only the attachment uploader or a system admin can delete this attachment.");
        }

        await _storageService.DeleteFileAsync(attachment.StorageKey, AttachmentBucketName, ct);
        _unitOfWork.Feedbacks.RemoveAttachment(attachment);
        feedback.UpdatedAtUtc = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(requestingUserId, AuditAction.Delete, nameof(FeedbackAttachment), attachment.Id, null, $"Deleted attachment '{attachment.FileName}'", null, ct);
    }

    private async Task<User> GetCurrentUserAsync(Guid userId, CancellationToken ct)
        => await _unitOfWork.Users.GetByIdAsync(userId, ct)
            ?? throw new UnauthorizedException("Authenticated user was not found.");

    private static void EnsureCanViewFeedback(User user, Feedback feedback, Guid userId)
    {
        if (user.Role == UserRole.Customer && feedback.SubmittedByUserId != userId)
        {
            throw new ForbiddenException("Customers can only access their own feedback.");
        }

        if (user.Role == UserRole.SupportStaff && feedback.AssignedToUserId != userId)
        {
            throw new ForbiddenException("Support staff can only access assigned feedback.");
        }

    }

    private static void EnsureCanHandleFeedback(User user, Feedback feedback, Guid userId)
    {
        if (user.Role == UserRole.SupportStaff && feedback.AssignedToUserId != userId)
        {
            throw new ForbiddenException("Support staff can only handle assigned feedback.");
        }

        if (user.Role == UserRole.Customer)
        {
            throw new ForbiddenException("Customers cannot perform this internal workflow action.");
        }
    }

    private async Task SafeNotifyAsync(Guid userId, NotificationType type, string title, string message, Guid? entityId, string? entityType, CancellationToken ct)
    {
        try
        {
            await _notificationService.SendNotificationAsync(userId, type, title, message, entityId, entityType, ct);
        }
        catch
        {
            // Notification failures must not cancel the primary feedback workflow.
        }
    }

    private async Task PopulateAttachmentUrlsAsync(Feedback feedback, FeedbackDetailDto dto)
    {
        var attachmentsById = feedback.Attachments.ToDictionary(attachment => attachment.Id);
        foreach (var attachmentDto in dto.Attachments)
        {
            if (attachmentsById.TryGetValue(attachmentDto.Id, out var attachment))
            {
                attachmentDto.PublicUrl = await _storageService.GetPublicUrlAsync(attachment.StorageKey, AttachmentBucketName);
            }
        }
    }
}
