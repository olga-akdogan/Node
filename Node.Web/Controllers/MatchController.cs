using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers;

/// <summary>
/// Match overview and chat. The chat screen connects directly to GetStream
/// Chat in the browser; this controller only supplies the match data and the
/// GetStream token needed to set up that connection. Members only: Admin and
/// Moderator are staff accounts, not dating profiles.
/// </summary>
[Authorize(Roles = DbSeeder.RoleMember)]
public class MatchController : Controller
{
    private readonly IMatchService _matchService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MatchController(IMatchService matchService, UserManager<ApplicationUser> userManager)
    {
        _matchService = matchService;
        _userManager = userManager;
    }

    /// <summary>Overview of all active matches of the logged-in user.</summary>
    /// <param name="sort">"recent" (default, most recent conversation first) or "score" (highest compatibility first).</param>
    [HttpGet]
    public async Task<IActionResult> Index(string sort = "recent")
    {
        var matches = await _matchService.GetMatchesForUserAsync(_userManager.GetUserId(User)!);

        if (sort == "score")
        {
            matches = matches.OrderByDescending(m => m.CompatibilityScore).ToList();
        }
        // "recent" is already the order the service returns: nothing to do.

        ViewData["Sort"] = sort;
        return View(matches);
    }

    /// <summary>The chat conversation of one match.</summary>
    [HttpGet]
    public async Task<IActionResult> Chat(int id)
    {
        var chat = await _matchService.GetChatAsync(id, _userManager.GetUserId(User)!);
        if (chat is null)
        {
            // Doesn't exist or the user isn't a participant: reveal nothing.
            return NotFound();
        }

        return View(chat);
    }
}
