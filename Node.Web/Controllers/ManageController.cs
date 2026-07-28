using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Node.Data.Models;
using Node.Web.Models.Account;

namespace Node.Web.Controllers;

/// <summary>
/// Gebruikersparametrisatie: de ingelogde gebruiker beheert
/// hier de eigen profielvelden en het wachtwoord.
/// </summary>
[Authorize]
public class ManageController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<ManageController> _logger;

    public ManageController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<ManageController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var gebruiker = await _userManager.GetUserAsync(User);
        if (gebruiker is null)
        {
            return NotFound();
        }

        var model = new ManageProfileViewModel
        {
            DisplayName = gebruiker.DisplayName,
            Bio = gebruiker.Bio,
            BirthDate = gebruiker.BirthDate,
            BirthTime = gebruiker.BirthTime,
            BirthPlace = gebruiker.BirthPlace,
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ManageProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var gebruiker = await _userManager.GetUserAsync(User);
        if (gebruiker is null)
        {
            return NotFound();
        }

        // Wanneer de geboortegegevens wijzigen moet de horoscoop later opnieuw
        // berekend worden (gebeurt in de astrologie-fase).
        gebruiker.DisplayName = model.DisplayName;
        gebruiker.Bio = model.Bio;
        gebruiker.BirthDate = model.BirthDate!.Value;
        gebruiker.BirthTime = model.BirthTime!.Value;
        gebruiker.BirthPlace = model.BirthPlace;

        var resultaat = await _userManager.UpdateAsync(gebruiker);
        if (!resultaat.Succeeded)
        {
            foreach (var fout in resultaat.Errors)
            {
                ModelState.AddModelError(string.Empty, fout.Description);
            }

            return View(model);
        }

        _logger.LogInformation("Gebruiker {Email} paste het eigen profiel aan.", gebruiker.Email);
        TempData["Melding"] = "Je profiel is opgeslagen.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult ChangePassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var gebruiker = await _userManager.GetUserAsync(User);
        if (gebruiker is null)
        {
            return NotFound();
        }

        var resultaat = await _userManager.ChangePasswordAsync(
            gebruiker, model.CurrentPassword, model.NewPassword);

        if (!resultaat.Succeeded)
        {
            foreach (var fout in resultaat.Errors)
            {
                ModelState.AddModelError(string.Empty, fout.Description);
            }

            return View(model);
        }

        // Opnieuw aanmelden zodat de bestaande cookie geldig blijft na de wijziging.
        await _signInManager.RefreshSignInAsync(gebruiker);
        _logger.LogInformation("Gebruiker {Email} wijzigde het wachtwoord.", gebruiker.Email);

        TempData["Melding"] = "Je wachtwoord is gewijzigd.";
        return RedirectToAction(nameof(Index));
    }
}
