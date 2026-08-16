using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
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
/// Aangepaste registratie- en loginflow met de extra
/// profielvelden, automatische roltoekenning en verplichte e-mailverificatie.
/// </summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _emailService;
    private readonly IGeocodingService _geocodingService;
    private readonly INatalChartCalculator _natalChartCalculator;
    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailService emailService,
        IGeocodingService geocodingService,
        INatalChartCalculator natalChartCalculator,
        ApplicationDbContext context,
        IStringLocalizer<SharedResource> localizer,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _geocodingService = geocodingService;
        _natalChartCalculator = natalChartCalculator;
        _context = context;
        _localizer = localizer;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register() => View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!model.LooksForMen && !model.LooksForWomen)
        {
            ModelState.AddModelError(string.Empty, _localizer["Valid_KiesMinstensEenVoorkeur"]);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Coördinaten van de geboorteplaats zijn nodig voor zowel de historische
        // tijdzone-omrekening als de Ascendant/huizenberekening.
        var coordinaten = await _geocodingService.ZoekCoordinatenAsync(model.BirthPlace);
        if (coordinaten is null)
        {
            ModelState.AddModelError(nameof(model.BirthPlace), _localizer["Fout_GeboorteplaatsNietGevonden"]);
            return View(model);
        }

        var gebruiker = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName,
            Bio = model.Bio,
            // De velden zijn [Required] in het viewmodel, dus hier nooit null.
            BirthDate = model.BirthDate!.Value,
            BirthTime = model.BirthTime!.Value,
            BirthPlace = model.BirthPlace,
            BirthLatitude = coordinaten.Value.Latitude,
            BirthLongitude = coordinaten.Value.Longitude,
            Gender = model.Gender!.Value,
        };

        var resultaat = await _userManager.CreateAsync(gebruiker, model.Password);
        if (!resultaat.Succeeded)
        {
            // De Identity-foutteksten worden in de meertaligheidsfase vertaald.
            foreach (var fout in resultaat.Errors)
            {
                ModelState.AddModelError(string.Empty, fout.Description);
            }

            return View(model);
        }

        // Elke nieuwe gebruiker krijgt automatisch de rol "Lid".
        await _userManager.AddToRoleAsync(gebruiker, DbSeeder.RolLid);
        _logger.LogInformation("Nieuwe gebruiker geregistreerd: {Email}.", model.Email);

        AddPartnerPreferences(gebruiker.Id, model.LooksForMen, model.LooksForWomen);

        var horoscoop = _natalChartCalculator.Calculate(gebruiker);
        _context.NatalCharts.Add(horoscoop);
        await _context.SaveChangesAsync();

        await VerstuurVerificatieEmailAsync(gebruiker);

        return RedirectToAction(nameof(RegisterConfirmation));
    }

    /// <summary>Informatiepagina na registratie: "bevestig je e-mailadres".</summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult RegisterConfirmation() => View();

    /// <summary>Verwerkt de bevestigingslink uit de verificatie-e-mail.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string? userId, string? code)
    {
        if (userId is null || code is null)
        {
            return RedirectToAction("Index", "Home");
        }

        var gebruiker = await _userManager.FindByIdAsync(userId);
        if (gebruiker is null)
        {
            return NotFound();
        }

        // De token werd URL-veilig gecodeerd bij het versturen; hier terug decoderen.
        var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var resultaat = await _userManager.ConfirmEmailAsync(gebruiker, token);

        ViewData["Gelukt"] = resultaat.Succeeded;
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var gebruiker = await _userManager.FindByEmailAsync(model.Email);

        // Geblokkeerde gebruikers mogen niet meer inloggen.
        if (gebruiker is not null && gebruiker.IsBlocked)
        {
            _logger.LogWarning("Geblokkeerde gebruiker probeerde in te loggen: {Email}.", model.Email);
            ModelState.AddModelError(string.Empty, _localizer["Fout_AccountGeblokkeerd"]);
            return View(model);
        }

        var resultaat = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (resultaat.Succeeded)
        {
            _logger.LogInformation("Gebruiker ingelogd: {Email}.", model.Email);
            return LocalRedirect(returnUrl ?? Url.Action("Index", "Home")!);
        }

        if (resultaat.IsNotAllowed)
        {
            // E-mail nog niet bevestigd: inloggen is nog niet toegestaan.
            ModelState.AddModelError(string.Empty, _localizer["Fout_EmailNietBevestigd"]);
            return View(model);
        }

        if (resultaat.IsLockedOut)
        {
            _logger.LogWarning("Account tijdelijk vergrendeld na mislukte pogingen: {Email}.", model.Email);
            ModelState.AddModelError(string.Empty, _localizer["Fout_TeVeelPogingen"]);
            return View(model);
        }

        ModelState.AddModelError(string.Empty, _localizer["Fout_OngeldigeInloggegevens"]);
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    /// <summary>Getoond wanneer een gebruiker een pagina zonder rechten opvraagt.</summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    /// <summary>Adds PartnerPreference rows for the checked genders (context.SaveChangesAsync is called by the caller).</summary>
    private void AddPartnerPreferences(string userId, bool looksForMen, bool looksForWomen)
    {
        if (looksForMen)
        {
            _context.PartnerPreferences.Add(new PartnerPreference { UserId = userId, Gender = Gender.Male });
        }

        if (looksForWomen)
        {
            _context.PartnerPreferences.Add(new PartnerPreference { UserId = userId, Gender = Gender.Female });
        }
    }

    /// <summary>
    /// Genereert de bevestigingstoken, bouwt de absolute link en verstuurt de
    /// verificatie-e-mail. Een tijdelijke SMTP-storing mag de registratie zelf
    /// (het account bestaat dan al) niet laten crashen: de fout wordt gelogd
    /// in plaats van door te bubbelen, zoals ook bij de Claude-oproepen gebeurt.
    /// </summary>
    private async Task VerstuurVerificatieEmailAsync(ApplicationUser gebruiker)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(gebruiker);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = Url.Action(nameof(ConfirmEmail), "Account",
            new { userId = gebruiker.Id, code }, protocol: Request.Scheme)!;

        try
        {
            await _emailService.SendAsync(
                gebruiker.Email!,
                "Bevestig je e-mailadres bij Node",
                $"<p>Welkom bij Node!</p><p><a href=\"{link}\">Klik hier om je e-mailadres te bevestigen</a>.</p>");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Versturen van de verificatie-e-mail naar {Email} is mislukt.", gebruiker.Email);
        }
    }
}
