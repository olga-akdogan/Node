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
/// the MAUI app: same registration/login rules (email verification,
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
            ModelState.AddModelError(string.Empty, _localizer["Valid_ChooseAtLeastOnePreference"]);
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var coordinates = await _geocodingService.FindCoordinatesAsync(request.BirthPlace);
        if (coordinates is null)
        {
            ModelState.AddModelError(nameof(request.BirthPlace), _localizer["Error_BirthPlaceNotFound"]);
            return ValidationProblem(ModelState);
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            Bio = request.Bio,
            BirthDate = request.BirthDate!.Value,
            BirthTime = request.BirthTime!.Value,
            BirthPlace = request.BirthPlace,
            BirthLatitude = coordinates.Value.Latitude,
            BirthLongitude = coordinates.Value.Longitude,
            Gender = request.Gender!.Value,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        await _userManager.AddToRoleAsync(user, DbSeeder.RoleMember);
        _logger.LogInformation("New user registered via API: {Email}.", request.Email);

        AddPartnerPreferences(user.Id, request.LooksForMen, request.LooksForWomen);

        var natalChart = _natalChartCalculator.Calculate(user);
        _context.NatalCharts.Add(natalChart);
        await _context.SaveChangesAsync();

        await SendVerificationEmailAsync(user);

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

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(new { error = _localizer["Error_InvalidLoginCredentials"].Value });
        }

        if (user.IsBlocked)
        {
            _logger.LogWarning("Blocked user attempted to log in via API: {Email}.", request.Email);
            return Unauthorized(new { error = _localizer["Error_AccountBlocked"].Value });
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { error = _localizer["Error_InvalidLoginCredentials"].Value });
        }

        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            return Unauthorized(new { error = _localizer["Error_EmailNotConfirmed"].Value });
        }

        var (token, expiresAt) = await _jwtTokenService.CreateTokenAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        _logger.LogInformation("User logged in via API: {Email}.", request.Email);

        return Ok(new AuthResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAt,
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Roles = roles,
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
    private async Task SendVerificationEmailAsync(ApplicationUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = Url.Action("ConfirmEmail", "Account",
            new { userId = user.Id, code },
            protocol: Request.Scheme)!;

        try
        {
            await _emailService.SendAsync(
                user.Email!,
                "Bevestig je e-mailadres bij Node",
                $"<p>Welkom bij Node!</p><p><a href=\"{link}\">Klik hier om je e-mailadres te bevestigen</a>.</p>");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sending the verification email to {Email} failed (API).", user.Email);
        }
    }
}
