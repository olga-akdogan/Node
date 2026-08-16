using System.ComponentModel.DataAnnotations;

namespace Node.Data.Models;

/// <summary>
/// Record of a cookie consent choice. Written by the custom cookie
/// middleware so it's provable when and from where a visitor gave or
/// refused consent.
/// </summary>
public class CookieConsentLog
{
    public int Id { get; set; }

    /// <summary>The logged-in user, or null for an anonymous visitor.</summary>
    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    /// <summary>True = consent given, false = refused.</summary>
    public bool HasAcceptedCookies { get; set; }

    /// <summary>IP address of the visitor at the moment of the choice.</summary>
    [MaxLength(64)]
    public string? IpAddress { get; set; }

    /// <summary>Browser information of the visitor.</summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
