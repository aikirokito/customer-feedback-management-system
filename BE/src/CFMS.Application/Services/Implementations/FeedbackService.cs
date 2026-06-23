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
    private const string AttachmentBucketName = "feedback-attachments";

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
            filter.Category,
            filter.Priority,
            submittedByUserId,
            assignedToUserId,
            filter.SearchTerm,
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

        return detail;
    }

    public async Task<FeedbackDetailDto> CreateFeedbackAsync(CreateFeedbackRequest request, Guid submittedByUserId, CancellationToken ct = default)
    {
        var user = await GetCurrentUserAsync(submittedByUserId, ct);
        if (user.Role != UserRole.Customer)
        {
            throw new ForbiddenException("Only customers can submit feedback.");
        }

        var feedback = new Feedback
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category,
            Rating = request.Rating,
            Status = FeedbackStatus.New,
            Priority = FeedbackPriority.Medium,
            SubmittedByUserId = submittedByUserId
        };

        feedback.StatusHistory.Add(new FeedbackStatusHistory
        {
            FeedbackId = feedback.Id,
            FromStatus = FeedbackStatus.New,
            ToStatus = FeedbackStatus.New,
            ChangedByUserId = submittedByUserId,
            Reason = "Feedback created."
        });

        await _unitOfWork.Feedbacks.AddAsync(feedback, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(submittedByUserId, AuditAction.Create, nameof(Feedback), feedback.Id, null, $"Created feedback '{feedback.Title}'", null, ct);
        await SafeNotifyAsync(submittedByUserId, NotificationType.FeedbackSubmitted, "Feedback received", $"Your feedback '{feedback.Title}' has been submitted.", feedback.Id, nameof(Feedback), ct);

        var detail = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(feedback.Id, ct) ?? feedback;
        return _mapper.Map<FeedbackDetailDto>(detail);
    }

    public async Task<FeedbackDetailDto> UpdateFeedbackAsync(Guid id, UpdateFeedbackRequest request, Guid requestingUserId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Feedback), id);
        var user = await GetCurrentUserAsync(requestingUserId, ct);

        if (user.Role == UserRole.Customer)
        {
            throw new ForbiddenException("Customers cannot edit feedback after submission.");
        }

        EnsureCanHandleFeedback(user, feedback, requestingUserId);

        var oldValues = $"Title={feedback.Title};Category={feedback.Category};Priority={feedback.Priority}";
        feedback.Title = request.Title.Trim();
        feedback.Description = request.Description.Trim();
        feedback.Category = request.Category;
        feedback.Priority = request.Priority;
        feedback.UpdatedAtUtc = DateTime.UtcNow;

        _unitOfWork.Feedbacks.Update(feedback);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(requestingUserId, AuditAction.Update, nameof(Feedback), feedback.Id, oldValues, $"Title={feedback.Title};Category={feedback.Category};Priority={feedback.Priority}", null, ct);

        var detail = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(feedback.Id, ct) ?? feedback;
        return _mapper.Map<FeedbackDetailDto>(detail);
    }

    public async Task<FeedbackDetailDto> UpdatePriorityAsync(Guid id, UpdateFeedbackPriorityRequest request, Guid requestingUserId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Feedback), id);
        var user = await GetCurrentUserAsync(requestingUserId, ct);
        EnsureCanHandleFeedback(user, feedback, requestingUserId);

        var oldPriority = feedback.Priority;
        feedback.Priority = request.Priority;
        feedback.UpdatedAtUtc = DateTime.UtcNow;

        _unitOfWork.Feedbacks.Update(feedback);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(requestingUserId, AuditAction.Update, nameof(Feedback), feedback.Id, oldPriority.ToString(), request.Priority.ToString(), null, ct);

        var detail = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(feedback.Id, ct) ?? feedback;
        return _mapper.Map<FeedbackDetailDto>(detail);
    }

    public async Task ChangeStatusAsync(Guid id, ChangeFeedbackStatusRequest request, Guid requestingUserId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Feedback), id);
        var user = await GetCurrentUserAsync(requestingUserId, ct);

        EnsureCanHandleFeedback(user, feedback, requestingUserId);

        if (FeedbackStatusRules.RequiresReason(request.NewStatus) && string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new BusinessRuleException($"A reason is required when moving feedback to {request.NewStatus}.");
        }

        if (!FeedbackStatusRules.IsTransitionAllowed(feedback.Status, request.NewStatus))
        {
            throw new BusinessRuleException($"Cannot transition feedback from {feedback.Status} to {request.NewStatus}.");
        }

        var oldStatus = feedback.Status;
        feedback.Status = request.NewStatus;
        feedback.UpdatedAtUtc = DateTime.UtcNow;

        if (request.NewStatus == FeedbackStatus.Resolved)
        {
            feedback.ResolvedAtUtc = DateTime.UtcNow;
        }

        if (request.NewStatus == FeedbackStatus.Closed)
        {
            feedback.ClosedAtUtc = DateTime.UtcNow;
        }

        feedback.StatusHistory.Add(new FeedbackStatusHistory
        {
            FeedbackId = feedback.Id,
            FromStatus = oldStatus,
            ToStatus = request.NewStatus,
            ChangedByUserId = requestingUserId,
            Reason = request.Reason
        });

        _unitOfWork.Feedbacks.Update(feedback);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(requestingUserId, AuditAction.StatusChange, nameof(Feedback), feedback.Id, oldStatus.ToString(), request.NewStatus.ToString(), null, ct);
        await SafeNotifyAsync(feedback.SubmittedByUserId, NotificationType.FeedbackStatusChanged, "Feedback status updated", $"Your feedback '{feedback.Title}' is now {request.NewStatus}.", feedback.Id, nameof(Feedback), ct);
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

            if (feedback.Status != FeedbackStatus.New)
            {
                throw new BusinessRuleException("Customers can only delete feedback while it is still New.");
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

        _unitOfWork.Feedbacks.Update(feedback);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(requestingUserId, AuditAction.Delete, nameof(Feedback), feedback.Id, null, $"Deleted feedback '{feedback.Title}'", null, ct);
    }

    public async Task<FeedbackAttachmentDto> UploadAttachmentAsync(Guid feedbackId, UploadedFileInput file, Guid uploadedByUserId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(feedbackId, ct)
            ?? throw new NotFoundException(nameof(Feedback), feedbackId);
        var user = await GetCurrentUserAsync(uploadedByUserId, ct);
        EnsureCanViewFeedback(user, feedback, uploadedByUserId);

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
            FileName = Path.GetFileName(file.FileName),
            StorageKey = storageKey,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            UploadedByUserId = uploadedByUserId
        };

        feedback.Attachments.Add(attachment);
        feedback.UpdatedAtUtc = DateTime.UtcNow;
        _unitOfWork.Feedbacks.Update(feedback);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(uploadedByUserId, AuditAction.Create, nameof(FeedbackAttachment), attachment.Id, null, $"Uploaded attachment '{attachment.FileName}'", null, ct);

        var dto = _mapper.Map<FeedbackAttachmentDto>(attachment);
        dto.PublicUrl = publicUrl;
        return dto;
    }

    public async Task DeleteAttachmentAsync(Guid attachmentId, Guid requestingUserId, CancellationToken ct = default)
    {
        var attachment = await _unitOfWork.Feedbacks.GetAttachmentByIdAsync(attachmentId, ct)
            ?? throw new NotFoundException(nameof(FeedbackAttachment), attachmentId);
        var feedback = attachment.Feedback;
        var user = await GetCurrentUserAsync(requestingUserId, ct);
        EnsureCanViewFeedback(user, feedback, requestingUserId);

        await _storageService.DeleteFileAsync(attachment.StorageKey, AttachmentBucketName, ct);
        _unitOfWork.Feedbacks.RemoveAttachment(attachment);
        feedback.UpdatedAtUtc = DateTime.UtcNow;
        _unitOfWork.Feedbacks.Update(feedback);
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
}
