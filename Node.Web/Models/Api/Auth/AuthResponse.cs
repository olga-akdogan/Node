namespace Node.Web.Models.Api.Auth;

/// <summary>Returned after a successful register or login: the MAUI app stores the token and sends it as a Bearer header.</summary>
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public IList<string> Roles { get; set; } = new List<string>();
}
