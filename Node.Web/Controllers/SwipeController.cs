using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers;

/// <summary>
/// De ontdek-pagina (naar het voorbeeld van Tinder/Bumble): één kandidaat
/// tegelijk, beoordelen met like of pass. De volgende kaart wordt via AJAX
/// geladen zonder dat de pagina herlaadt. Enkel voor leden: Admin/Moderator
/// zijn beheeraccounts, geen datingprofielen.
/// </summary>
[Authorize(Roles = DbSeeder.RolLid)]
public class SwipeController : Controller
{
    private readonly ISwipeService _swipeService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SwipeController(ISwipeService swipeService, UserManager<ApplicationUser> userManager)
    {
        _swipeService = swipeService;
        _userManager = userManager;
    }

    /// <summary>De ontdek-pagina met de eerste kandidaat.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var kandidaat = await _swipeService.GetVolgendeKandidaatAsync(_userManager.GetUserId(User)!);
        return View(kandidaat);
    }

    /// <summary>
    /// AJAX: de volgende kandidaat als partial view (HTML-fragment), zodat de
    /// stapel doorloopt zonder paginaherlaad.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> VolgendeKaart()
    {
        var kandidaat = await _swipeService.GetVolgendeKandidaatAsync(_userManager.GetUserId(User)!);
        if (kandidaat is null)
        {
            return PartialView("_StapelLeeg");
        }

        return PartialView("_SwipeKaart", kandidaat);
    }

    /// <summary>
    /// AJAX: verwerkt een like of pass en meldt of er een match ontstond.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Beoordeel(string targetUserId, bool isLike)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            return BadRequest();
        }

        var (isMatch, matchId) = await _swipeService.BeoordeelAsync(
            _userManager.GetUserId(User)!, targetUserId, isLike);

        return Json(new { isMatch, matchId });
    }
}
