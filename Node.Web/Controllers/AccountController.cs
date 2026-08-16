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
/// Customized registration and login flow with the extra
/// profile fields, automatic role assignment and required email verification.
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
            ModelState.AddModelError(string.Empty, _localizer["Valid_ChooseAtLeastOnePreference"]);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Coordinates of the birth place are needed both for the historical
        // timezone conversion and for the Ascendant/house calculation.
        var coordinates = await _geocodingService.FindCoordinatesAsync(model.BirthPlace);
        if (coordinates is null)
        {
            ModelState.AddModelError(nameof(model.BirthPlace), _localizer["Error_BirthPlaceNotFound"]);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName,
            Bio = model.Bio,
            // The fields are [Required] on the view model, so never null here.
            BirthDate = model.BirthDate!.Value,
            BirthTime = model.BirthTime!.Value,
            BirthPlace = model.BirthPlace,
            BirthLatitude = coordinates.Value.Latitude,
            BirthLongitude = coordinates.Value.Longitude,
            Gender = model.Gender!.Value,
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            // The Identity error texts are translated in the localization pipeline.
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // Every new user automatically gets the "Lid" role.
        await _userManager.AddToRoleAsync(user, DbSeeder.RoleMember);
        _logger.LogInformation("New user registered: {Email}.", model.Email);

        AddPartnerPreferences(user.Id, model.LooksForMen, model.LooksForWomen);

        var natalChart = _natalChartCalculator.Calculate(user);
        _context.NatalCharts.Add(natalChart);
        await _context.SaveChangesAsync();

        await SendVerificationEmailAsync(user);

        return RedirectToAction(nameof(RegisterConfirmation));
    }

    /// <summary>Info page after registration: "confirm your email address".</summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult RegisterConfirmation() => View();

    /// <summary>Processes the confirmation link from the verification email.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string? userId, string? code)
    {
        if (userId is null || code is null)
        {
            return RedirectToAction("Index", "Home");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        // The token was URL-safe encoded when sent; decode it back here.
        var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await _userManager.ConfirmEmailAsync(user, token);

        ViewData["Gelukt"] = result.Succeeded;
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

        var user = await _userManager.FindByEmailAsync(model.Email);

        // Blocked users are no longer allowed to log in.
        if (user is not null && user.IsBlocked)
        {
            _logger.LogWarning("Blocked user attempted to log in: {Email}.", model.Email);
            ModelState.AddModelError(string.Empty, _localizer["Error_AccountBlocked"]);
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in: {Email}.", model.Email);
            return LocalRedirect(returnUrl ?? Url.Action("Index", "Home")!);
        }

        if (result.IsNotAllowed)
        {
            // Email not confirmed yet: login isn't allowed yet.
            ModelState.AddModelError(string.Empty, _localizer["Error_EmailNotConfirmed"]);
            return View(model);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Account temporarily locked after failed attempts: {Email}.", model.Email);
            ModelState.AddModelError(string.Empty, _localizer["Error_TooManyAttempts"]);
            return View(model);
        }

        ModelState.AddModelError(string.Empty, _localizer["Error_InvalidLoginCredentials"]);
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

    /// <summary>Shown when a user requests a page they don't have permission for.</summary>
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
    /// Generates the confirmation token, builds the absolute link and sends
    /// the verification email. A transient SMTP failure must not crash the
    /// registration itself (the account already exists at that point): the
    /// error is logged instead of bubbling up, same as with the Claude calls.
    /// </summary>
    private async Task SendVerificationEmailAsync(ApplicationUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = Url.Action(nameof(ConfirmEmail), "Account",
            new { userId = user.Id, code }, protocol: Request.Scheme)!;

        try
        {
            await _emailService.SendAsync(
                user.Email!,
                "Bevestig je e-mailadres bij Node",
                $"<p>Welkom bij Node!</p><p><a href=\"{link}\">Klik hier om je e-mailadres te bevestigen</a>.</p>");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sending the verification email to {Email} failed.", user.Email);
        }
    }
}
