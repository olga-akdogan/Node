using Node.Web.Models.Notifications;

namespace Node.Web.Services.Interfaces;

/// <summary>In-app notifications for user actions (currently: someone liked you).</summary>
public interface INotificationService
{
    /// <summary>Creates a "liked you" notification for the recipient. Called on every like, match or not.</summary>
    Task NotifyLikeAsync(string recipientUserId, string actorUserId);

    /// <summary>Number of unread notifications, shown as the badge count on the navbar bell.</summary>
    Task<int> GetUnreadCountAsync(string userId);

    /// <summary>Most recent notifications for the notifications page, newest first.</summary>
    Task<List<NotificationViewModel>> GetRecentAsync(string userId, int max = 20);

    /// <summary>Marks all of the user's notifications as read (called when the notifications page is opened).</summary>
    Task MarkAllReadAsync(string userId);
}
