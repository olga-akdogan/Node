using System.ComponentModel.DataAnnotations;

namespace Node.Data.Models;

/// <summary>
/// One user's rating of another user (like or pass).
/// When two users like each other, a <see cref="Match"/> is created.
/// One swipe per swiper/target combination (unique index in the DbContext).
/// </summary>
public class Swipe
{
    public int Id { get; set; }

    /// <summary>The user doing the swiping.</summary>
    [Required]
    public string SwiperUserId { get; set; } = string.Empty;

    public ApplicationUser? SwiperUser { get; set; }

    /// <summary>The user being rated.</summary>
    [Required]
    public string TargetUserId { get; set; } = string.Empty;

    public ApplicationUser? TargetUser { get; set; }

    /// <summary>True = like, false = pass.</summary>
    public bool IsLike { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
