namespace CFMS.Domain.Constants;

public static class AuthConstants
{
    public const int RefreshTokenExpiryDays = 30;
    public const int AccessTokenExpiryMinutes = 60;
    public const int MaxFailedLoginAttempts = 5;
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;
}
