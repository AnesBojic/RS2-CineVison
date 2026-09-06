namespace CineVision.Model;

/// <summary>
/// Canonical application role names. Admin and Customer stay frozen as system roles;
/// Staff is optional and may be renamed or deleted when unused.
/// Authorization itself is permission-based (see <see cref="RolePermissionNames"/>).
/// </summary>
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Customer = "Customer";

    /// <summary>Admin or Staff. Kept for any remaining role-name checks; authorization is permission-based.</summary>
    public const string AdminStaff = Admin + "," + Staff;

    /// <summary>Any authenticated app role.</summary>
    public const string All = Admin + "," + Staff + "," + Customer;

    public static readonly string[] AllRoles = { Admin, Staff, Customer };

    /// <summary>Admin and Customer — names stay frozen and the rows cannot be deleted.</summary>
    public static readonly string[] SystemRoles = { Admin, Customer };

    /// <summary>Seeded system roles whose names stay frozen. Assignment is any row in Roles.</summary>
    public static bool IsSystemRole(string? role) =>
        role is Admin or Customer;
}
