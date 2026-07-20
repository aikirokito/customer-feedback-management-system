using CFMS.Application.DTOs.Reports;

namespace CFMS.Application.Services.Interfaces;

public interface IReportService
{
    Task<FeedbackSummaryReportDto> GetFeedbackSummaryAsync(ReportFilterRequest filter, Guid requestingUserId, CancellationToken ct = default);
    Task<IEnumerable<StaffPerformanceReportDto>> GetStaffPerformanceAsync(ReportFilterRequest filter, Guid requestingUserId, CancellationToken ct = default);
    Task<IEnumerable<FeedbackTrendPointDto>> GetFeedbackTrendAsync(ReportFilterRequest filter, Guid requestingUserId, CancellationToken ct = default);
}
