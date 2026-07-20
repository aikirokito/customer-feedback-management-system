using CFMS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

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
        bucketName = ResolveBucketName(bucketName);
        var storageKey = fileName;
        var requestUrl = $"{_configuration["SupabaseStorage:Url"]?.TrimEnd('/')}/storage/v1/object/{bucketName}/{storageKey}";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        AddServerAuthorization(request);

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
        bucketName = ResolveBucketName(bucketName);
        var configuredPublicUrl = _configuration["SupabaseStorage:PublicBucketUrl"]?.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(configuredPublicUrl) && !configuredPublicUrl.Contains("YOUR_PROJECT_REF", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult($"{configuredPublicUrl}/{storageKey}");
        }

        var baseUrl = _configuration["SupabaseStorage:Url"]?.TrimEnd('/');
        return Task.FromResult($"{baseUrl}/storage/v1/object/public/{bucketName}/{storageKey}");
    }

    public async Task DeleteFileAsync(string storageKey, string bucketName, CancellationToken ct = default)
    {
        bucketName = ResolveBucketName(bucketName);
        var requestUrl = $"{_configuration["SupabaseStorage:Url"]?.TrimEnd('/')}/storage/v1/object/{bucketName}/{storageKey}";

        using var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
        AddServerAuthorization(request);

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
        bucketName = ResolveBucketName(bucketName);
        var requestUrl = $"{_configuration["SupabaseStorage:Url"]?.TrimEnd('/')}/storage/v1/object/{bucketName}/{storageKey}";

        using var request = new HttpRequestMessage(HttpMethod.Head, requestUrl);
        AddServerAuthorization(request);

        var response = await _httpClient.SendAsync(request, ct);
        return response.IsSuccessStatusCode;
    }

    private string ResolveBucketName(string fallback)
        => string.IsNullOrWhiteSpace(_configuration["SupabaseStorage:BucketName"])
            ? fallback
            : _configuration["SupabaseStorage:BucketName"]!;

    private void AddServerAuthorization(HttpRequestMessage request)
    {
        var secretKey = _configuration["SupabaseStorage:SecretKey"];
        var legacyServiceRoleKey = _configuration["SupabaseStorage:ServiceRoleKey"];
        var apiKey = IsConfigured(secretKey) ? secretKey! : legacyServiceRoleKey;

        if (!IsConfigured(apiKey))
        {
            throw new InvalidOperationException(
                "Supabase Storage requires SupabaseStorage:SecretKey or the legacy SupabaseStorage:ServiceRoleKey configuration value.");
        }

        request.Headers.Add("apikey", apiKey);

        // New sb_secret_ keys are API keys rather than JWTs and must not be sent as Bearer tokens.
        // Legacy service_role keys are JWTs and still require the Authorization header for RLS bypass.
        if (!apiKey!.StartsWith("sb_secret_", StringComparison.Ordinal))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    private static bool IsConfigured(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && !value.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);
}
