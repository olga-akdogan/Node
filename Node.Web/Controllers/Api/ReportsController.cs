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
/// Reporting misconduct and the moderation queue. There is no web-page
/// equivalent yet (the Report model was seeded but never given a UI), so this
/// controller is built directly from the model rather than mirrored from an
/// existing MVC controller. Any signed-in user can file a report; only
/// Moderator/Admin can see and resolve the queue.
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
    /// match between reporter and reported user (see IMatchService.EindigMatchTussenAsync).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateReportRequest request)
    {
        var reporterId = _userManager.GetUserId(User)!;
        if (request.ReportedUserId == reporterId)
        {
            ModelState.AddModelError(nameof(request.ReportedUserId), _localizer["Fout_KanZichzelfNietRapporteren"]);
            return ValidationProblem(ModelState);
        }

        var gerapporteerde = await _userManager.FindByIdAsync(request.ReportedUserId);
        if (gerapporteerde is null)
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

        await _matchService.EindigMatchTussenAsync(reporterId, request.ReportedUserId);

        _logger.LogInformation("Gebruiker {ReporterId} rapporteerde {ReportedId} (API).", reporterId, request.ReportedUserId);

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpGet]
    [Authorize(Roles = $"{DbSeeder.RolModerator},{DbSeeder.RolAdmin}")]
    public async Task<ActionResult<List<ReportOverviewViewModel>>> GetQueue()
    {
        var meldingen = await _context.Reports
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

        return Ok(meldingen);
    }

    [HttpPost("{id}/resolve")]
    [Authorize(Roles = $"{DbSeeder.RolModerator},{DbSeeder.RolAdmin}")]
    public async Task<IActionResult> Resolve(int id)
    {
        var melding = await _context.Reports.FindAsync(id);
        if (melding is null)
        {
            return NotFound();
        }

        melding.IsResolved = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Moderator {Moderator} handelde melding {Id} af (API).", User.Identity?.Name, id);

        return NoContent();
    }

    /// <summary>Blocks the reported user's account and marks the report resolved. Moderator or Admin only.</summary>
    [HttpPost("{id}/block")]
    [Authorize(Roles = $"{DbSeeder.RolModerator},{DbSeeder.RolAdmin}")]
    public async Task<IActionResult> BlockReportedUser(int id)
    {
        var melding = await _context.Reports.FindAsync(id);
        if (melding is null)
        {
            return NotFound();
        }

        var gerapporteerde = await _userManager.FindByIdAsync(melding.ReportedUserId);
        if (gerapporteerde is null)
        {
            return NotFound();
        }

        if (gerapporteerde.Id == _userManager.GetUserId(User))
        {
            return BadRequest(new { error = "Je kan jezelf niet blokkeren." });
        }

        gerapporteerde.IsBlocked = true;
        await _userManager.UpdateAsync(gerapporteerde);
        await _userManager.UpdateSecurityStampAsync(gerapporteerde);

        melding.IsResolved = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("{Moderator} blokkeerde {Email} n.a.v. melding {Id} (API).",
            User.Identity?.Name, gerapporteerde.Email, id);

        return NoContent();
    }
}
