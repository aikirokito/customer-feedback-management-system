using CFMS.Application.DTOs.Reports;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CFMS.Api.Controllers;

/// <summary>
/// Reporting endpoints. Manager and Admin only.
/// </summary>
[Authorize(Roles = $"{RoleNames.DepartmentManager},{RoleNames.SystemAdmin}")]
[Tags("Reports")]
public class ReportsController : BaseController
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("summary")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetFeedbackSummary([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var result = await _reportService.GetFeedbackSummaryAsync(filter, ct);
        return OkResponse(result);
    }

    [HttpGet("feedback-by-status")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetFeedbackByStatus([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var result = await _reportService.GetFeedbackSummaryAsync(filter, ct);
        return OkResponse(result.ByStatus);
    }

    [HttpGet("feedback-by-category")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetFeedbackByCategory([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var result = await _reportService.GetFeedbackSummaryAsync(filter, ct);
        return OkResponse(result.ByCategory);
    }

    [HttpGet("feedback-by-priority")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetFeedbackByPriority([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var result = await _reportService.GetFeedbackSummaryAsync(filter, ct);
        return OkResponse(result.ByPriority);
    }

    [HttpGet("staff-workload")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetStaffPerformance([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var result = await _reportService.GetStaffPerformanceAsync(filter, ct);
        return OkResponse(result);
    }
}
