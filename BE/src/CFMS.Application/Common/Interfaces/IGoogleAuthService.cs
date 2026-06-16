namespace CFMS.Application.Common.Interfaces;

/// <summary>
/// Service contract to validate Google ID Tokens.
/// </summary>
public interface IGoogleAuthService
{
    /// <summary>
    /// Validates a Google ID Token and returns the external subject identifier (sub) and email.
    /// </summary>
    Task<(string Subject, string Email, string FirstName, string LastName)> ValidateTokenAsync(string idToken, CancellationToken ct = default);
}
