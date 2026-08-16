using Microsoft.EntityFrameworkCore;
using Node.Data.Data;
using Node.Data.Models;
using Node.Data.Models.Enums;
using Node.Web.Models.Notifications;
using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

/// <summary>
/// In-app notifications for user actions: someone liked you (stored in our
/// own Notifications table) and new chat messages (not stored here — chat
/// lives in GetStream, so unread messages are read live via IMatchService,
/// which already asks GetStream for the unread count per conversation).
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IMatchService _matchService;

    public NotificationService(ApplicationDbContext context, IMatchService matchService)
    {
        _context = context;
        _matchService = matchService;
    }

    public async Task NotifyLikeAsync(string recipientUserId, string actorUserId)
    {
        _context.Notifications.Add(new Notification
        {
            UserId = recipientUserId,
            ActorUserId = actorUserId,
            Type = NotificationType.Like,
        });

        await _context.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        var unreadLikes = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        var unreadMessages = (await _matchService.GetMatchesForUserAsync(userId)).Sum(m => m.UnreadCount);

        return unreadLikes + unreadMessages;
    }

    public async Task<List<NotificationViewModel>> GetRecentAsync(string userId, int max = 20)
    {
        var likeNotificaties = await _context.Notifications
            .Include(n => n.ActorUser)
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(max)
            .Select(n => new NotificationViewModel
            {
                Id = n.Id,
                ActorDisplayName = n.ActorUser!.DisplayName,
                ActorProfilePictureUrl = n.ActorUser.ProfilePictureUrl,
                Type = n.Type,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead,
            })
            .ToListAsync();

        // Unread messages aren't stored as Notification rows: GetStream already
        // tracks read/unread per conversation, so we ask it live via
        // IMatchService instead of duplicating that state in our own database.
        var berichtNotificaties = (await _matchService.GetMatchesForUserAsync(userId))
            .Where(m => m.UnreadCount > 0 && m.LastMessageAt.HasValue)
            .Select(m => new NotificationViewModel
            {
                Id = -m.MatchId, // Negative so it never collides with a real Notification.Id.
                ActorDisplayName = m.OtherDisplayName,
                ActorProfilePictureUrl = m.OtherProfilePictureUrl,
                Type = NotificationType.Message,
                CreatedAt = m.LastMessageAt!.Value,
                IsRead = false,
                MatchId = m.MatchId,
            });

        return likeNotificaties
            .Concat(berichtNotificaties)
            .OrderByDescending(n => n.CreatedAt)
            .Take(max)
            .ToList();
    }

    public Task MarkAllReadAsync(string userId) =>
        _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));
}
