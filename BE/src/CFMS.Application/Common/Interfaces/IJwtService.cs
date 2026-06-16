using CFMS.Domain.Entities;

namespace CFMS.Application.Common.Interfaces;

/// <summary>
/// JWT token generation and validation service.
/// </summary>
public interface IJwtService
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(Guid userId, string ipAddress);
    Guid? ValidateAccessToken(string token);
}
