using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers;

/// <summary>
/// "My chart": the wheel, the big three and the placements
/// table of the logged-in user. Members only: Admin/Moderator are staff
/// accounts, not dating profiles.
/// </summary>
[Authorize(Roles = DbSeeder.RoleMember)]
public class ChartController : Controller
{
    private readonly IChartService _chartService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChartController(IChartService chartService, UserManager<ApplicationUser> userManager)
    {
        _chartService = chartService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var horoscope = await _chartService.GetHoroscopeAsync(_userManager.GetUserId(User)!);
        if (horoscope is null)
        {
            // No calculated chart yet: redirect to a friendly placeholder.
            return View("NoChartYet");
        }

        return View(horoscope);
    }
}
