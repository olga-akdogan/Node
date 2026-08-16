using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Models.Api.Reports;
using Node.Web.Models.Moderation;
using Node.Web.Resources;
using Node.Web.Services.Interfaces;

namespace Node.Web.Controllers.Api;

/// <summary>
/// Reporting misconduct and the moderation queue, mirroring the web
/// ReportController/ModerationController. Members can file a report; only
/// Moderator/Admin can see, resolve, or block from the queue.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMatchService _matchService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IMatchService matchService,
        IStringLocalizer<SharedResource> localizer,
        ILogger<ReportsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _matchService = matchService;
        _localizer = localizer;
        _logger = logger;
    }

    /// <summary>
    /// Creates a report and, when one exists, immediately ends the active
    /// match between reporter and reported user (see IMatchService.EndMatchBetweenAsync).
    /// Members only, matching the web ReportController.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = DbSeeder.RoleMember)]
    public async Task<IActionResult> Create(CreateReportRequest request)
    {
        var reporterId = _userManager.GetUserId(User)!;
        if (request.ReportedUserId == reporterId)
        {
            ModelState.AddModelError(nameof(request.ReportedUserId), _localizer["Error_CannotReportYourself"]);
            return ValidationProblem(ModelState);
        }

        var reportedUser = await _userManager.FindByIdAsync(request.ReportedUserId);
        if (reportedUser is null)
        {
            return NotFound();
        }

        _context.Reports.Add(new Report
        {
            ReporterUserId = reporterId,
            ReportedUserId = request.ReportedUserId,
            Reason = request.Reason,
        });
        await _context.SaveChangesAsync();

        await _matchService.EndMatchBetweenAsync(reporterId, request.ReportedUserId);

        _logger.LogInformation("User {ReporterId} reported {ReportedId} (API).", reporterId, request.ReportedUserId);

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpGet]
    [Authorize(Roles = $"{DbSeeder.RoleModerator},{DbSeeder.RoleAdmin}")]
    public async Task<ActionResult<List<ReportOverviewViewModel>>> GetQueue()
    {
        var reports = await _context.Reports
            .Include(r => r.ReporterUser)
            .Include(r => r.ReportedUser)
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

        return Ok(reports);
    }

    [HttpPost("{id}/resolve")]
    [Authorize(Roles = $"{DbSeeder.RoleModerator},{DbSeeder.RoleAdmin}")]
    public async Task<IActionResult> Resolve(int id)
    {
        var report = await _context.Reports.FindAsync(id);
        if (report is null)
        {
            return NotFound();
        }

        report.IsResolved = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Moderator {Moderator} resolved report {Id} (API).", User.Identity?.Name, id);

        return NoContent();
    }

    /// <summary>Blocks the reported user's account and marks the report resolved. Moderator or Admin only.</summary>
    [HttpPost("{id}/block")]
    [Authorize(Roles = $"{DbSeeder.RoleModerator},{DbSeeder.RoleAdmin}")]
    public async Task<IActionResult> BlockReportedUser(int id)
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
            return BadRequest(new { error = _localizer["Admin_CannotBlockYourself"].Value });
        }

        reportedUser.IsBlocked = true;
        await _userManager.UpdateAsync(reportedUser);
        await _userManager.UpdateSecurityStampAsync(reportedUser);

        report.IsResolved = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("{Moderator} blocked {Email} following report {Id} (API).",
            User.Identity?.Name, reportedUser.Email, id);

        return NoContent();
    }
}
