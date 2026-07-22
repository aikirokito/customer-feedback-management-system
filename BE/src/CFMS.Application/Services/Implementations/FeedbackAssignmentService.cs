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

    /// <summary>
    /// Manager phân công hoặc phân công lại một phản hồi cho Staff đang hoạt động.
    /// Lần phân công đầu tiên đồng thời chuyển SUBMITTED sang ASSIGNED và ghi lịch sử.
    /// </summary>
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

        if (feedback.Status is FeedbackStatus.Closed or FeedbackStatus.Cancelled or FeedbackStatus.Resolved)
        {
            throw new BusinessRuleException("Closed, rejected, or resolved feedback cannot be assigned.");
        }
        var isReassignment = feedback.AssignedToUserId.HasValue;

        if (!isReassignment && feedback.Status != FeedbackStatus.Submitted)
        {
            throw new BusinessRuleException("Only new feedback can be assigned for the first time.");
        }

        if (isReassignment && feedback.Status is not (
            FeedbackStatus.Assigned or
            FeedbackStatus.InProgress))
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
        feedback.DepartmentId ??= assignee.DepartmentId;
        feedback.UpdatedAtUtc = DateTime.UtcNow;

        if (feedback.Status == FeedbackStatus.Submitted)
        {
            feedback.Status = FeedbackStatus.Assigned;
            feedback.StatusHistory.Add(new FeedbackStatusHistory
            {
                FeedbackId = feedback.Id,
                FromStatus = FeedbackStatus.Submitted,
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

    public async Task<IEnumerable<AssignmentDto>> GetAssignmentHistoryAsync(Guid feedbackId, Guid requestingUserId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(feedbackId, ct)
            ?? throw new NotFoundException(nameof(Feedback), feedbackId);
        var user = await _unitOfWork.Users.GetByIdAsync(requestingUserId, ct)
            ?? throw new UnauthorizedException("User was not found.");

        EnsureCanManageAssignment(user, feedback, requestingUserId);

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
        if (!previousAssigneeId.HasValue)
        {
            throw new BusinessRuleException("Feedback is not currently assigned.");
        }

        if (feedback.Status is FeedbackStatus.Closed or FeedbackStatus.Cancelled or FeedbackStatus.Resolved)
        {
            throw new BusinessRuleException("Closed, rejected, or resolved feedback cannot be unassigned.");
        }

        var previousStatus = feedback.Status;
        foreach (var activeAssignment in feedback.AssignmentHistory.Where(a => a.IsActive))
        {
            activeAssignment.IsActive = false;
        }

        feedback.AssignedToUserId = null;
        feedback.Status = FeedbackStatus.Submitted;
        feedback.ResolvedAtUtc = null;
        feedback.ClosedAtUtc = null;
        feedback.UpdatedAtUtc = DateTime.UtcNow;

        if (previousStatus != FeedbackStatus.Submitted)
        {
            feedback.StatusHistory.Add(new FeedbackStatusHistory
            {
                FeedbackId = feedback.Id,
                FromStatus = previousStatus,
                ToStatus = FeedbackStatus.Submitted,
                ChangedByUserId = requestingUserId,
                Reason = "Feedback unassigned and returned to the triage queue."
            });
        }

        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(
            requestingUserId,
            AuditAction.Assignment,
            nameof(Feedback),
            feedback.Id,
            $"AssignedToUserId={previousAssigneeId};Status={previousStatus}",
            "AssignedToUserId=null;Status=New",
            null,
            ct);

        await SafeNotifyAsync(previousAssigneeId.Value, NotificationType.FeedbackAssigned, "Feedback unassigned", $"You have been unassigned from feedback '{feedback.Title}'.", feedback.Id, nameof(Feedback), ct);
    }

    private static void EnsureCanManageAssignment(User user, Feedback feedback, Guid userId)
    {
        if (user.Role == UserRole.Customer)
        {
            throw new ForbiddenException("Customers cannot view internal assignment history.");
        }

        if (user.Role == UserRole.SupportStaff && feedback.AssignedToUserId != userId)
        {
            throw new ForbiddenException("Support Staff can only view assignment history for assigned feedback.");
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
