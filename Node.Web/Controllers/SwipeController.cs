using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers;

/// <summary>
/// The discover page (in the style of Tinder/Bumble): one candidate at a
/// time, rated with a like or pass. The next card is loaded via AJAX without
/// reloading the page. Members only: Admin/Moderator are staff accounts, not
/// dating profiles.
/// </summary>
[Authorize(Roles = DbSeeder.RoleMember)]
public class SwipeController : Controller
{
    private readonly ISwipeService _swipeService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SwipeController(ISwipeService swipeService, UserManager<ApplicationUser> userManager)
    {
        _swipeService = swipeService;
        _userManager = userManager;
    }

    /// <summary>The discover page with the first candidate.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var candidate = await _swipeService.GetNextCandidateAsync(_userManager.GetUserId(User)!);
        return View(candidate);
    }

    /// <summary>
    /// AJAX: the next candidate as a partial view (HTML fragment), so the
    /// stack keeps going without a page reload.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> NextCard()
    {
        var candidate = await _swipeService.GetNextCandidateAsync(_userManager.GetUserId(User)!);
        if (candidate is null)
        {
            return PartialView("_StackEmpty");
        }

        return PartialView("_SwipeCard", candidate);
    }

    /// <summary>
    /// AJAX: processes a like or pass and reports whether it resulted in a match.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rate(string targetUserId, bool isLike)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            return BadRequest();
        }

        var (isMatch, matchId) = await _swipeService.RateAsync(
            _userManager.GetUserId(User)!, targetUserId, isLike);

        return Json(new { isMatch, matchId });
    }
}
