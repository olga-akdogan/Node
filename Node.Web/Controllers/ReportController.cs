using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Resources;

namespace Node.Web.Controllers;

/// <summary>Reporting misconduct from any web page that shows another member (currently: the chat screen).</summary>
[Authorize]
public class ReportController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<ReportController> _logger;

    public ReportController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<SharedResource> localizer,
        ILogger<ReportController> logger)
    {
        _context = context;
        _userManager = userManager;
        _localizer = localizer;
        _logger = logger;
    }

    /// <summary>
    /// Creates a report and returns to the page it was filed from (matchId,
    /// when given, routes back to that chat; otherwise to the home page).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string reportedUserId, string reason, int? matchId)
    {
        var terug = matchId.HasValue
            ? RedirectToAction("Chat", "Match", new { id = matchId })
            : RedirectToAction("Index", "Home");

        var reporterId = _userManager.GetUserId(User)!;
        if (reportedUserId == reporterId)
        {
            TempData["Fout"] = _localizer["Fout_KanZichzelfNietRapporteren"].Value;
            return terug;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Fout"] = _localizer["Valid_RedenVerplicht"].Value;
            return terug;
        }

        var gerapporteerde = await _userManager.FindByIdAsync(reportedUserId);
        if (gerapporteerde is null)
        {
            return NotFound();
        }

        _context.Reports.Add(new Report
        {
            ReporterUserId = reporterId,
            ReportedUserId = reportedUserId,
            Reason = reason,
        });
        await _context.SaveChangesAsync();

        _logger.LogInformation("Gebruiker {ReporterId} rapporteerde {ReportedId}.", reporterId, reportedUserId);

        TempData["Melding"] = _localizer["Report_Verstuurd"].Value;
        return terug;
    }
}
