namespace CFMS.Application.DTOs.Responses;

public class CreateResponseRequest
{
    public Guid FeedbackId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; } = false;
}

public class UpdateResponseRequest
{
    public string Content { get; set; } = string.Empty;
}
