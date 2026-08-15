using Node.Data.Models.Enums;

namespace Node.Web.Models.Notifications;

/// <summary>One row on the notifications page / in the navbar dropdown.</summary>
public class NotificationViewModel
{
    public int Id { get; set; }

    public string ActorDisplayName { get; set; } = string.Empty;

    /// <summary>Photo of the user who triggered the notification; null = initial-letter avatar as fallback.</summary>
    public string? ActorProfilePictureUrl { get; set; }

    public NotificationType Type { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }

    /// <summary>Only set for Type == Message: links the row to that conversation.</summary>
    public int? MatchId { get; set; }
}
