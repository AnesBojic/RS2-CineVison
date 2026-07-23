namespace eCommerce.Model.Requests
{
    /// <summary>
    /// Complete password reset using the code emailed to the user.
    /// </summary>
    public class ResetPasswordRequest
    {
        public string EmailOrUsername { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
