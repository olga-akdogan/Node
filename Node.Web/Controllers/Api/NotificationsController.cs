using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Models.Notifications;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers.Api;

/// <summary>
/// API equivalent of <see cref="Node.Web.Controllers.NotificationController"/> for the
/// MAUI app. Members only, same as the web page.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = DbSeeder.RolLid)]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(INotificationService notificationService, UserManager<ApplicationUser> userManager)
    {
        _notificationService = notificationService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationViewModel>>> GetRecent()
    {
        var notifications = await _notificationService.GetRecentAsync(_userManager.GetUserId(User)!);
        return Ok(notifications);
    }

    /// <summary>Badge count for the app's notification icon.</summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var count = await _notificationService.GetUnreadCountAsync(_userManager.GetUserId(User)!);
        return Ok(count);
    }

    /// <summary>Called when the app opens the notifications screen, same as the web page does on load.</summary>
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notificationService.MarkAllReadAsync(_userManager.GetUserId(User)!);
        return NoContent();
    }
}
