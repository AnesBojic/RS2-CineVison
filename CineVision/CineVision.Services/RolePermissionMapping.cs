using CineVision.Model;
using CineVision.Model.Requests;
using CineVision.Services.Database;

namespace CineVision.Services;

internal static class RolePermissionMapping
{
    public static string ToClaimValue(Role role) =>
        RolePermissionNames.ToClaimValue(
            role.CanAccessDesktop,
            role.CanManageUsers,
            role.CanManageMovies,
            role.CanManageHalls,
            role.CanManageProjections,
            role.CanManageNews,
            role.CanManageReferenceData,
            role.CanManageRoles,
            role.CanViewAnalytics,
            role.CanUseChatBot);

    public static string Snapshot(Role role) =>
        $"{ToClaimValue(role)}|{RolePermissionNames.NormalizeColor(role.Color)}";

    public static string Snapshot(RoleUpdateRequest request) =>
        RolePermissionNames.ToClaimValue(
            request.CanAccessDesktop,
            request.CanManageUsers,
            request.CanManageMovies,
            request.CanManageHalls,
            request.CanManageProjections,
            request.CanManageNews,
            request.CanManageReferenceData,
            request.CanManageRoles,
            request.CanViewAnalytics,
            request.CanUseChatBot)
        + "|" + RolePermissionNames.NormalizeColor(request.Color);

    public static void ApplyFullAccess(RoleUpdateRequest request)
    {
        request.CanAccessDesktop = true;
        request.CanManageUsers = true;
        request.CanManageMovies = true;
        request.CanManageHalls = true;
        request.CanManageProjections = true;
        request.CanManageNews = true;
        request.CanManageReferenceData = true;
        request.CanManageRoles = true;
        request.CanViewAnalytics = true;
        request.CanUseChatBot = true;
    }

    public static void NormalizeColor(RoleInsertRequest request) =>
        request.Color = RolePermissionNames.NormalizeColor(request.Color);

    public static void NormalizeColor(RoleUpdateRequest request) =>
        request.Color = RolePermissionNames.NormalizeColor(request.Color);
}
