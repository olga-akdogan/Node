using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers;

/// <summary>
/// "Mijn horoscoop" (ontwerp 04): het wiel, de grote drie en de
/// plaatsingentabel van de ingelogde gebruiker. Enkel voor leden: Admin/Moderator
/// zijn beheeraccounts, geen datingprofielen.
/// </summary>
[Authorize(Roles = DbSeeder.RolLid)]
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
        var horoscoop = await _chartService.GetHoroscoopAsync(_userManager.GetUserId(User)!);
        if (horoscoop is null)
        {
            // Nog geen berekende horoscoop: vriendelijk doorverwijzen.
            return View("NogGeenHoroscoop");
        }

        return View(horoscoop);
    }
}
