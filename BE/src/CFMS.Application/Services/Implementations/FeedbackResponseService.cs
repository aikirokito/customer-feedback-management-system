using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.DTOs.Feedback;
using CFMS.Application.DTOs.Responses;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;

namespace CFMS.Application.Services.Implementations;

public class FeedbackResponseService : IFeedbackResponseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;

    public FeedbackResponseService(
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

    public async Task<IEnumerable<FeedbackResponseDto>> GetResponsesForFeedbackAsync(Guid feedbackId, Guid requestingUserId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(feedbackId, ct)
            ?? throw new NotFoundException(nameof(Feedback), feedbackId);
        var user = await GetUserAsync(requestingUserId, ct);
        EnsureCanView(user, feedback, requestingUserId);

        IEnumerable<FeedbackResponse> responses = feedback.Responses
            .Where(r => !r.IsDeleted);

        if (user.Role == UserRole.Customer)
        {
            responses = responses.Where(r => !r.IsInternal);
        }

        responses = responses.OrderBy(r => r.CreatedAtUtc);

        return _mapper.Map<IEnumerable<FeedbackResponseDto>>(responses);
    }

    public async Task<FeedbackResponseDto> CreateResponseAsync(CreateResponseRequest request, Guid respondedByUserId, CancellationToken ct = default)
    {
        var feedback = await _unitOfWork.Feedbacks.GetByIdWithDetailsAsync(request.FeedbackId, ct)
            ?? throw new NotFoundException(nameof(Feedback), request.FeedbackId);
        var responder = await GetUserAsync(respondedByUserId, ct);
        EnsureCanRespond(responder, feedback, respondedByUserId);

        var response = new FeedbackResponse
        {
            FeedbackId = feedback.Id,
            RespondedByUserId = respondedByUserId,
            Content = request.Content.Trim(),
            IsInternal = request.IsInternal,
            RespondedByUser = responder
        };

        feedback.Responses.Add(response);
        feedback.UpdatedAtUtc = DateTime.UtcNow;
        _unitOfWork.Feedbacks.Update(feedback);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(respondedByUserId, AuditAction.Create, nameof(FeedbackResponse), response.Id, null, $"Response added to feedback {feedback.Id}", null, ct);

        if (!request.IsInternal)
        {
            await SafeNotifyAsync(feedback.SubmittedByUserId, NotificationType.FeedbackResponseAdded, "Staff responded", $"A staff member responded to your feedback '{feedback.Title}'.", feedback.Id, nameof(Feedback), ct);
        }

        return _mapper.Map<FeedbackResponseDto>(response);
    }

    public async Task<FeedbackResponseDto> UpdateResponseAsync(Guid responseId, UpdateResponseRequest request, Guid requestingUserId, CancellationToken ct = default)
    {
        var response = await _unitOfWork.Feedbacks.GetResponseByIdAsync(responseId, ct)
            ?? throw new NotFoundException(nameof(FeedbackResponse), responseId);
        var user = await GetUserAsync(requestingUserId, ct);

        if (response.RespondedByUserId != requestingUserId && user.Role != UserRole.SystemAdmin)
        {
            throw new ForbiddenException("Only the response author or a system admin can update this response.");
        }

        response.Content = request.Content.Trim();
        response.UpdatedAtUtc = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(requestingUserId, AuditAction.Update, nameof(FeedbackResponse), response.Id, null, "Response updated", null, ct);
        return _mapper.Map<FeedbackResponseDto>(response);
    }

    public async Task DeleteResponseAsync(Guid responseId, Guid requestingUserId, CancellationToken ct = default)
    {
        var response = await _unitOfWork.Feedbacks.GetResponseByIdAsync(responseId, ct)
            ?? throw new NotFoundException(nameof(FeedbackResponse), responseId);
        var user = await GetUserAsync(requestingUserId, ct);

        if (response.RespondedByUserId != requestingUserId && user.Role != UserRole.SystemAdmin)
        {
            throw new ForbiddenException("Only the response author or a system admin can delete this response.");
        }

        response.IsDeleted = true;
        response.DeletedAtUtc = DateTime.UtcNow;
        response.DeletedByUserId = requestingUserId;
        response.UpdatedAtUtc = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditLogService.LogAsync(requestingUserId, AuditAction.Delete, nameof(FeedbackResponse), response.Id, null, "Response deleted", null, ct);
    }

    private async Task<User> GetUserAsync(Guid userId, CancellationToken ct)
        => await _unitOfWork.Users.GetByIdAsync(userId, ct)
            ?? throw new UnauthorizedException("Authenticated user was not found.");

    private static void EnsureCanView(User user, Feedback feedback, Guid userId)
    {
        if (user.Role == UserRole.Customer && feedback.SubmittedByUserId != userId)
            throw new ForbiddenException("Customers can only view responses on their own feedback.");
        if (user.Role == UserRole.SupportStaff && feedback.AssignedToUserId != userId)
            throw new ForbiddenException("Support staff can only view assigned feedback responses.");
    }

    private static void EnsureCanRespond(User user, Feedback feedback, Guid userId)
    {
        if (user.Role == UserRole.SupportStaff && feedback.AssignedToUserId != userId)
            throw new ForbiddenException("Support staff can only respond to assigned feedback.");
        if (user.Role == UserRole.Customer)
            throw new ForbiddenException("Customers cannot create staff responses.");
    }

    private async Task SafeNotifyAsync(Guid userId, NotificationType type, string title, string message, Guid? entityId, string? entityType, CancellationToken ct)
    {
        try
        {
            await _notificationService.SendNotificationAsync(userId, type, title, message, entityId, entityType, ct);
        }
        catch
        {
            // Notification failures are non-blocking for response creation.
        }
    }
}
