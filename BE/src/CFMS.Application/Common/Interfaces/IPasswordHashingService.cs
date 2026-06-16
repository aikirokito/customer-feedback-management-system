namespace CFMS.Application.Common.Interfaces;

/// <summary>
/// Password hashing and verification.
/// </summary>
public interface IPasswordHashingService
{
    string HashPassword(string plainTextPassword);
    bool VerifyPassword(string plainTextPassword, string hashedPassword);
}
