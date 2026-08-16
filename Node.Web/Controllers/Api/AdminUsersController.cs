using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Models.Admin;
using Node.Web.Resources;

namespace Node.Web.Controllers.Api;

/// <summary>API equivalent of <see cref="Node.Web.Controllers.AdminController"/>: user management for the MAUI app's admin role.</summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = DbSeeder.RoleAdmin)]
public class AdminUsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IStringLocalizer<SharedResource> localizer,
        ILogger<AdminUsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _localizer = localizer;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserOverviewViewModel>>> GetUsers()
    {
        var allRoles = await _roleManager.Roles
            .Select(r => r.Name!)
            .OrderBy(r => r)
            .ToListAsync();

        var users = await _userManager.Users
            .OrderBy(u => u.DisplayName)
            .ToListAsync();

        var model = new List<UserOverviewViewModel>();
        foreach (var user in users)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            model.Add(new UserOverviewViewModel
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                IsBlocked = user.IsBlocked,
                Roles = userRoles,
                AssignableRoles = allRoles.Except(userRoles).ToList(),
            });
        }

        return Ok(model);
    }

    [HttpPost("{id}/toggle-block")]
    public async Task<IActionResult> ToggleBlock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        // An admin can't block themselves (that would lock them out).
        if (user.Id == _userManager.GetUserId(User))
        {
            return BadRequest(new { error = _localizer["Admin_CannotBlockYourself"].Value });
        }

        user.IsBlocked = !user.IsBlocked;
        await _userManager.UpdateAsync(user);

        // Refresh the security stamp so an existing token/login of the
        // blocked user becomes invalid quickly (see also Program.cs).
        await _userManager.UpdateSecurityStampAsync(user);

        _logger.LogInformation("Admin {Admin} set block status of {Email} to {Status} (API).",
            User.Identity?.Name, user.Email, user.IsBlocked);

        return Ok(new { user.IsBlocked });
    }

    [HttpPost("{id}/roles/{role}")]
    public async Task<IActionResult> AddRole(string id, string role)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null || !await _roleManager.RoleExistsAsync(role))
        {
            return NotFound();
        }

        await _userManager.AddToRoleAsync(user, role);
        _logger.LogInformation("Admin {Admin} gave role {Role} to {Email} (API).",
            User.Identity?.Name, role, user.Email);

        return NoContent();
    }

    [HttpDelete("{id}/roles/{role}")]
    public async Task<IActionResult> RemoveRole(string id, string role)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        // An admin can't remove their own Admin role.
        if (user.Id == _userManager.GetUserId(User) && role == DbSeeder.RoleAdmin)
        {
            return BadRequest(new { error = _localizer["Admin_CannotRemoveOwnAdminRole"].Value });
        }

        await _userManager.RemoveFromRoleAsync(user, role);
        _logger.LogInformation("Admin {Admin} removed role {Role} from {Email} (API).",
            User.Identity?.Name, role, user.Email);

        return NoContent();
    }
}
