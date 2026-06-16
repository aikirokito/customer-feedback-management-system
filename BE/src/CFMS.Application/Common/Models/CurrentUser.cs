using CFMS.Domain.Enums;

namespace CFMS.Application.Common.Models;

/// <summary>
/// Carries the authenticated user's identity through the request pipeline.
/// Populated by CurrentUserMiddleware or extracted in BaseController.
/// </summary>
public class CurrentUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    public bool IsAdmin => Role == UserRole.SystemAdmin;
    public bool IsManager => Role == UserRole.DepartmentManager;
    public bool IsStaff => Role == UserRole.SupportStaff;
    public bool IsCustomer => Role == UserRole.Customer;
    public bool IsInternal => Role != UserRole.Customer;
}
