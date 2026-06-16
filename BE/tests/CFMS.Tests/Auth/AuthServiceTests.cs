using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.DTOs.Auth;
using CFMS.Application.Mappings;
using CFMS.Application.Services.Implementations;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace CFMS.Tests.Auth;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IPasswordHashingService> _passwordHashingMock;
    private readonly Mock<IGoogleAuthService> _googleAuthMock;
    private readonly IMapper _mapper;

    public AuthServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _jwtServiceMock = new Mock<IJwtService>();
        _passwordHashingMock = new Mock<IPasswordHashingService>();
        _googleAuthMock = new Mock<IGoogleAuthService>();

        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.RefreshTokens).Returns(_refreshTokenRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.AuditLogs).Returns(_auditLogRepositoryMock.Object);

        var config = new MapperConfiguration(cfg => cfg.AddProfile<UserMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private AuthService CreateService() => new(
        _unitOfWorkMock.Object,
        _jwtServiceMock.Object,
        _passwordHashingMock.Object,
        _googleAuthMock.Object,
        _mapper
    );

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "Password123!" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hashed_pwd",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Customer,
            Status = UserStatus.Active
        };
        var mockToken = new RefreshToken
        {
            Token = "refresh_token_xyz",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHashingMock.Setup(h => h.VerifyPassword("Password123!", "hashed_pwd"))
            .Returns(true);
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(user))
            .Returns("access_token_xyz");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken(user.Id, "127.0.0.1"))
            .Returns(mockToken);

        var service = CreateService();

        // Act
        var result = await service.LoginAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token_xyz");
        result.RefreshToken.Should().Be("refresh_token_xyz");
        result.User.Email.Should().Be("test@example.com");
        _userRepositoryMock.Verify(r => r.Update(user), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "WrongPassword!" };
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hashed_pwd",
            Status = UserStatus.Active
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHashingMock.Setup(h => h.VerifyPassword("WrongPassword!", "hashed_pwd"))
            .Returns(false);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(request, "127.0.0.1"));
    }

    [Fact]
    public async Task Login_WithInactiveUser_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "Password123!" };
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hashed_pwd",
            Status = UserStatus.Disabled
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHashingMock.Setup(h => h.VerifyPassword("Password123!", "hashed_pwd"))
            .Returns(true);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(request, "127.0.0.1"));
    }

    [Fact]
    public async Task Register_WithExistingEmail_ThrowsConflictException()
    {
        // Arrange
        var request = new RegisterRequest { Email = "existing@example.com", Password = "Password123!" };
        _userRepositoryMock.Setup(r => r.IsEmailTakenAsync("existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => service.RegisterAsync(request));
    }

    [Fact]
    public async Task Register_WithValidData_CreatesCustomerRole()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "new@example.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            PhoneNumber = "123456789"
        };
        var mockToken = new RefreshToken { Token = "token_xyz", ExpiresAtUtc = DateTime.UtcNow.AddDays(7) };

        _userRepositoryMock.Setup(r => r.IsEmailTakenAsync("new@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHashingMock.Setup(h => h.HashPassword("Password123!"))
            .Returns("new_hashed_pwd");
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>()))
            .Returns("access_xyz");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns(mockToken);

        var service = CreateService();

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.User.Role.Should().Be(UserRole.Customer);
        _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == "new@example.com" && u.Role == UserRole.Customer), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RefreshToken_WithExpiredToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var expiredToken = new RefreshToken
        {
            Token = "expired_token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
            User = new User { Status = UserStatus.Active }
        };

        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync("expired_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => service.RefreshTokenAsync("expired_token", "127.0.0.1"));
    }

    [Fact]
    public async Task RefreshToken_WithRevokedToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var revokedToken = new RefreshToken
        {
            Token = "revoked_token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            IsRevoked = true,
            User = new User { Status = UserStatus.Active }
        };

        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync("revoked_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => service.RefreshTokenAsync("revoked_token", "127.0.0.1"));
    }

    [Fact]
    public async Task GoogleLogin_WithValidIdToken_ReturnsAuthResponse()
    {
        // Arrange
        var request = new GoogleLoginRequest { IdToken = "valid_google_token" };
        var googleSub = "google_sub_123";
        var googleEmail = "google@example.com";
        var firstName = "Google";
        var lastName = "User";

        _googleAuthMock.Setup(g => g.ValidateTokenAsync("valid_google_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((googleSub, googleEmail, firstName, lastName));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = googleEmail,
            GoogleSubject = googleSub,
            FirstName = firstName,
            LastName = lastName,
            Role = UserRole.Customer,
            Status = UserStatus.Active
        };

        var mockToken = new RefreshToken { Token = "google_refresh_token", ExpiresAtUtc = DateTime.UtcNow.AddDays(7) };

        _userRepositoryMock.Setup(r => r.GetByGoogleSubjectAsync(googleSub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(user))
            .Returns("google_access");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken(user.Id, "127.0.0.1"))
            .Returns(mockToken);

        var service = CreateService();

        // Act
        var result = await service.GoogleLoginAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("google_access");
        result.RefreshToken.Should().Be("google_refresh_token");
        result.User.Email.Should().Be(googleEmail);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        // Arrange
        var token = new RefreshToken
        {
            Token = "token_to_logout",
            UserId = Guid.NewGuid(),
            IsRevoked = false
        };

        _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync("token_to_logout", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var service = CreateService();

        // Act
        await service.LogoutAsync("token_to_logout");

        // Assert
        token.IsRevoked.Should().BeTrue();
        token.RevokedAtUtc.Should().NotBeNull();
        _refreshTokenRepositoryMock.Verify(r => r.Update(token), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WithIncorrectCurrentPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ChangePasswordRequest { CurrentPassword = "WrongPassword!", NewPassword = "NewPassword123!" };
        var user = new User
        {
            Id = userId,
            PasswordHash = "hashed_pwd"
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHashingMock.Setup(h => h.VerifyPassword("WrongPassword!", "hashed_pwd"))
            .Returns(false);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => service.ChangePasswordAsync(userId, request));
    }
}
