using CFMS.Application.Common.Interfaces;

namespace CFMS.Infrastructure.Services.Implementations;

public class PasswordHashingService : IPasswordHashingService
{
    public string HashPassword(string plainTextPassword)
        => BCrypt.Net.BCrypt.HashPassword(plainTextPassword, workFactor: 12);

    public bool VerifyPassword(string plainTextPassword, string hashedPassword)
        => BCrypt.Net.BCrypt.Verify(plainTextPassword, hashedPassword);
}
