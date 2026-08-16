using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Node.Data.Data;
using Node.Data.Models;
using Node.Data.Models.Enums;
using Node.Web.Models.Notifications;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers;

/// <summary>The logged-in user's notifications (currently: "X liked you" and unread messages).</summary>
[Authorize(Roles = DbSeeder.RolLid)]
public class NotificationController : Controller
{
    /// <summary>Fetched from the service before filtering, so a type filter still has a full pool to pick from.</summary>
    private const int OpgehaaldAantal = 50;

    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationController(INotificationService notificationService, UserManager<ApplicationUser> userManager)
    {
        _notificationService = notificationService;
        _userManager = userManager;
    }

    /// <summary>Overzicht van de meldingen, met filter op type en sortering op datum.</summary>
    /// <param name="type">Null/leeg = alle types; anders "Like" of "Message".</param>
    /// <param name="sortering">"nieuw" (standaard) of "oud".</param>
    [HttpGet]
    public async Task<IActionResult> Index(string? type, string sortering = "nieuw")
    {
        var userId = _userManager.GetUserId(User)!;
        var notifications = await _notificationService.GetRecentAsync(userId, OpgehaaldAantal);

        // Viewing the page counts as reading: clear the unread badge.
        await _notificationService.MarkAllReadAsync(userId);

        IEnumerable<NotificationViewModel> gefilterd = notifications;
        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<NotificationType>(type, out var typeWaarde))
        {
            gefilterd = gefilterd.Where(n => n.Type == typeWaarde);
        }

        gefilterd = sortering == "oud"
            ? gefilterd.OrderBy(n => n.CreatedAt)
            : gefilterd.OrderByDescending(n => n.CreatedAt);

        ViewData["Type"] = type;
        ViewData["Sortering"] = sortering;
        return View(gefilterd.ToList());
    }
}
