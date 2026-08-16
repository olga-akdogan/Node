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
using Node.Web.Models.Api.Auth;
using Node.Web.Resources;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers.Api;

/// <summary>
/// API equivalent of <see cref="Node.Web.Controllers.AccountController"/> for
/// the MAUI companion app: same registration/login rules (email verification,
/// blocked-account check, auto-assigned "Lid" role) but returns a JWT instead
/// of setting the Identity cookie.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly IGeocodingService _geocodingService;
    private readonly INatalChartCalculator _natalChartCalculator;
    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        IGeocodingService geocodingService,
        INatalChartCalculator natalChartCalculator,
        ApplicationDbContext context,
        IStringLocalizer<SharedResource> localizer,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _geocodingService = geocodingService;
        _natalChartCalculator = natalChartCalculator;
        _context = context;
        _localizer = localizer;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (!request.LooksForMen && !request.LooksForWomen)
        {
            ModelState.AddModelError(string.Empty, _localizer["Valid_KiesMinstensEenVoorkeur"]);
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var coordinaten = await _geocodingService.ZoekCoordinatenAsync(request.BirthPlace);
        if (coordinaten is null)
        {
            ModelState.AddModelError(nameof(request.BirthPlace), _localizer["Fout_GeboorteplaatsNietGevonden"]);
            return ValidationProblem(ModelState);
        }

        var gebruiker = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            Bio = request.Bio,
            BirthDate = request.BirthDate!.Value,
            BirthTime = request.BirthTime!.Value,
            BirthPlace = request.BirthPlace,
            BirthLatitude = coordinaten.Value.Latitude,
            BirthLongitude = coordinaten.Value.Longitude,
            Gender = request.Gender!.Value,
        };

        var resultaat = await _userManager.CreateAsync(gebruiker, request.Password);
        if (!resultaat.Succeeded)
        {
            foreach (var fout in resultaat.Errors)
            {
                ModelState.AddModelError(string.Empty, fout.Description);
            }

            return ValidationProblem(ModelState);
        }

        await _userManager.AddToRoleAsync(gebruiker, DbSeeder.RolLid);
        _logger.LogInformation("Nieuwe gebruiker geregistreerd via API: {Email}.", request.Email);

        AddPartnerPreferences(gebruiker.Id, request.LooksForMen, request.LooksForWomen);

        var horoscoop = _natalChartCalculator.Calculate(gebruiker);
        _context.NatalCharts.Add(horoscoop);
        await _context.SaveChangesAsync();

        await VerstuurVerificatieEmailAsync(gebruiker);

        // Email verification is required before login, so no token yet: the
        // app should show the same "check your inbox" message as the web flow.
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var gebruiker = await _userManager.FindByEmailAsync(request.Email);
        if (gebruiker is null)
        {
            return Unauthorized(new { error = _localizer["Fout_OngeldigeInloggegevens"].Value });
        }

        if (gebruiker.IsBlocked)
        {
            _logger.LogWarning("Geblokkeerde gebruiker probeerde via API in te loggen: {Email}.", request.Email);
            return Unauthorized(new { error = _localizer["Fout_AccountGeblokkeerd"].Value });
        }

        if (!await _userManager.CheckPasswordAsync(gebruiker, request.Password))
        {
            return Unauthorized(new { error = _localizer["Fout_OngeldigeInloggegevens"].Value });
        }

        if (!await _userManager.IsEmailConfirmedAsync(gebruiker))
        {
            return Unauthorized(new { error = _localizer["Fout_EmailNietBevestigd"].Value });
        }

        var (token, verlooptOp) = await _jwtTokenService.CreateTokenAsync(gebruiker);
        var rollen = await _userManager.GetRolesAsync(gebruiker);

        _logger.LogInformation("Gebruiker ingelogd via API: {Email}.", request.Email);

        return Ok(new AuthResponse
        {
            Token = token,
            ExpiresAtUtc = verlooptOp,
            UserId = gebruiker.Id,
            DisplayName = gebruiker.DisplayName,
            Roles = rollen,
        });
    }

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
    /// A transient SMTP failure must not crash the registration request
    /// itself (the account already exists at this point): log it instead of
    /// letting it bubble, same as the AccountController web equivalent.
    /// </summary>
    private async Task VerstuurVerificatieEmailAsync(ApplicationUser gebruiker)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(gebruiker);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = Url.Action("ConfirmEmail", "Account",
            new { userId = gebruiker.Id, code },
            protocol: Request.Scheme)!;

        try
        {
            await _emailService.SendAsync(
                gebruiker.Email!,
                "Bevestig je e-mailadres bij Node",
                $"<p>Welkom bij Node!</p><p><a href=\"{link}\">Klik hier om je e-mailadres te bevestigen</a>.</p>");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Versturen van de verificatie-e-mail naar {Email} is mislukt (API).", gebruiker.Email);
        }
    }
}
