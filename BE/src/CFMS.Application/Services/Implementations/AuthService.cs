using AutoMapper;
using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.DTOs.Auth;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;

namespace CFMS.Application.Services.Implementations;

/// <summary>
/// Implements all authentication flows: register, login, token refresh,
/// Google OAuth, logout, and password change.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHashingService _passwordHashing;
    private readonly IGoogleAuthService _googleAuth;
    private readonly IMapper _mapper;

    public AuthService(
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IPasswordHashingService passwordHashing,
        IGoogleAuthService googleAuth,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _passwordHashing = passwordHashing;
        _googleAuth = googleAuth;
        _mapper = mapper;
    }

    // -------------------------------------------------------------------------
    // Register
    // -------------------------------------------------------------------------

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        // 1. Reject duplicate email
        var emailTaken = await _unitOfWork.Users.IsEmailTakenAsync(request.Email.ToLowerInvariant(), ct);
        if (emailTaken)
            throw new ConflictException($"Email '{request.Email}' is already registered.");

        // 2. Hash password and create user with Customer role
        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordHashing.HashPassword(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Role = UserRole.Customer,
            Status = UserStatus.Active,
            IsEmailVerified = false
        };

        await _unitOfWork.Users.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 3. Issue tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken(user.Id, string.Empty);

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken, ct);

        // 4. Log registration
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            UserId = user.Id,
            Action = AuditAction.Create,
            EntityType = nameof(User),
            EntityId = user.Id
        }, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return BuildAuthResponse(accessToken, refreshToken, user);
    }

    // -------------------------------------------------------------------------
    // Login
    // -------------------------------------------------------------------------

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress, CancellationToken ct = default)
    {
        // 1. Find user — use generic error to avoid user enumeration
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email.ToLowerInvariant(), ct);
        if (user == null || user.PasswordHash == null || !_passwordHashing.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        // 2. Reject disabled accounts
        if (!user.IsActive)
            throw new UnauthorizedException("This account has been disabled. Please contact support.");

        // 3. Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken(user.Id, ipAddress);

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken, ct);

        // 4. Update last login
        user.LastLoginAtUtc = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);

        // 5. Audit log
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            UserId = user.Id,
            Action = AuditAction.Login,
            EntityType = nameof(User),
            EntityId = user.Id,
            IpAddress = ipAddress
        }, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return BuildAuthResponse(accessToken, refreshToken, user);
    }

    // -------------------------------------------------------------------------
    // Refresh Token
    // -------------------------------------------------------------------------

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken ct = default)
    {
        // 1. Find token record (eager-loads User)
        var storedToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken, ct);

        if (storedToken == null || !storedToken.IsActive)
            throw new UnauthorizedException("Refresh token is invalid or has expired.");

        var user = storedToken.User;

        if (!user.IsActive)
            throw new UnauthorizedException("This account has been disabled.");

        // 2. Rotate — revoke old, create new
        storedToken.IsRevoked = true;
        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;

        var newRefreshToken = _jwtService.GenerateRefreshToken(user.Id, ipAddress);
        storedToken.ReplacedByToken = newRefreshToken.Token;

        _unitOfWork.RefreshTokens.Update(storedToken);
        await _unitOfWork.RefreshTokens.AddAsync(newRefreshToken, ct);

        // 3. New access token
        var newAccessToken = _jwtService.GenerateAccessToken(user);

        await _unitOfWork.SaveChangesAsync(ct);

        return BuildAuthResponse(newAccessToken, newRefreshToken, user);
    }

    // -------------------------------------------------------------------------
    // Logout
    // -------------------------------------------------------------------------

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var storedToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken, ct);

        // Silently succeed if token is already gone/revoked — prevents information leakage
        if (storedToken == null || storedToken.IsRevoked)
            return;

        storedToken.IsRevoked = true;
        storedToken.RevokedAtUtc = DateTime.UtcNow;

        _unitOfWork.RefreshTokens.Update(storedToken);

        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            UserId = storedToken.UserId,
            Action = AuditAction.Logout,
            EntityType = nameof(User),
            EntityId = storedToken.UserId
        }, ct);

        await _unitOfWork.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // Google Login / Register
    // -------------------------------------------------------------------------

    public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request, string ipAddress, CancellationToken ct = default)
    {
        // 1. Validate Google ID token
        var (subject, email, firstName, lastName) = await _googleAuth.ValidateTokenAsync(request.IdToken, ct);

        // 2. Find existing user by Google subject
        var user = await _unitOfWork.Users.GetByGoogleSubjectAsync(subject, ct);

        if (user == null)
        {
            // 2a. Try to find by email — link if found and not already linked
            var existingByEmail = await _unitOfWork.Users.GetByEmailAsync(email.ToLowerInvariant(), ct);
            if (existingByEmail != null)
            {
                if (existingByEmail.GoogleSubject != null && existingByEmail.GoogleSubject != subject)
                    throw new ConflictException("This email is already linked to a different Google account.");

                // Safe to link
                existingByEmail.GoogleSubject = subject;
                existingByEmail.IsEmailVerified = true;
                _unitOfWork.Users.Update(existingByEmail);
                user = existingByEmail;
            }
            else
            {
                // 2b. Create a new Customer account
                user = new User
                {
                    Email = email.ToLowerInvariant(),
                    GoogleSubject = subject,
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim(),
                    Role = UserRole.Customer,
                    Status = UserStatus.Active,
                    IsEmailVerified = true
                };
                await _unitOfWork.Users.AddAsync(user, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }
        }

        // 3. Reject disabled accounts
        if (!user.IsActive)
            throw new UnauthorizedException("This account has been disabled. Please contact support.");

        // 4. Issue CFMS tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken(user.Id, ipAddress);

        user.LastLoginAtUtc = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.RefreshTokens.AddAsync(refreshToken, ct);

        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            UserId = user.Id,
            Action = AuditAction.Login,
            EntityType = nameof(User),
            EntityId = user.Id,
            IpAddress = ipAddress,
            NewValues = "google-oauth"
        }, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return BuildAuthResponse(accessToken, refreshToken, user);
    }

    // -------------------------------------------------------------------------
    // Change Password
    // -------------------------------------------------------------------------

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        // 1. Load authenticated user
        var user = await _unitOfWork.Users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        // 2. Verify current password (OAuth-only users have no password hash)
        if (user.PasswordHash == null || !_passwordHashing.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Current password is incorrect.");

        // 3. Hash new password and save
        user.PasswordHash = _passwordHashing.HashPassword(request.NewPassword);
        _unitOfWork.Users.Update(user);

        // 4. Revoke all active refresh tokens (force re-login on all devices)
        await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(userId, ct);

        // 5. Audit log
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = AuditAction.Update,
            EntityType = nameof(User),
            EntityId = userId,
            NewValues = "password-changed"
        }, ct);

        await _unitOfWork.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // Private helper
    // -------------------------------------------------------------------------

    private static AuthResponse BuildAuthResponse(string accessToken, RefreshToken refreshToken, User user)
    {
        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAtUtc = refreshToken.ExpiresAtUtc, // access token expiry is baked into the JWT; this surfaces refresh expiry
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role,
                Status = user.Status
            }
        };
    }
}
