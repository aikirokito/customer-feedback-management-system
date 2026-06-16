using AutoMapper;
using CFMS.Application.Common.Interfaces;
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

    public async Task<FeedbackSummaryReportDto> GetFeedbackSummaryAsync(ReportFilterRequest filter, CancellationToken ct = default)
    {
        var feedbacks = (await _unitOfWork.Feedbacks.GetReportFeedbacksAsync(
            filter.FromDate,
            filter.ToDate,
            filter.Category,
            filter.Status,
            filter.AssignedToUserId,
            ct)).ToList();

        var resolvedOrClosed = feedbacks
            .Where(f => f.CreatedAtUtc != default && (f.ResolvedAtUtc.HasValue || f.ClosedAtUtc.HasValue))
            .Select(f => ((f.ResolvedAtUtc ?? f.ClosedAtUtc)!.Value - f.CreatedAtUtc).TotalHours)
            .ToList();

        return new FeedbackSummaryReportDto
        {
            TotalFeedbacks = feedbacks.Count,
            OpenFeedbacks = feedbacks.Count(f => f.Status is not FeedbackStatus.Closed and not FeedbackStatus.Rejected),
            ResolvedFeedbacks = feedbacks.Count(f => f.Status == FeedbackStatus.Resolved),
            ClosedFeedbacks = feedbacks.Count(f => f.Status == FeedbackStatus.Closed),
            AverageResolutionTimeHours = resolvedOrClosed.Count == 0 ? 0 : resolvedOrClosed.Average(),
            ByCategory = feedbacks.GroupBy(f => f.Category.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            ByStatus = feedbacks.GroupBy(f => f.Status.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            ByPriority = feedbacks.GroupBy(f => f.Priority.ToString()).ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<IEnumerable<StaffPerformanceReportDto>> GetStaffPerformanceAsync(ReportFilterRequest filter, CancellationToken ct = default)
    {
        var feedbacks = (await _unitOfWork.Feedbacks.GetReportFeedbacksAsync(
            filter.FromDate,
            filter.ToDate,
            filter.Category,
            filter.Status,
            filter.AssignedToUserId,
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
}
