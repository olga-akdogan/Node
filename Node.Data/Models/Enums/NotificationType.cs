namespace Node.Data.Models.Enums;

/// <summary>
/// Kind of event a Notification represents. Message isn't stored in the
/// Notifications table (chat lives in GetStream, not our database) — it's
/// only used as a tag on the notification view model built live from
/// IMatchService's unread counts. See NotificationService.GetRecentAsync.
/// </summary>
public enum NotificationType
{
    Like,
    Message
}
