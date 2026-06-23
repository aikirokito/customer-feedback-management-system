using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.DTOs.Assignments;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;

namespace CFMS.Application.Services.Implementations;

public class FeedbackAssignmentService : IFeedbackAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;

    public FeedbackAssignmentService(
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

    public async Task<AssignmentDto> AssignFeedbackAsync(AssignFeedbackRequest request, Guid assignedByUserId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(request.FeedbackId, ct)
            ?? throw new NotFoundException(nameof(Feedback), request.FeedbackId);
        var assigner = await _unitOfWork.Users.GetByIdAsync(assignedByUserId, ct)
            ?? throw new UnauthorizedException("Assigner was not found.");

        if (assigner.Role is not (UserRole.DepartmentManager or UserRole.SystemAdmin))
        {
            throw new ForbiddenException("Only Department Managers or System Admins can assign feedback.");
        }

        if (feedback.Status is FeedbackStatus.Closed or FeedbackStatus.Rejected or FeedbackStatus.Resolved)
        {
            throw new BusinessRuleException("Closed, rejected, or resolved feedback cannot be assigned.");
        }
        var isReassignment = feedback.AssignedToUserId.HasValue;

        if (!isReassignment && feedback.Status != FeedbackStatus.New)
        {
            throw new BusinessRuleException("Only new feedback can be assigned for the first time.");
        }

        if (isReassignment && feedback.Status is not (
            FeedbackStatus.Assigned or
            FeedbackStatus.InProgress or
            FeedbackStatus.WaitingForCustomer))
        {
            throw new BusinessRuleException("Only active assigned feedback can reassigned.");
        }

        var assignee = await _unitOfWork.Users.GetByIdAsync(request.AssignToUserId, ct)
            ?? throw new NotFoundException(nameof(User), request.AssignToUserId);

        if (assignee.Role != UserRole.SupportStaff)
        {
            throw new BusinessRuleException("Feedback can only be assigned to active Support Staff.");
        }

        if (!assignee.IsActive)
        {
            throw new BusinessRuleException("Feedback cannot be assigned to a disabled staff account.");
        }

        foreach (var activeAssignment in feedback.AssignmentHistory.Where(a => a.IsActive))
        {
            activeAssignment.IsActive = false;
        }

        var assignment = new FeedbackAssignment
        {
            FeedbackId = feedback.Id,
            AssignedToUserId = assignee.Id,
            AssignedByUserId = assignedByUserId,
            Note = request.Note,
            IsActive = true,
            AssignedToUser = assignee,
            AssignedByUser = assigner
        };

        feedback.AssignmentHistory.Add(assignment);
        feedback.AssignedToUserId = assignee.Id;
        feedback.UpdatedAtUtc = DateTime.UtcNow;

        if (feedback.Status == FeedbackStatus.New)
        {
            feedback.Status = FeedbackStatus.Assigned;
            feedback.StatusHistory.Add(new FeedbackStatusHistory
            {
                FeedbackId = feedback.Id,
                FromStatus = FeedbackStatus.New,
                ToStatus = FeedbackStatus.Assigned,
                ChangedByUserId = assignedByUserId,
                Reason = "Feedback assigned to support staff."
            });
        }

        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(assignedByUserId, AuditAction.Assignment, nameof(FeedbackAssignment), assignment.Id, null, $"Assigned feedback {feedback.Id} to user {assignee.Id}", null, ct);
        await SafeNotifyAsync(assignee.Id, NotificationType.FeedbackAssigned, "New feedback assignment", $"You have been assigned feedback '{feedback.Title}'.", feedback.Id, nameof(Feedback), ct);
        await SafeNotifyAsync(feedback.SubmittedByUserId, NotificationType.FeedbackAssigned, "Feedback assigned", $"Your feedback '{feedback.Title}' has been assigned to support staff.", feedback.Id, nameof(Feedback), ct);

        return _mapper.Map<AssignmentDto>(assignment);
    }

    public async Task<IEnumerable<AssignmentDto>> GetAssignmentHistoryAsync(Guid feedbackId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(feedbackId, ct)
            ?? throw new NotFoundException(nameof(Feedback), feedbackId);

        return _mapper.Map<IEnumerable<AssignmentDto>>(feedback.AssignmentHistory.OrderByDescending(a => a.CreatedAtUtc));
    }

    public async Task UnassignFeedbackAsync(Guid feedbackId, Guid requestingUserId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(feedbackId, ct)
            ?? throw new NotFoundException(nameof(Feedback), feedbackId);
        var user = await _unitOfWork.Users.GetByIdAsync(requestingUserId, ct)
            ?? throw new UnauthorizedException("User was not found.");

        if (user.Role is not (UserRole.DepartmentManager or UserRole.SystemAdmin))
        {
            throw new ForbiddenException("Only Department Managers or System Admins can unassign feedback.");
        }

        var previousAssigneeId = feedback.AssignedToUserId;
        foreach (var activeAssignment in feedback.AssignmentHistory.Where(a => a.IsActive))
        {
            activeAssignment.IsActive = false;
        }

        feedback.AssignedToUserId = null;
        feedback.UpdatedAtUtc = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(requestingUserId, AuditAction.Assignment, nameof(Feedback), feedback.Id, null, "Feedback unassigned", null, ct);

        if (previousAssigneeId.HasValue)
        {
            await SafeNotifyAsync(previousAssigneeId.Value, NotificationType.FeedbackAssigned, "Feedback unassigned", $"You have been unassigned from feedback '{feedback.Title}'.", feedback.Id, nameof(Feedback), ct);
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
            // Real-time/in-app notification failure should not cancel the assignment workflow.
        }
    }
}
