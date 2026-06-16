using CFMS.Domain.Constants;
using CFMS.Domain.Entities;
using CFMS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

// Alias to avoid ambiguity with CFMS.Domain.Constants.ClaimTypes
using SystemClaimTypes = System.Security.Claims.ClaimTypes;

namespace CFMS.Infrastructure.Services.Implementations;

/// <summary>
/// JWT access token generation + refresh token generation.
/// Reads config from the "Jwt" section in appsettings.
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // -------------------------------------------------------------------------
    // Access token generation
    // -------------------------------------------------------------------------

    public string GenerateAccessToken(User user)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        var issuer   = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var expiryMinutes = int.Parse(jwtSection["AccessTokenExpirationMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Use CFMS custom claim types — avoids collision with System.Security.Claims.ClaimTypes
        var claims = new[]
        {
            new Claim(AppClaimTypes.UserId,   user.Id.ToString()),
            new Claim(AppClaimTypes.Email,    user.Email),
            new Claim(AppClaimTypes.Role,     user.Role.ToString()),
            new Claim(AppClaimTypes.FullName, user.FullName),
            // Also emit standard role claim so [Authorize(Roles=...)] works out of the box
            new Claim(SystemClaimTypes.Role,  user.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // -------------------------------------------------------------------------
    // Refresh token generation
    // -------------------------------------------------------------------------

    public RefreshToken GenerateRefreshToken(Guid userId, string ipAddress)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var expiryDays = int.Parse(jwtSection["RefreshTokenExpirationDays"] ?? "30");

        return new RefreshToken
        {
            UserId      = userId,
            Token       = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(expiryDays),
            CreatedByIp = ipAddress
        };
    }

    // -------------------------------------------------------------------------
    // Validate access token — used by middleware / refresh flow
    // -------------------------------------------------------------------------

    public Guid? ValidateAccessToken(string token)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"];
        if (string.IsNullOrEmpty(secret)) return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secret);

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer   = true,
                ValidIssuer      = jwtSection["Issuer"],
                ValidateAudience = true,
                ValidAudience    = jwtSection["Audience"],
                ValidateLifetime = true,
                ClockSkew        = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            var userIdClaim = jwtToken.Claims
                .FirstOrDefault(c => c.Type == AppClaimTypes.UserId);

            return userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId)
                ? userId
                : null;
        }
        catch
        {
            // Any validation failure (expired, tampered, wrong key) → return null
            return null;
        }
    }
}
