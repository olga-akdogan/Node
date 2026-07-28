using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Node.Data.Models;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers;

/// <summary>
/// Matchoverzicht en chat. Berichten worden via AJAX verstuurd en als
/// HTML-fragment teruggegeven.
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
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var matches = await _matchService.GetMatchesVoorGebruikerAsync(_userManager.GetUserId(User)!);
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

    /// <summary>
    /// AJAX: verstuurt een bericht en geeft de nieuwe tekstballon terug als
    /// HTML-fragment dat de pagina onderaan het gesprek plakt.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StuurBericht(int matchId, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 1000)
        {
            return BadRequest();
        }

        var bericht = await _matchService.StuurBerichtAsync(
            matchId, _userManager.GetUserId(User)!, content.Trim());

        if (bericht is null)
        {
            return NotFound();
        }

        return PartialView("_ChatBericht", bericht);
    }
}
