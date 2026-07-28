using System.ComponentModel.DataAnnotations;

namespace Node.Data.Models;

/// <summary>
/// Registratie van een cookietoestemming. Wordt weggeschreven door de eigen
/// cookie-middleware zodat aantoonbaar is
/// wanneer en vanwaar een bezoeker toestemming gaf of weigerde.
/// </summary>
public class CookieConsentLog
{
    public int Id { get; set; }

    /// <summary>De ingelogde gebruiker, of null voor een anonieme bezoeker.</summary>
    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    /// <summary>True = toestemming gegeven, false = geweigerd.</summary>
    public bool HasAcceptedCookies { get; set; }

    /// <summary>IP-adres van de bezoeker op het moment van de keuze.</summary>
    [MaxLength(64)]
    public string? IpAddress { get; set; }

    /// <summary>Browserinformatie van de bezoeker.</summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
