namespace CineVision.Model;

/// <summary>
/// Canonical application role names. Use these instead of magic strings in
/// [Authorize], validators, and seed data.
/// </summary>
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Customer = "Customer";

    /// <summary>Admin or Staff — desktop / management operations.</summary>
    public const string AdminStaff = Admin + "," + Staff;

    /// <summary>Any authenticated app role.</summary>
    public const string All = Admin + "," + Staff + "," + Customer;

    public static readonly string[] AllRoles = { Admin, Staff, Customer };

    public static bool IsKnown(string? role) =>
        role is Admin or Staff or Customer;
}
