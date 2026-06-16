using CFMS.Application.DTOs.Assignments;

namespace CFMS.Application.Services.Interfaces;

public interface IFeedbackAssignmentService
{
    Task<AssignmentDto> AssignFeedbackAsync(AssignFeedbackRequest request, Guid assignedByUserId, CancellationToken ct = default);
    Task<IEnumerable<AssignmentDto>> GetAssignmentHistoryAsync(Guid feedbackId, CancellationToken ct = default);
    Task UnassignFeedbackAsync(Guid feedbackId, Guid requestingUserId, CancellationToken ct = default);
}
