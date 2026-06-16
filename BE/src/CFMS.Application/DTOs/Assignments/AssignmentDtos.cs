namespace CFMS.Application.DTOs.Assignments;

public class AssignFeedbackRequest
{
    public Guid FeedbackId { get; set; }
    public Guid AssignToUserId { get; set; }
    public string? Note { get; set; }
}

public class AssignmentDto
{
    public Guid Id { get; set; }
    public Guid FeedbackId { get; set; }
    public string AssignedToUserName { get; set; } = string.Empty;
    public string AssignedByUserName { get; set; } = string.Empty;
    public string? Note { get; set; }
    public bool IsActive { get; set; }
    public DateTime AssignedAtUtc { get; set; }
}
