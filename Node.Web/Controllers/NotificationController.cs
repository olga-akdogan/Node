using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Node.Data.Models;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers;

/// <summary>The logged-in user's notifications (currently: "X liked you").</summary>
[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationController(INotificationService notificationService, UserManager<ApplicationUser> userManager)
    {
        _notificationService = notificationService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var notifications = await _notificationService.GetRecentAsync(userId);

        // Viewing the page counts as reading: clear the unread badge.
        await _notificationService.MarkAllReadAsync(userId);

        return View(notifications);
    }
}
