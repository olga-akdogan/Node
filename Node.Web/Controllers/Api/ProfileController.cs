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
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        var preferences = await _context.PartnerPreferences
            .Where(p => p.UserId == user.Id)
            .Select(p => p.Gender)
            .ToListAsync();

        return Ok(new ProfileDto
        {
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            BirthDate = user.BirthDate,
            BirthTime = user.BirthTime,
            BirthPlace = user.BirthPlace,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Gender = user.Gender,
            LooksForMen = preferences.Contains(Gender.Male),
            LooksForWomen = preferences.Contains(Gender.Female),
        });
    }

    [HttpPut("me")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ProfileDto>> UpdateMe([FromForm] UpdateProfileRequest request)
    {
        if (request.ProfilePicture is not null && !_profilePictureService.AllowedTypes.ContainsKey(request.ProfilePicture.ContentType))
        {
            ModelState.AddModelError(nameof(request.ProfilePicture), _localizer["Error_ImageType"]);
        }
        else if (request.ProfilePicture is not null && request.ProfilePicture.Length > _profilePictureService.MaxSizeBytes)
        {
            ModelState.AddModelError(nameof(request.ProfilePicture), _localizer["Error_ImageSize"]);
        }

        if (!request.LooksForMen && !request.LooksForWomen)
        {
            ModelState.AddModelError(string.Empty, _localizer["Valid_ChooseAtLeastOnePreference"]);
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        // The natal chart depends on birth date, time AND place: only
        // recalculate (and re-geocode if needed) when one of those three
        // actually changes, not on every profile edit.
        var placeChanged = !string.Equals(user.BirthPlace, request.BirthPlace, StringComparison.Ordinal);
        var birthDataChanged =
            user.BirthDate != request.BirthDate!.Value ||
            user.BirthTime != request.BirthTime!.Value ||
            placeChanged;

        if (placeChanged)
        {
            var coordinates = await _geocodingService.FindCoordinatesAsync(request.BirthPlace);
            if (coordinates is null)
            {
                ModelState.AddModelError(nameof(request.BirthPlace), _localizer["Error_BirthPlaceNotFound"]);
                return ValidationProblem(ModelState);
            }

            user.BirthLatitude = coordinates.Value.Latitude;
            user.BirthLongitude = coordinates.Value.Longitude;
        }

        user.DisplayName = request.DisplayName;
        user.Bio = request.Bio;
        user.BirthDate = request.BirthDate!.Value;
        user.BirthTime = request.BirthTime!.Value;
        user.BirthPlace = request.BirthPlace;
        user.Gender = request.Gender!.Value;

        if (request.ProfilePicture is not null)
        {
            user.ProfilePictureUrl = await _profilePictureService.SaveAsync(user.Id, request.ProfilePicture);
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        await ReplacePartnerPreferencesAsync(user.Id, request.LooksForMen, request.LooksForWomen);

        if (birthDataChanged)
        {
            var existingChart = await _context.NatalCharts.FirstOrDefaultAsync(n => n.UserId == user.Id);
            if (existingChart is not null)
            {
                _context.NatalCharts.Remove(existingChart);
            }

            var newChart = _natalChartCalculator.Calculate(user);
            _context.NatalCharts.Add(newChart);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Natal chart for {Email} recalculated after birth data change (API).", user.Email);
        }

        _logger.LogInformation("User {Email} updated their own profile (API).", user.Email);

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
