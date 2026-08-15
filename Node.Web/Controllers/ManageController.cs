using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Node.Data.Data;
using Node.Data.Models;
using Node.Data.Models.Enums;
using Node.Data.Services;
using Node.Web.Models.Account;
using Node.Web.Resources;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers;

/// <summary>
/// Gebruikersparametrisatie: de ingelogde gebruiker beheert
/// hier de eigen profielvelden, profielfoto en het wachtwoord.
/// </summary>
[Authorize]
public class ManageController : Controller
{
    /// <summary>Toegestane afbeeldingstypes voor de profielfoto.</summary>
    private static readonly Dictionary<string, string> ToegestaneAfbeeldingTypes = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    private const long MaxFotoGrootteBytes = 5 * 1024 * 1024; // 5 MB

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IGeocodingService _geocodingService;
    private readonly INatalChartCalculator _natalChartCalculator;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _omgeving;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<ManageController> _logger;

    public ManageController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IGeocodingService geocodingService,
        INatalChartCalculator natalChartCalculator,
        ApplicationDbContext context,
        IWebHostEnvironment omgeving,
        IStringLocalizer<SharedResource> localizer,
        ILogger<ManageController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _geocodingService = geocodingService;
        _natalChartCalculator = natalChartCalculator;
        _context = context;
        _omgeving = omgeving;
        _localizer = localizer;
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

        var preferences = await _context.PartnerPreferences
            .Where(p => p.UserId == gebruiker.Id)
            .Select(p => p.Gender)
            .ToListAsync();

        var model = new ManageProfileViewModel
        {
            DisplayName = gebruiker.DisplayName,
            Bio = gebruiker.Bio,
            BirthDate = gebruiker.BirthDate,
            BirthTime = gebruiker.BirthTime,
            BirthPlace = gebruiker.BirthPlace,
            HuidigeProfielFotoUrl = gebruiker.ProfilePictureUrl,
            Gender = gebruiker.Gender,
            LooksForMen = preferences.Contains(Gender.Male),
            LooksForWomen = preferences.Contains(Gender.Female),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ManageProfileViewModel model)
    {
        if (model.ProfilePicture is not null && !ToegestaneAfbeeldingTypes.ContainsKey(model.ProfilePicture.ContentType))
        {
            ModelState.AddModelError(nameof(model.ProfilePicture), _localizer["Fout_AfbeeldingType"]);
        }
        else if (model.ProfilePicture is not null && model.ProfilePicture.Length > MaxFotoGrootteBytes)
        {
            ModelState.AddModelError(nameof(model.ProfilePicture), _localizer["Fout_AfbeeldingGrootte"]);
        }

        if (!model.LooksForMen && !model.LooksForWomen)
        {
            ModelState.AddModelError(string.Empty, _localizer["Valid_KiesMinstensEenVoorkeur"]);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var gebruiker = await _userManager.GetUserAsync(User);
        if (gebruiker is null)
        {
            return NotFound();
        }

        model.HuidigeProfielFotoUrl = gebruiker.ProfilePictureUrl;

        // De horoscoop hangt af van geboortedatum, -tijd én -plaats: enkel
        // opnieuw berekenen (en eventueel opnieuw geocoderen) wanneer één van
        // die drie effectief wijzigt, niet bij elke profielwijziging.
        var plaatsGewijzigd = !string.Equals(gebruiker.BirthPlace, model.BirthPlace, StringComparison.Ordinal);
        var geboortegegevensGewijzigd =
            gebruiker.BirthDate != model.BirthDate!.Value ||
            gebruiker.BirthTime != model.BirthTime!.Value ||
            plaatsGewijzigd;

        if (plaatsGewijzigd)
        {
            var coordinaten = await _geocodingService.ZoekCoordinatenAsync(model.BirthPlace);
            if (coordinaten is null)
            {
                ModelState.AddModelError(nameof(model.BirthPlace), _localizer["Fout_GeboorteplaatsNietGevonden"]);
                return View(model);
            }

            gebruiker.BirthLatitude = coordinaten.Value.Latitude;
            gebruiker.BirthLongitude = coordinaten.Value.Longitude;
        }

        gebruiker.DisplayName = model.DisplayName;
        gebruiker.Bio = model.Bio;
        gebruiker.BirthDate = model.BirthDate!.Value;
        gebruiker.BirthTime = model.BirthTime!.Value;
        gebruiker.BirthPlace = model.BirthPlace;
        gebruiker.Gender = model.Gender!.Value;

        if (model.ProfilePicture is not null)
        {
            gebruiker.ProfilePictureUrl = await BewaarProfielFotoAsync(gebruiker.Id, model.ProfilePicture);
            model.HuidigeProfielFotoUrl = gebruiker.ProfilePictureUrl;
        }

        var resultaat = await _userManager.UpdateAsync(gebruiker);
        if (!resultaat.Succeeded)
        {
            foreach (var fout in resultaat.Errors)
            {
                ModelState.AddModelError(string.Empty, fout.Description);
            }

            return View(model);
        }

        await ReplacePartnerPreferencesAsync(gebruiker.Id, model.LooksForMen, model.LooksForWomen);

        if (geboortegegevensGewijzigd)
        {
            var bestaandeHoroscoop = await _context.NatalCharts.FirstOrDefaultAsync(n => n.UserId == gebruiker.Id);
            if (bestaandeHoroscoop is not null)
            {
                _context.NatalCharts.Remove(bestaandeHoroscoop);
            }

            var nieuweHoroscoop = _natalChartCalculator.Calculate(gebruiker);
            _context.NatalCharts.Add(nieuweHoroscoop);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Horoscoop van {Email} opnieuw berekend na wijziging van de geboortegegevens.", gebruiker.Email);
        }

        _logger.LogInformation("Gebruiker {Email} paste het eigen profiel aan.", gebruiker.Email);
        TempData["Melding"] = _localizer["Melding_ProfielOpgeslagen"].Value;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Removes the existing partner preferences and sets them again based on the checked genders.</summary>
    private async Task ReplacePartnerPreferencesAsync(string userId, bool looksForMen, bool looksForWomen)
    {
        var existing = await _context.PartnerPreferences.Where(p => p.UserId == userId).ToListAsync();
        _context.PartnerPreferences.RemoveRange(existing);

        if (looksForMen)
        {
            _context.PartnerPreferences.Add(new PartnerPreference { UserId = userId, Gender = Gender.Male });
        }

        if (looksForWomen)
        {
            _context.PartnerPreferences.Add(new PartnerPreference { UserId = userId, Gender = Gender.Female });
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Slaat de geüploade foto op in wwwroot/uploads/profiels, met de
    /// gebruikers-id als bestandsnaam zodat een nieuwe upload de vorige foto
    /// gewoon vervangt in plaats van te stapelen.
    /// </summary>
    private async Task<string> BewaarProfielFotoAsync(string userId, IFormFile foto)
    {
        var extensie = ToegestaneAfbeeldingTypes[foto.ContentType];
        var mapPad = Path.Combine(_omgeving.WebRootPath, "uploads", "profiles");
        Directory.CreateDirectory(mapPad);

        var bestandsPad = Path.Combine(mapPad, $"{userId}{extensie}");
        await using (var stream = new FileStream(bestandsPad, FileMode.Create))
        {
            await foto.CopyToAsync(stream);
        }

        return $"/uploads/profiles/{userId}{extensie}";
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

        TempData["Melding"] = _localizer["Melding_WachtwoordGewijzigd"].Value;
        return RedirectToAction(nameof(Index));
    }
}
