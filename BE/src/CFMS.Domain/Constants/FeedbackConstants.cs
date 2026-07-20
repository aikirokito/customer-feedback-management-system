namespace CFMS.Domain.Constants;

public static class FeedbackConstants
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 5000;
    public const int MaxAttachmentsPerFeedback = 3;
    public const long MaxAttachmentSizeBytes = 5 * 1024 * 1024; // 5 MB
    public static readonly string[] AllowedAttachmentExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".docx", ".xlsx" };
    public static readonly IReadOnlyDictionary<string, string[]> AllowedAttachmentContentTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".png"] = ["image/png"],
            [".gif"] = ["image/gif"],
            [".pdf"] = ["application/pdf"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]
        };
}
