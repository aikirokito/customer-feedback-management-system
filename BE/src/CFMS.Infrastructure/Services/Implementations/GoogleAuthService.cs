using CFMS.Application.Common.Interfaces;
using CFMS.Application.Common.Exceptions;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace CFMS.Infrastructure.Services.Implementations;

/// <summary>
/// Concrete wrapper for Google API ID Token validation.
/// </summary>
public class GoogleAuthService : IGoogleAuthService
{
    private readonly IConfiguration _configuration;

    public GoogleAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<(string Subject, string Email, string FirstName, string LastName)> ValidateTokenAsync(string idToken, CancellationToken ct = default)
    {
        var clientId = _configuration["GoogleAuth:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId) || clientId == "YOUR_GOOGLE_CLIENT_ID")
        {
            throw new InvalidOperationException("GoogleAuth:ClientId must be configured before Google login can be used.");
        }

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { clientId }
        };

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            return (payload.Subject, payload.Email, payload.GivenName ?? "", payload.FamilyName ?? "");
        }
        catch (InvalidJwtException)
        {
            throw new UnauthorizedException("Invalid Google ID token.");
        }
    }
}
