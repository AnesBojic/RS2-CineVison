namespace eCommerce.Model.Requests
{
    /// <summary>
    /// Request a password-reset code by email or username.
    /// </summary>
    public class ForgotPasswordRequest
    {
        /// <summary>Email address or username of the account.</summary>
        public string EmailOrUsername { get; set; } = string.Empty;
    }
}
