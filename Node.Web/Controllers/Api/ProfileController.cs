using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Node.Data.Data;
using Node.Data.Models;
using Node.Data.Models.Enums;
using Node.Data.Services;
using Node.Web.Models.Api.Profile;
using Node.Web.Resources;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers.Api;

/// <summary>API equivalent of <see cref="Node.Web.Controllers.ManageController"/> for the MAUI app: view/edit the own profile.</summary>
[ApiController]
[Route("api/profile")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGeocodingService _geocodingService;
    private readonly INatalChartCalculator _natalChartCalculator;
    private readonly ApplicationDbContext _context;
    private readonly IProfilePictureService _profilePictureService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        IGeocodingService geocodingService,
        INatalChartCalculator natalChartCalculator,
        ApplicationDbContext context,
        IProfilePictureService profilePictureService,
        IStringLocalizer<SharedResource> localizer,
        ILogger<ProfileController> logger)
    {
        _userManager = userManager;
        _geocodingService = geocodingService;
        _natalChartCalculator = natalChartCalculator;
        _context = context;
        _profilePictureService = profilePictureService;
        _localizer = localizer;
        _logger = logger;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ProfileDto>> GetMe()
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

        return Ok(new ProfileDto
        {
            Email = gebruiker.Email ?? string.Empty,
            DisplayName = gebruiker.DisplayName,
            Bio = gebruiker.Bio,
            BirthDate = gebruiker.BirthDate,
            BirthTime = gebruiker.BirthTime,
            BirthPlace = gebruiker.BirthPlace,
            ProfilePictureUrl = gebruiker.ProfilePictureUrl,
            Gender = gebruiker.Gender,
            LooksForMen = preferences.Contains(Gender.Male),
            LooksForWomen = preferences.Contains(Gender.Female),
        });
    }

    [HttpPut("me")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ProfileDto>> UpdateMe([FromForm] UpdateProfileRequest request)
    {
        if (request.ProfilePicture is not null && !_profilePictureService.ToegestaneTypes.ContainsKey(request.ProfilePicture.ContentType))
        {
            ModelState.AddModelError(nameof(request.ProfilePicture), _localizer["Fout_AfbeeldingType"]);
        }
        else if (request.ProfilePicture is not null && request.ProfilePicture.Length > _profilePictureService.MaxGrootteBytes)
        {
            ModelState.AddModelError(nameof(request.ProfilePicture), _localizer["Fout_AfbeeldingGrootte"]);
        }

        if (!request.LooksForMen && !request.LooksForWomen)
        {
            ModelState.AddModelError(string.Empty, _localizer["Valid_KiesMinstensEenVoorkeur"]);
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var gebruiker = await _userManager.GetUserAsync(User);
        if (gebruiker is null)
        {
            return NotFound();
        }

        // De horoscoop hangt af van geboortedatum, -tijd én -plaats: enkel
        // opnieuw berekenen (en eventueel opnieuw geocoderen) wanneer één van
        // die drie effectief wijzigt, niet bij elke profielwijziging.
        var plaatsGewijzigd = !string.Equals(gebruiker.BirthPlace, request.BirthPlace, StringComparison.Ordinal);
        var geboortegegevensGewijzigd =
            gebruiker.BirthDate != request.BirthDate!.Value ||
            gebruiker.BirthTime != request.BirthTime!.Value ||
            plaatsGewijzigd;

        if (plaatsGewijzigd)
        {
            var coordinaten = await _geocodingService.ZoekCoordinatenAsync(request.BirthPlace);
            if (coordinaten is null)
            {
                ModelState.AddModelError(nameof(request.BirthPlace), _localizer["Fout_GeboorteplaatsNietGevonden"]);
                return ValidationProblem(ModelState);
            }

            gebruiker.BirthLatitude = coordinaten.Value.Latitude;
            gebruiker.BirthLongitude = coordinaten.Value.Longitude;
        }

        gebruiker.DisplayName = request.DisplayName;
        gebruiker.Bio = request.Bio;
        gebruiker.BirthDate = request.BirthDate!.Value;
        gebruiker.BirthTime = request.BirthTime!.Value;
        gebruiker.BirthPlace = request.BirthPlace;
        gebruiker.Gender = request.Gender!.Value;

        if (request.ProfilePicture is not null)
        {
            gebruiker.ProfilePictureUrl = await _profilePictureService.BewaarAsync(gebruiker.Id, request.ProfilePicture);
        }

        var resultaat = await _userManager.UpdateAsync(gebruiker);
        if (!resultaat.Succeeded)
        {
            foreach (var fout in resultaat.Errors)
            {
                ModelState.AddModelError(string.Empty, fout.Description);
            }

            return ValidationProblem(ModelState);
        }

        await ReplacePartnerPreferencesAsync(gebruiker.Id, request.LooksForMen, request.LooksForWomen);

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

            _logger.LogInformation("Horoscoop van {Email} opnieuw berekend na wijziging van de geboortegegevens (API).", gebruiker.Email);
        }

        _logger.LogInformation("Gebruiker {Email} paste het eigen profiel aan (API).", gebruiker.Email);

        return await GetMe();
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
}
