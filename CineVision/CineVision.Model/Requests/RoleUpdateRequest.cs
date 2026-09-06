using System.ComponentModel.DataAnnotations;
using CineVision.Model;

namespace CineVision.Model.Requests
{
    public class RoleUpdateRequest : LookupRequest
    {
        [MaxLength(7)]
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
    }
}
