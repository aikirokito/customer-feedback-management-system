using CFMS.Domain.Enums;

namespace CFMS.Application.DTOs.Users;

public class UserListItemDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; }
    public string RoleName => Role.ToString();
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class UserDetailDto : UserListItemDto
{
    public string? PhoneNumber { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}

public class UpdateUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

public class UpdateUserRoleRequest
{
    public UserRole Role { get; set; }
}

public class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
}
