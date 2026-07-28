using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Models.Account;
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
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailService emailService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
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
        if (!ModelState.IsValid)
        {
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
            ModelState.AddModelError(string.Empty, "Dit account is geblokkeerd door een beheerder.");
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
            ModelState.AddModelError(string.Empty, "Bevestig eerst je e-mailadres via de link in je mailbox.");
            return View(model);
        }

        if (resultaat.IsLockedOut)
        {
            _logger.LogWarning("Account tijdelijk vergrendeld na mislukte pogingen: {Email}.", model.Email);
            ModelState.AddModelError(string.Empty, "Te veel mislukte pogingen. Probeer het later opnieuw.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Ongeldige inloggegevens.");
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

    /// <summary>
    /// Genereert de bevestigingstoken, bouwt de absolute link en verstuurt de
    /// verificatie-e-mail.
    /// </summary>
    private async Task VerstuurVerificatieEmailAsync(ApplicationUser gebruiker)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(gebruiker);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = Url.Action(nameof(ConfirmEmail), "Account",
            new { userId = gebruiker.Id, code }, protocol: Request.Scheme)!;

        await _emailService.SendAsync(
            gebruiker.Email!,
            "Bevestig je e-mailadres bij Node",
            $"<p>Welkom bij Node!</p><p><a href=\"{link}\">Klik hier om je e-mailadres te bevestigen</a>.</p>");
    }
}
