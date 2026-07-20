using AutoMapper;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.DTOs.Reports;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Enums;

namespace CFMS.Application.Services.Implementations;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReportService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<FeedbackSummaryReportDto> GetFeedbackSummaryAsync(ReportFilterRequest filter, Guid requestingUserId, CancellationToken ct = default)
    {
        var departmentId = await GetReportDepartmentScopeAsync(requestingUserId, ct);
        var feedbacks = (await _unitOfWork.Feedbacks.GetReportFeedbacksAsync(
            filter.FromDate,
            filter.ToDate,
            filter.CategoryId,
            filter.Status,
            filter.AssignedToUserId,
            departmentId,
            ct)).ToList();

        var resolvedOrClosed = feedbacks
            .Where(f => f.Status is FeedbackStatus.Resolved or FeedbackStatus.Closed)
            .Where(f => f.CreatedAtUtc != default && (f.ResolvedAtUtc.HasValue || f.ClosedAtUtc.HasValue))
            .Select(f => ((f.ResolvedAtUtc ?? f.ClosedAtUtc)!.Value - f.CreatedAtUtc).TotalHours)
            .ToList();

        var ratedFeedbacks = feedbacks.Where(f => f.Rating.HasValue).Select(f => f.Rating!.Value).ToList();
        var resolvedCount = feedbacks.Count(f => f.Status is FeedbackStatus.Resolved or FeedbackStatus.Closed);

        return new FeedbackSummaryReportDto
        {
            TotalFeedbacks = feedbacks.Count,
            OpenFeedbacks = feedbacks.Count(f => f.Status is
                FeedbackStatus.New or
                FeedbackStatus.Assigned or
                FeedbackStatus.InProgress or
                FeedbackStatus.WaitingForCustomer),
            ResolvedFeedbacks = feedbacks.Count(f => f.Status == FeedbackStatus.Resolved),
            ClosedFeedbacks = feedbacks.Count(f => f.Status == FeedbackStatus.Closed),
            AverageResolutionTimeHours = resolvedOrClosed.Count == 0 ? 0 : resolvedOrClosed.Average(),
            AverageRating = ratedFeedbacks.Count == 0 ? 0 : ratedFeedbacks.Average(),
            ResolutionRate = feedbacks.Count == 0 ? 0 : resolvedCount * 100d / feedbacks.Count,
            UnresolvedHighPriorityCount = feedbacks.Count(f =>
                f.Priority is FeedbackPriority.High or FeedbackPriority.Urgent &&
                f.Status is not (FeedbackStatus.Resolved or FeedbackStatus.Closed or FeedbackStatus.Rejected)),
            ByCategory = feedbacks.GroupBy(f => f.Category?.Name ?? "Uncategorized").ToDictionary(g => g.Key, g => g.Count()),
            ByStatus = feedbacks.GroupBy(f => f.Status.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            ByPriority = feedbacks.GroupBy(f => f.Priority.ToString()).ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<IEnumerable<StaffPerformanceReportDto>> GetStaffPerformanceAsync(ReportFilterRequest filter, Guid requestingUserId, CancellationToken ct = default)
    {
        var departmentId = await GetReportDepartmentScopeAsync(requestingUserId, ct);
        var feedbacks = (await _unitOfWork.Feedbacks.GetReportFeedbacksAsync(
            filter.FromDate,
            filter.ToDate,
            filter.CategoryId,
            filter.Status,
            filter.AssignedToUserId,
            departmentId,
            ct)).Where(f => f.AssignedToUserId.HasValue).ToList();

        return feedbacks
            .GroupBy(f => new
            {
                StaffUserId = f.AssignedToUserId!.Value,
                StaffName = f.AssignedToUser != null ? f.AssignedToUser.FullName : string.Empty
            })
            .Select(g =>
            {
                var completedDurations = g
                    .Where(f => f.Status is FeedbackStatus.Resolved or FeedbackStatus.Closed)
                    .Where(f => f.ResolvedAtUtc.HasValue || f.ClosedAtUtc.HasValue)
                    .Select(f => ((f.ResolvedAtUtc ?? f.ClosedAtUtc)!.Value - f.CreatedAtUtc).TotalHours)
                    .ToList();

                return new StaffPerformanceReportDto
                {
                    StaffUserId = g.Key.StaffUserId,
                    StaffName = g.Key.StaffName,
                    AssignedCount = g.Count(),
                    ResolvedCount = g.Count(f => f.Status is FeedbackStatus.Resolved or FeedbackStatus.Closed),
                    AverageResolutionTimeHours = completedDurations.Count == 0 ? 0 : completedDurations.Average()
                };
            })
            .OrderByDescending(x => x.AssignedCount)
            .ToList();
    }

    public async Task<IEnumerable<FeedbackTrendPointDto>> GetFeedbackTrendAsync(ReportFilterRequest filter, Guid requestingUserId, CancellationToken ct = default)
    {
        var departmentId = await GetReportDepartmentScopeAsync(requestingUserId, ct);
        var feedbacks = await _unitOfWork.Feedbacks.GetReportFeedbacksAsync(
            filter.FromDate,
            filter.ToDate,
            filter.CategoryId,
            filter.Status,
            filter.AssignedToUserId,
            departmentId,
            ct);

        return feedbacks
            .GroupBy(feedback => new { feedback.CreatedAtUtc.Year, feedback.CreatedAtUtc.Month })
            .OrderBy(group => group.Key.Year)
            .ThenBy(group => group.Key.Month)
            .Select(group => new FeedbackTrendPointDto
            {
                Period = $"{group.Key.Year:D4}-{group.Key.Month:D2}",
                TotalCount = group.Count(),
                ResolvedCount = group.Count(feedback => feedback.Status == FeedbackStatus.Resolved),
                ClosedCount = group.Count(feedback => feedback.Status == FeedbackStatus.Closed)
            })
            .ToList();
    }

    private async Task<Guid?> GetReportDepartmentScopeAsync(Guid requestingUserId, CancellationToken ct)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(requestingUserId, ct)
            ?? throw new UnauthorizedException("Authenticated user was not found.");

        if (user.Role == UserRole.SystemAdmin)
        {
            return null;
        }

        if (user.Role != UserRole.DepartmentManager)
        {
            throw new ForbiddenException("Only Department Managers and System Admins can view reports.");
        }

        return user.DepartmentId
            ?? throw new ForbiddenException("Department Manager must belong to a department.");
    }
}
