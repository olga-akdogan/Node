using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Models.Chart;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers.Api;

/// <summary>
/// API equivalent of <see cref="Node.Web.Controllers.ChartController"/>: the own natal
/// chart for the MAUI app. Members only, same as the web page.
/// </summary>
[ApiController]
[Route("api/natalcharts")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = DbSeeder.RoleMember)]
public class NatalChartsController : ControllerBase
{
    private readonly IChartService _chartService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NatalChartsController(IChartService chartService, UserManager<ApplicationUser> userManager)
    {
        _chartService = chartService;
        _userManager = userManager;
    }

    [HttpGet("me")]
    public async Task<ActionResult<HoroscopeViewModel>> GetMe()
    {
        var horoscope = await _chartService.GetHoroscopeAsync(_userManager.GetUserId(User)!);
        if (horoscope is null)
        {
            // No calculated chart yet (e.g. account just created before the background calculation).
            return NotFound();
        }

        return Ok(horoscope);
    }
}
