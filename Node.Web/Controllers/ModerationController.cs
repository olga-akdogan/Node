using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Models.Moderation;
using Node.Web.Resources;

namespace Node.Web.Controllers;

/// <summary>
/// The report queue: viewing and handling reported users. Accessible to
/// Moderator and Admin — this is what concretely distinguishes the
/// Moderator role from a regular member. A Moderator can block a reported
/// user, but not unblock or manage roles: that stays reserved for Admin
/// (Admin/Users).
/// </summary>
[Authorize(Roles = $"{DbSeeder.RoleModerator},{DbSeeder.RoleAdmin}")]
public class ModerationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<ModerationController> _logger;

    public ModerationController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<SharedResource> localizer,
        ILogger<ModerationController> logger)
    {
        _context = context;
        _userManager = userManager;
        _localizer = localizer;
        _logger = logger;
    }

    /// <summary>Overview of the reports, by default only the ones not yet handled.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(bool unresolvedOnly = true)
    {
        var query = _context.Reports
            .Include(r => r.ReporterUser)
            .Include(r => r.ReportedUser)
            .AsQueryable();

        if (unresolvedOnly)
        {
            query = query.Where(r => !r.IsResolved);
        }

        var reports = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReportOverviewViewModel
            {
                Id = r.Id,
                ReporterDisplayName = r.ReporterUser!.DisplayName,
                ReportedUserId = r.ReportedUserId,
                ReportedDisplayName = r.ReportedUser!.DisplayName,
                ReportedUserIsBlocked = r.ReportedUser!.IsBlocked,
                Reason = r.Reason,
                IsResolved = r.IsResolved,
                CreatedAt = r.CreatedAt,
            })
            .ToListAsync();

        ViewData["UnresolvedOnly"] = unresolvedOnly;
        return View(reports);
    }

    /// <summary>Marks a report as resolved.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(int id, bool unresolvedOnly = true)
    {
        var report = await _context.Reports.FindAsync(id);
        if (report is null)
        {
            return NotFound();
        }

        report.IsResolved = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("{Moderator} resolved report {Id}.", User.Identity?.Name, id);

        TempData["Message"] = _localizer["Moderation_ResolvedMessage"].Value;
        return RedirectToAction(nameof(Index), new { unresolvedOnly });
    }

    /// <summary>
    /// Blocks the reported user and marks the report resolved right away.
    /// Blocking only, no unblocking: that stays with Admin.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BlockReportedUser(int id, bool unresolvedOnly = true)
    {
        var report = await _context.Reports.FindAsync(id);
        if (report is null)
        {
            return NotFound();
        }

        var reportedUser = await _userManager.FindByIdAsync(report.ReportedUserId);
        if (reportedUser is null)
        {
            return NotFound();
        }

        if (reportedUser.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = _localizer["Admin_CannotBlockYourself"].Value;
            return RedirectToAction(nameof(Index), new { unresolvedOnly });
        }

        reportedUser.IsBlocked = true;
        await _userManager.UpdateAsync(reportedUser);

        // Refresh the security stamp so an existing login cookie of the
        // blocked user becomes invalid quickly (see also Program.cs).
        await _userManager.UpdateSecurityStampAsync(reportedUser);

        report.IsResolved = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("{Moderator} blocked {Email} following report {Id}.",
            User.Identity?.Name, reportedUser.Email, id);

        TempData["Message"] = _localizer["Moderation_BlockedMessage"].Value;
        return RedirectToAction(nameof(Index), new { unresolvedOnly });
    }
}
