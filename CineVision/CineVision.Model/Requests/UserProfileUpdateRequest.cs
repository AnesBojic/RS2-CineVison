namespace CineVision.Model.Requests
{
    /// <summary>
    /// Fields a logged-in user may edit on their own profile via PUT /Users/Me.
    /// Role, IsActive, Username and password are intentionally NOT editable here.
    /// </summary>
    public class UserProfileUpdateRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfileImageBase64 { get; set; }
    }
}
