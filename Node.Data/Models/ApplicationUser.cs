using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Node.Data.Models;

/// <summary>
/// Eigen Identity-gebruiker met extra eigenschappen
/// De geboortegegevens staan rechtstreeks op de gebruiker omdat ze bij registratie
/// verplicht ingevuld worden en de basis vormen voor de geboortehoroscoop.
/// [PersonalData] markeert velden die bij een GDPR-export/verwijdering meegaan.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Weergavenaam in de app (niet uniek, los van de login).</summary>
    [Required]
    [MaxLength(80)]
    [PersonalData]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Korte zelfbeschrijving op het profiel.</summary>
    [MaxLength(1000)]
    [PersonalData]
    public string? Bio { get; set; }

    /// <summary>Pad naar de profielfoto (in wwwroot of externe opslag).</summary>
    [MaxLength(400)]
    public string? ProfilePictureUrl { get; set; }

    /// <summary>Geboortedatum (lokale datum op de geboorteplaats).</summary>
    [Required]
    [PersonalData]
    public DateOnly BirthDate { get; set; }

    /// <summary>Geboortetijd (lokale kloktijd op de geboorteplaats), nodig voor de Ascendant.</summary>
    [Required]
    [PersonalData]
    public TimeOnly BirthTime { get; set; }

    /// <summary>Geboorteplaats zoals de gebruiker ze intypte (bv. "Antwerpen, België").</summary>
    [Required]
    [MaxLength(150)]
    [PersonalData]
    public string BirthPlace { get; set; } = string.Empty;

    /// <summary>Breedtegraad van de geboorteplaats (via geocoding ingevuld).</summary>
    [Column(TypeName = "decimal(9,6)")]
    [PersonalData]
    public decimal? BirthLatitude { get; set; }

    /// <summary>Lengtegraad van de geboorteplaats (via geocoding ingevuld).</summary>
    [Column(TypeName = "decimal(9,6)")]
    [PersonalData]
    public decimal? BirthLongitude { get; set; }

    /// <summary>Door een beheerder geblokkeerd: inloggen wordt geweigerd.</summary>
    public bool IsBlocked { get; set; }

    /// <summary>Tijdstip waarop het account werd aangemaakt (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>De berekende geboortehoroscoop van deze gebruiker (1-op-1).</summary>
    public NatalChart? NatalChart { get; set; }
}
