using System.IO;

namespace CFMS.Application.Common.Models;

/// <summary>
/// Application-layer wrapper for uploaded files to decouple from ASP.NET Core web framework types.
/// </summary>
public class UploadedFileInput
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Length { get; set; }
    public Stream Content { get; set; } = Stream.Null;
}
