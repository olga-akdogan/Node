using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Node.Data.Data;
using Node.Web.Models.Moderation;
using Node.Web.Resources;

namespace Node.Web.Controllers;

/// <summary>
/// De meldingenwachtrij: gerapporteerde gebruikers bekijken en afhandelen.
/// Toegankelijk voor Moderator en Admin — dit is wat de rol Moderator
/// concreet onderscheidt van een gewoon lid.
/// </summary>
[Authorize(Roles = $"{DbSeeder.RolModerator},{DbSeeder.RolAdmin}")]
public class ModerationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<ModerationController> _logger;

    public ModerationController(ApplicationDbContext context, IStringLocalizer<SharedResource> localizer, ILogger<ModerationController> logger)
    {
        _context = context;
        _localizer = localizer;
        _logger = logger;
    }

    /// <summary>Overzicht van de meldingen, standaard enkel de nog niet afgehandelde.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(bool alleenOnopgelost = true)
    {
        var query = _context.Reports
            .Include(r => r.ReporterUser)
            .Include(r => r.ReportedUser)
            .AsQueryable();

        if (alleenOnopgelost)
        {
            query = query.Where(r => !r.IsResolved);
        }

        var meldingen = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReportOverviewViewModel
            {
                Id = r.Id,
                ReporterDisplayName = r.ReporterUser!.DisplayName,
                ReportedUserId = r.ReportedUserId,
                ReportedDisplayName = r.ReportedUser!.DisplayName,
                Reason = r.Reason,
                IsResolved = r.IsResolved,
                CreatedAt = r.CreatedAt,
            })
            .ToListAsync();

        ViewData["AlleenOnopgelost"] = alleenOnopgelost;
        return View(meldingen);
    }

    /// <summary>Markeert een melding als afgehandeld.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(int id, bool alleenOnopgelost = true)
    {
        var melding = await _context.Reports.FindAsync(id);
        if (melding is null)
        {
            return NotFound();
        }

        melding.IsResolved = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("{Moderator} handelde melding {Id} af.", User.Identity?.Name, id);

        TempData["Melding"] = _localizer["Moderation_AfgehandeldMelding"].Value;
        return RedirectToAction(nameof(Index), new { alleenOnopgelost });
    }
}
