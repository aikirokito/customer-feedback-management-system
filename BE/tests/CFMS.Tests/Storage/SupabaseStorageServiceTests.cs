using CFMS.Infrastructure.Services.Implementations;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;

namespace CFMS.Tests.Storage;

public class SupabaseStorageServiceTests
{
    [Fact]
    public async Task DeleteFileAsync_WithSecretKey_UsesApiKeyHeaderOnly()
    {
        var handler = new RecordingHandler();
        var service = CreateService(new Dictionary<string, string?>
        {
            ["SupabaseStorage:Url"] = "https://project.supabase.co",
            ["SupabaseStorage:SecretKey"] = "sb_secret_test"
        }, handler);

        await service.DeleteFileAsync("feedback/file.pdf", "cfms-attachments");

        handler.Request.Should().NotBeNull();
        handler.Request!.Headers.GetValues("apikey").Should().ContainSingle("sb_secret_test");
        handler.Request.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFileAsync_WithLegacyServiceRoleKey_UsesBearerAndApiKeyHeaders()
    {
        var handler = new RecordingHandler();
        var service = CreateService(new Dictionary<string, string?>
        {
            ["SupabaseStorage:Url"] = "https://project.supabase.co",
            ["SupabaseStorage:SecretKey"] = "YOUR_SUPABASE_SECRET_KEY",
            ["SupabaseStorage:ServiceRoleKey"] = "legacy-service-role-jwt"
        }, handler);

        await service.DeleteFileAsync("feedback/file.pdf", "cfms-attachments");

        handler.Request.Should().NotBeNull();
        handler.Request!.Headers.GetValues("apikey").Should().ContainSingle("legacy-service-role-jwt");
        handler.Request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.Request.Headers.Authorization.Parameter.Should().Be("legacy-service-role-jwt");
    }

    private static SupabaseStorageService CreateService(
        IDictionary<string, string?> values,
        HttpMessageHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new SupabaseStorageService(
            configuration,
            Mock.Of<ILogger<SupabaseStorageService>>(),
            new HttpClient(handler));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
