using CFMS.Domain.Common;

namespace CFMS.Domain.Entities;

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<User> Users { get; set; } = new List<User>();

    public ICollection<FeedbackCategoryEntity> Categories { get; set; } = new List<FeedbackCategoryEntity>();

    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
