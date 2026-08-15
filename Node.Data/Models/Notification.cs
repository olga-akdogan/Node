using System.ComponentModel.DataAnnotations;
using Node.Data.Models.Enums;

namespace Node.Data.Models;

/// <summary>
/// An in-app notification for one user, triggered by another user's action
/// (currently only a like; extensible via NotificationType).
/// </summary>
public class Notification
{
    public int Id { get; set; }

    /// <summary>The user who receives this notification.</summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    /// <summary>The user whose action triggered this notification.</summary>
    [Required]
    public string ActorUserId { get; set; } = string.Empty;

    public ApplicationUser? ActorUser { get; set; }

    [Required]
    public NotificationType Type { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
