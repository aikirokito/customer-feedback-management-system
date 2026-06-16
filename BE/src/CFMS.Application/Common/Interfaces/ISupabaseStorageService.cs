namespace CFMS.Application.Common.Interfaces;

/// <summary>
/// Supabase Storage file operations wrapper.
/// </summary>
public interface ISupabaseStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string bucketName, CancellationToken ct = default);
    Task<string> GetPublicUrlAsync(string storageKey, string bucketName);
    Task DeleteFileAsync(string storageKey, string bucketName, CancellationToken ct = default);
    Task<bool> FileExistsAsync(string storageKey, string bucketName, CancellationToken ct = default);
}
