namespace CFMS.Application.DTOs.Comments;

public class CreateCommentRequest
{
    public Guid FeedbackId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
}

public class UpdateCommentRequest
{
    public string Content { get; set; } = string.Empty;
}

public class CommentDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public IEnumerable<CommentDto> Replies { get; set; } = new List<CommentDto>();
}
