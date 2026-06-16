namespace CFMS.Domain.Constants;

/// <summary>
/// Custom JWT claim type constants used across CFMS.
/// Named "AppClaimTypes" to avoid collision with System.Security.Claims.ClaimTypes.
/// </summary>
public static class AppClaimTypes
{
    public const string UserId   = "uid";
    public const string Email    = "email";
    public const string Role     = "role";
    public const string FullName = "fullName";
}

/// <summary>
/// Backward-compat alias — use AppClaimTypes in new code.
/// </summary>
[Obsolete("Use AppClaimTypes instead to avoid System.Security.Claims.ClaimTypes ambiguity.")]
public static class ClaimTypes
{
    public const string UserId   = AppClaimTypes.UserId;
    public const string Email    = AppClaimTypes.Email;
    public const string Role     = AppClaimTypes.Role;
    public const string FullName = AppClaimTypes.FullName;
}
