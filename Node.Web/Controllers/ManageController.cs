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
/// User parametrization: the logged-in user manages
/// their own profile fields, profile picture and password here.
/// </summary>
[Authorize]
public class ManageController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IGeocodingService _geocodingService;
    private readonly INatalChartCalculator _natalChartCalculator;
    private readonly ApplicationDbContext _context;
    private readonly IProfilePictureService _profilePictureService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<ManageController> _logger;

    public ManageController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IGeocodingService geocodingService,
        INatalChartCalculator natalChartCalculator,
        ApplicationDbContext context,
        IProfilePictureService profilePictureService,
        IStringLocalizer<SharedResource> localizer,
        ILogger<ManageController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _geocodingService = geocodingService;
        _natalChartCalculator = natalChartCalculator;
        _context = context;
        _profilePictureService = profilePictureService;
        _localizer = localizer;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
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

        var model = new ManageProfileViewModel
        {
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            BirthDate = user.BirthDate,
            BirthTime = user.BirthTime,
            BirthPlace = user.BirthPlace,
            CurrentProfilePictureUrl = user.ProfilePictureUrl,
            Gender = user.Gender,
            LooksForMen = preferences.Contains(Gender.Male),
            LooksForWomen = preferences.Contains(Gender.Female),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ManageProfileViewModel model)
    {
        if (model.ProfilePicture is not null && !_profilePictureService.AllowedTypes.ContainsKey(model.ProfilePicture.ContentType))
        {
            ModelState.AddModelError(nameof(model.ProfilePicture), _localizer["Error_ImageType"]);
        }
        else if (model.ProfilePicture is not null && model.ProfilePicture.Length > _profilePictureService.MaxSizeBytes)
        {
            ModelState.AddModelError(nameof(model.ProfilePicture), _localizer["Error_ImageSize"]);
        }

        if (!model.LooksForMen && !model.LooksForWomen)
        {
            ModelState.AddModelError(string.Empty, _localizer["Valid_ChooseAtLeastOnePreference"]);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        model.CurrentProfilePictureUrl = user.ProfilePictureUrl;

        // The natal chart depends on birth date, time AND place: only
        // recalculate (and re-geocode if needed) when one of those three
        // actually changes, not on every profile edit.
        var placeChanged = !string.Equals(user.BirthPlace, model.BirthPlace, StringComparison.Ordinal);
        var birthDataChanged =
            user.BirthDate != model.BirthDate!.Value ||
            user.BirthTime != model.BirthTime!.Value ||
            placeChanged;

        if (placeChanged)
        {
            var coordinates = await _geocodingService.FindCoordinatesAsync(model.BirthPlace);
            if (coordinates is null)
            {
                ModelState.AddModelError(nameof(model.BirthPlace), _localizer["Error_BirthPlaceNotFound"]);
                return View(model);
            }

            user.BirthLatitude = coordinates.Value.Latitude;
            user.BirthLongitude = coordinates.Value.Longitude;
        }

        user.DisplayName = model.DisplayName;
        user.Bio = model.Bio;
        user.BirthDate = model.BirthDate!.Value;
        user.BirthTime = model.BirthTime!.Value;
        user.BirthPlace = model.BirthPlace;
        user.Gender = model.Gender!.Value;

        if (model.ProfilePicture is not null)
        {
            user.ProfilePictureUrl = await _profilePictureService.SaveAsync(user.Id, model.ProfilePicture);
            model.CurrentProfilePictureUrl = user.ProfilePictureUrl;
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await ReplacePartnerPreferencesAsync(user.Id, model.LooksForMen, model.LooksForWomen);

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

            _logger.LogInformation("Natal chart for {Email} recalculated after birth data change.", user.Email);
        }

        _logger.LogInformation("User {Email} updated their own profile.", user.Email);
        TempData["Message"] = _localizer["Success_ProfileSaved"].Value;
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

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        var result = await _userManager.ChangePasswordAsync(
            user, model.CurrentPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // Re-sign-in so the existing cookie stays valid after the change.
        await _signInManager.RefreshSignInAsync(user);
        _logger.LogInformation("User {Email} changed their password.", user.Email);

        TempData["Message"] = _localizer["Success_PasswordChanged"].Value;
        return RedirectToAction(nameof(Index));
    }
}
