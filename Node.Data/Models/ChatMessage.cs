using System.ComponentModel.DataAnnotations;

namespace Node.Data.Models;

/// <summary>
/// Eén chatbericht binnen een match. De afzender is altijd één van de twee
/// gebruikers van de match (wordt in de servicelaag afgedwongen).
/// </summary>
public class ChatMessage
{
    public int Id { get; set; }

    /// <summary>De match (het gesprek) waarin dit bericht verstuurd werd.</summary>
    public int MatchId { get; set; }

    public Match? Match { get; set; }

    /// <summary>De gebruiker die het bericht verstuurde.</summary>
    [Required]
    public string SenderUserId { get; set; } = string.Empty;

    public ApplicationUser? SenderUser { get; set; }

    /// <summary>De inhoud van het bericht.</summary>
    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>True zodra de ontvanger het bericht gelezen heeft.</summary>
    public bool IsRead { get; set; }
}
