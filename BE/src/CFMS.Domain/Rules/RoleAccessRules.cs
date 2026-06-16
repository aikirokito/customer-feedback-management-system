using CFMS.Domain.Enums;

namespace CFMS.Domain.Rules;

/// <summary>
/// Encodes role-based access constraints at the domain level.
/// Used by authorization services to validate permission combinations.
/// </summary>
public static class RoleAccessRules
{
    /// <summary>Admin cannot act as Manager — they have a separate, higher scope.</summary>
    public static bool CanAdminActAsManager() => false;

    /// <summary>Manager cannot delegate down and act as Staff.</summary>
    public static bool CanManagerActAsStaff() => false;

    /// <summary>Customers are external — they never access internal workflows.</summary>
    public static bool CanCustomerAccessInternalWorkflows() => false;

    /// <summary>Roles that can be assigned to feedback tickets as handlers.</summary>
    public static readonly IReadOnlyList<UserRole> InternalRoles = new[]
    {
        UserRole.SupportStaff,
        UserRole.DepartmentManager,
        UserRole.SystemAdmin
    };

    public static bool IsInternalRole(UserRole role) => InternalRoles.Contains(role);
}
