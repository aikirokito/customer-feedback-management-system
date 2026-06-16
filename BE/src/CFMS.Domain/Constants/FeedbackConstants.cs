namespace CFMS.Domain.Constants;

public static class FeedbackConstants
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 5000;
    public const int MaxAttachmentsPerFeedback = 3;
    public const long MaxAttachmentSizeBytes = 5 * 1024 * 1024; // 5 MB
    public static readonly string[] AllowedAttachmentExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".docx", ".xlsx" };
}
