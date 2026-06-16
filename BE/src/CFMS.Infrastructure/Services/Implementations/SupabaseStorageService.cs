using CFMS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CFMS.Infrastructure.Services.Implementations;

/// <summary>
/// Supabase Storage wrapper using direct REST API calls.
/// Supabase Storage is S3-compatible; this can be replaced with an official SDK when available.
/// </summary>
public class SupabaseStorageService : ISupabaseStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SupabaseStorageService> _logger;
    private readonly HttpClient _httpClient;

    public SupabaseStorageService(
        IConfiguration configuration,
        ILogger<SupabaseStorageService> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string bucketName, CancellationToken ct = default)
    {
        var storageKey = fileName;
        var requestUrl = $"{_configuration["SupabaseStorage:Url"]?.TrimEnd('/')}/storage/v1/object/{bucketName}/{storageKey}";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Add("Authorization", $"Bearer {_configuration["SupabaseStorage:ServiceRoleKey"]}");
        request.Headers.Add("apikey", _configuration["SupabaseStorage:ServiceRoleKey"]);

        var content = new StreamContent(fileStream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        request.Content = content;

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorMsg = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Supabase upload failed: {StatusCode} - {Error}", response.StatusCode, errorMsg);
            throw new HttpRequestException($"Supabase upload failed: {response.StatusCode} - {errorMsg}");
        }

        return storageKey;
    }

    public Task<string> GetPublicUrlAsync(string storageKey, string bucketName)
    {
        var baseUrl = _configuration["SupabaseStorage:Url"]?.TrimEnd('/');
        return Task.FromResult($"{baseUrl}/storage/v1/object/public/{bucketName}/{storageKey}");
    }

    public async Task DeleteFileAsync(string storageKey, string bucketName, CancellationToken ct = default)
    {
        var requestUrl = $"{_configuration["SupabaseStorage:Url"]?.TrimEnd('/')}/storage/v1/object/{bucketName}/{storageKey}";

        using var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
        request.Headers.Add("Authorization", $"Bearer {_configuration["SupabaseStorage:ServiceRoleKey"]}");
        request.Headers.Add("apikey", _configuration["SupabaseStorage:ServiceRoleKey"]);

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorMsg = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Supabase delete failed: {StatusCode} - {Error}", response.StatusCode, errorMsg);
            throw new HttpRequestException($"Supabase delete failed: {response.StatusCode} - {errorMsg}");
        }
    }

    public async Task<bool> FileExistsAsync(string storageKey, string bucketName, CancellationToken ct = default)
    {
        var requestUrl = $"{_configuration["SupabaseStorage:Url"]?.TrimEnd('/')}/storage/v1/object/{bucketName}/{storageKey}";

        using var request = new HttpRequestMessage(HttpMethod.Head, requestUrl);
        request.Headers.Add("Authorization", $"Bearer {_configuration["SupabaseStorage:ServiceRoleKey"]}");
        request.Headers.Add("apikey", _configuration["SupabaseStorage:ServiceRoleKey"]);

        var response = await _httpClient.SendAsync(request, ct);
        return response.IsSuccessStatusCode;
    }
}
