using CineVision.Model;

namespace CineVision.Model.Responses
{
    public class RoleResponse : LookupResponse
    {
        public string Color { get; set; } = RolePermissionNames.DefaultColor;

        public bool CanAccessDesktop { get; set; }
        public bool CanManageUsers { get; set; }
        public bool CanManageMovies { get; set; }
        public bool CanManageHalls { get; set; }
        public bool CanManageProjections { get; set; }
        public bool CanManageNews { get; set; }
        public bool CanManageReferenceData { get; set; }
        public bool CanManageRoles { get; set; }
        public bool CanViewAnalytics { get; set; }
        public bool CanUseChatBot { get; set; }

        /// <summary>True when this is Admin or Customer — the name cannot be renamed or deleted.</summary>
        public bool IsSystemRole { get; set; }

        /// <summary>Admin permissions stay full so the last administrator cannot lock themselves out.</summary>
        public bool PermissionsLocked { get; set; }
    }
}
