using Node.Data.Models;

namespace Node.Web.Services.Interfaces;

/// <summary>Issues signed JWT access tokens for the REST API (used by the MAUI companion app).</summary>
public interface IJwtTokenService
{
    /// <summary>Creates a signed token for the user, embedding their id, email and current roles.</summary>
    Task<(string Token, DateTime ExpiresAtUtc)> CreateTokenAsync(ApplicationUser user);
}
