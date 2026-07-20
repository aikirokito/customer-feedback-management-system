using CFMS.Domain.Common;

namespace CFMS.Domain.Entities;

public class FeedbackCategoryEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
