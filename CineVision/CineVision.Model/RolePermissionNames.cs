using System.Text.RegularExpressions;

namespace CineVision.Model;

/// <summary>
/// Permission claim values stored on <c>Role</c> and issued in the JWT <c>Permissions</c> claim.
/// [Authorize(Policy = ...)] and desktop nav both use these strings.
/// </summary>
public static class RolePermissionNames
{
    public const string AccessDesktop = "AccessDesktop";
    public const string ManageUsers = "ManageUsers";
    public const string ManageMovies = "ManageMovies";
    public const string ManageHalls = "ManageHalls";
    public const string ManageProjections = "ManageProjections";
    public const string ManageNews = "ManageNews";
    public const string ManageReferenceData = "ManageReferenceData";
    public const string ManageRoles = "ManageRoles";
    public const string ViewAnalytics = "ViewAnalytics";
    public const string UseChatBot = "UseChatBot";

    public const string DefaultColor = "#64748B";
    public const string AdminColor = "#7C3AED";
    public const string StaffColor = "#2563EB";
    public const string CustomerColor = "#16A34A";

    public static readonly string[] All =
    {
        AccessDesktop,
        ManageUsers,
        ManageMovies,
        ManageHalls,
        ManageProjections,
        ManageNews,
        ManageReferenceData,
        ManageRoles,
        ViewAnalytics,
        UseChatBot
    };

    private static readonly Regex HexColor = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    public static bool IsHexColor(string? value) =>
        !string.IsNullOrWhiteSpace(value) && HexColor.IsMatch(value.Trim());

    public static string NormalizeColor(string? value) =>
        IsHexColor(value) ? value!.Trim().ToUpperInvariant() : DefaultColor;

    public static string ToClaimValue(
        bool canAccessDesktop,
        bool canManageUsers,
        bool canManageMovies,
        bool canManageHalls,
        bool canManageProjections,
        bool canManageNews,
        bool canManageReferenceData,
        bool canManageRoles,
        bool canViewAnalytics,
        bool canUseChatBot)
    {
        var parts = new List<string>(All.Length);
        if (canAccessDesktop) parts.Add(AccessDesktop);
        if (canManageUsers) parts.Add(ManageUsers);
        if (canManageMovies) parts.Add(ManageMovies);
        if (canManageHalls) parts.Add(ManageHalls);
        if (canManageProjections) parts.Add(ManageProjections);
        if (canManageNews) parts.Add(ManageNews);
        if (canManageReferenceData) parts.Add(ManageReferenceData);
        if (canManageRoles) parts.Add(ManageRoles);
        if (canViewAnalytics) parts.Add(ViewAnalytics);
        if (canUseChatBot) parts.Add(UseChatBot);
        return string.Join(',', parts);
    }

    public static bool ClaimContains(string? claim, string permission)
    {
        if (string.IsNullOrWhiteSpace(claim) || string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        return claim
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(value => string.Equals(value, permission, StringComparison.OrdinalIgnoreCase));
    }
}
