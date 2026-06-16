using CFMS.Domain.Enums;

namespace CFMS.Application.DTOs.Reports;

public class ReportFilterRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public FeedbackCategory? Category { get; set; }
    public FeedbackStatus? Status { get; set; }
    public Guid? AssignedToUserId { get; set; }
}

public class FeedbackSummaryReportDto
{
    public int TotalFeedbacks { get; set; }
    public int OpenFeedbacks { get; set; }
    public int ResolvedFeedbacks { get; set; }
    public int ClosedFeedbacks { get; set; }
    public double AverageResolutionTimeHours { get; set; }
    public Dictionary<string, int> ByCategory { get; set; } = new();
    public Dictionary<string, int> ByStatus { get; set; } = new();
    public Dictionary<string, int> ByPriority { get; set; } = new();
}

public class StaffPerformanceReportDto
{
    public Guid StaffUserId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public int AssignedCount { get; set; }
    public int ResolvedCount { get; set; }
    public double AverageResolutionTimeHours { get; set; }
}
