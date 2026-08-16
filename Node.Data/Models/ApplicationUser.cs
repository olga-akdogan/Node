using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Node.Data.Models.Enums;

namespace Node.Data.Models;

/// <summary>
/// Custom Identity user with extra properties.
/// The birth data lives directly on the user because it's required at
/// registration and forms the basis for the natal chart.
/// [PersonalData] marks fields that are included in a GDPR export/deletion.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Display name in the app (not unique, separate from the login).</summary>
    [Required]
    [MaxLength(80)]
    [PersonalData]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Short self-description on the profile.</summary>
    [MaxLength(1000)]
    [PersonalData]
    public string? Bio { get; set; }

    /// <summary>Path to the profile picture (in wwwroot or external storage).</summary>
    [MaxLength(400)]
    public string? ProfilePictureUrl { get; set; }

    /// <summary>Birth date (local date at the birthplace).</summary>
    [Required]
    [PersonalData]
    public DateOnly BirthDate { get; set; }

    /// <summary>Birth time (local clock time at the birthplace), needed for the Ascendant.</summary>
    [Required]
    [PersonalData]
    public TimeOnly BirthTime { get; set; }

    /// <summary>
    /// True when the exact birth time is unknown and BirthTime is therefore a
    /// conventional placeholder (12:00). Sun, Moon and planet signs remain
    /// reliable; the Ascendant and houses are then marked as uncertain.
    /// </summary>
    public bool BirthTimeIsUnknown { get; set; }

    /// <summary>Birthplace as the user typed it (e.g. "Antwerp, Belgium").</summary>
    [Required]
    [MaxLength(150)]
    [PersonalData]
    public string BirthPlace { get; set; } = string.Empty;

    /// <summary>Latitude of the birthplace (filled in via geocoding).</summary>
    [Column(TypeName = "decimal(9,6)")]
    [PersonalData]
    public decimal? BirthLatitude { get; set; }

    /// <summary>Longitude of the birthplace (filled in via geocoding).</summary>
    [Column(TypeName = "decimal(9,6)")]
    [PersonalData]
    public decimal? BirthLongitude { get; set; }

    /// <summary>Own gender; together with <see cref="PartnerPreferences"/> determines who appears in the swipe deck.</summary>
    [Required]
    [PersonalData]
    public Gender Gender { get; set; }

    /// <summary>Gender(s) this user is interested in (at least one).</summary>
    public ICollection<PartnerPreference> PartnerPreferences { get; set; } = new List<PartnerPreference>();

    /// <summary>Blocked by an admin: login is refused.</summary>
    public bool IsBlocked { get; set; }

    /// <summary>Timestamp when the account was created (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>This user's calculated natal chart (1-to-1).</summary>
    public NatalChart? NatalChart { get; set; }
}
