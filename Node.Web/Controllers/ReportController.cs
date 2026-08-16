using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Resources;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers;

/// <summary>
/// Reporting misconduct from any web page that shows another member
/// (currently: the chat screen). Members only, same as the chat/match
/// features it's filed from — Admin/Moderator have no matches to report from.
/// </summary>
[Authorize(Roles = DbSeeder.RolLid)]
public class ReportController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMatchService _matchService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<ReportController> _logger;

    public ReportController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IMatchService matchService,
        IStringLocalizer<SharedResource> localizer,
        ILogger<ReportController> logger)
    {
        _context = context;
        _userManager = userManager;
        _matchService = matchService;
        _localizer = localizer;
        _logger = logger;
    }

    /// <summary>
    /// Creates a report and, when it came from an active match, immediately
    /// ends that match — the reporter shouldn't be stuck chatting with
    /// someone they just reported while it's reviewed. On success, that means
    /// the originating chat page no longer exists, so success redirects to
    /// the match list instead of back to it; a validation error (nothing
    /// created yet) still redirects back to the chat.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string reportedUserId, string reason, int? matchId)
    {
        var terugBijFout = matchId.HasValue
            ? RedirectToAction("Chat", "Match", new { id = matchId })
            : RedirectToAction("Index", "Home");

        var reporterId = _userManager.GetUserId(User)!;
        if (reportedUserId == reporterId)
        {
            TempData["Fout"] = _localizer["Fout_KanZichzelfNietRapporteren"].Value;
            return terugBijFout;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Fout"] = _localizer["Valid_RedenVerplicht"].Value;
            return terugBijFout;
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

        await _matchService.EindigMatchTussenAsync(reporterId, reportedUserId);

        _logger.LogInformation("Gebruiker {ReporterId} rapporteerde {ReportedId}.", reporterId, reportedUserId);

        TempData["Melding"] = _localizer["Report_Verstuurd"].Value;
        return RedirectToAction("Index", "Match");
    }
}
