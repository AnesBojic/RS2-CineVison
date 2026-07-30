namespace eCommerce.Model.Requests;

/// <summary>
/// Public self-registration payload. Role / privilege fields are intentionally absent —
/// new accounts are always created as Customer.
/// </summary>
public class UserRegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfileImageBase64 { get; set; }
}
