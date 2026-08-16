using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Node.Data.Data;
using Node.Data.Models;
using Node.Data.Models.Enums;
using Node.Web.Models.Notifications;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers;

/// <summary>The logged-in user's notifications </summary>
[Authorize(Roles = DbSeeder.RoleMember)]
public class NotificationController : Controller
{
    /// <summary>Fetched from the service before filtering, so a type filter still has a full pool to pick from.</summary>
    private const int FetchCount = 50;

    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationController(INotificationService notificationService, UserManager<ApplicationUser> userManager)
    {
        _notificationService = notificationService;
        _userManager = userManager;
    }

    /// <summary>Overview of the notifications, with a type filter and date sort.</summary>
    /// <param name="type">Null/empty = all types; otherwise "Like" or "Message".</param>
    /// <param name="sort">"new" (default) or "old".</param>
    [HttpGet]
    public async Task<IActionResult> Index(string? type, string sort = "new")
    {
        var userId = _userManager.GetUserId(User)!;
        var notifications = await _notificationService.GetRecentAsync(userId, FetchCount);

        // Viewing the page counts as reading: clear the unread badge.
        await _notificationService.MarkAllReadAsync(userId);

        IEnumerable<NotificationViewModel> filtered = notifications;
        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<NotificationType>(type, out var parsedType))
        {
            filtered = filtered.Where(n => n.Type == parsedType);
        }

        filtered = sort == "old"
            ? filtered.OrderBy(n => n.CreatedAt)
            : filtered.OrderByDescending(n => n.CreatedAt);

        ViewData["Type"] = type;
        ViewData["Sort"] = sort;
        return View(filtered.ToList());
    }
}
