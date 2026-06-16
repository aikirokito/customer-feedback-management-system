using CFMS.Application.DTOs.Reports;

namespace CFMS.Application.Services.Interfaces;

public interface IReportService
{
    Task<FeedbackSummaryReportDto> GetFeedbackSummaryAsync(ReportFilterRequest filter, CancellationToken ct = default);
    Task<IEnumerable<StaffPerformanceReportDto>> GetStaffPerformanceAsync(ReportFilterRequest filter, CancellationToken ct = default);
}
