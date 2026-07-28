using System.ComponentModel.DataAnnotations;

namespace Node.Data.Models;

/// <summary>
/// Eén beoordeling van een gebruiker over een andere gebruiker (like of pass).
/// Wanneer twee gebruikers elkaar liken ontstaat er een <see cref="Match"/>.
/// Eén swipe per combinatie swiper/doelwit (unieke index in de DbContext).
/// </summary>
public class Swipe
{
    public int Id { get; set; }

    /// <summary>De gebruiker die swipet.</summary>
    [Required]
    public string SwiperUserId { get; set; } = string.Empty;

    public ApplicationUser? SwiperUser { get; set; }

    /// <summary>De gebruiker die beoordeeld wordt.</summary>
    [Required]
    public string TargetUserId { get; set; } = string.Empty;

    public ApplicationUser? TargetUser { get; set; }

    /// <summary>True = like, false = pass.</summary>
    public bool IsLike { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
