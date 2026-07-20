using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using CFMS.Infrastructure.Services.Implementations;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CFMS.Tests.Auth;

public class JwtServiceTests
{
    private readonly JwtService _service;

    public JwtServiceTests()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "test-secret-that-is-longer-than-thirty-two-characters",
            ["Jwt:Issuer"] = "cfms-tests",
            ["Jwt:Audience"] = "cfms-test-clients",
            ["Jwt:AccessTokenExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7"
        }).Build();
        _service = new JwtService(configuration);
    }

    [Fact]
    public void GenerateAndValidateAccessToken_ReturnsUserId()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "user@example.com", FirstName = "Test", LastName = "User", Role = UserRole.Customer };
        var token = _service.GenerateAccessToken(user);
        _service.ValidateAccessToken(token).Should().Be(user.Id);
    }

    [Fact]
    public void GenerateRefreshToken_UsesCryptographicallyUniqueValues()
    {
        var userId = Guid.NewGuid();
        var first = _service.GenerateRefreshToken(userId, "127.0.0.1");
        var second = _service.GenerateRefreshToken(userId, "127.0.0.1");
        first.Token.Should().NotBe(second.Token);
        first.UserId.Should().Be(userId);
        first.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow.AddDays(6));
    }

    [Fact]
    public void ValidateAccessToken_WhenTampered_ReturnsNull()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "user@example.com", Role = UserRole.Customer };
        var token = _service.GenerateAccessToken(user);
        var tampered = token[..^1] + (token[^1] == 'a' ? 'b' : 'a');
        _service.ValidateAccessToken(tampered).Should().BeNull();
    }
}
