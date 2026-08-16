using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Node.Data.Models;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers;

/// <summary>
/// Match overview and chat. The chat screen connects directly to GetStream
/// Chat in the browser; this controller only supplies the match data and the
/// GetStream token needed to set up that connection.
/// </summary>
[Authorize]
public class MatchController : Controller
{
    private readonly IMatchService _matchService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MatchController(IMatchService matchService, UserManager<ApplicationUser> userManager)
    {
        _matchService = matchService;
        _userManager = userManager;
    }

    /// <summary>Overzicht van alle actieve matches van de ingelogde gebruiker.</summary>
    /// <param name="sortering">"recent" (standaard, meest recente gesprek eerst) of "score" (hoogste compatibiliteit eerst).</param>
    [HttpGet]
    public async Task<IActionResult> Index(string sortering = "recent")
    {
        var matches = await _matchService.GetMatchesVoorGebruikerAsync(_userManager.GetUserId(User)!);

        if (sortering == "score")
        {
            matches = matches.OrderByDescending(m => m.CompatibilityScore).ToList();
        }
        // "recent" is al de volgorde die de service teruggeeft: niets te doen.

        ViewData["Sortering"] = sortering;
        return View(matches);
    }

    /// <summary>Het chatgesprek van één match.</summary>
    [HttpGet]
    public async Task<IActionResult> Chat(int id)
    {
        var chat = await _matchService.GetChatAsync(id, _userManager.GetUserId(User)!);
        if (chat is null)
        {
            // Bestaat niet of de gebruiker is geen deelnemer: niets prijsgeven.
            return NotFound();
        }

        return View(chat);
    }
}
