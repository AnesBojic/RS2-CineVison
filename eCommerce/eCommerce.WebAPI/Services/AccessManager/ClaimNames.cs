namespace eCommerce.WebAPI.Services.AccessManager
{
    public class ClaimNames
    {
        public static readonly string Id = "Id";
        public static readonly string FirstName = "FirstName";
        public static readonly string LastName = "LastName";
        public static readonly string Email = "Email";
        public static readonly string Role = "Role";
        public static readonly string IsActive = "IsActive";

        /// <summary>Session generation the token was issued for; see ITokenRevocationService.</summary>
        public static readonly string TokenVersion = "TokenVersion";
    }
}
