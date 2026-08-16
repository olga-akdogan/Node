using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Models.Admin;

namespace Node.Web.Controllers.Api;

/// <summary>API equivalent of <see cref="Node.Web.Controllers.AdminController"/>: user management for the MAUI app's admin role.</summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = DbSeeder.RolAdmin)]
public class AdminUsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AdminUsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserOverviewViewModel>>> GetUsers()
    {
        var alleRollen = await _roleManager.Roles
            .Select(r => r.Name!)
            .OrderBy(r => r)
            .ToListAsync();

        var gebruikers = await _userManager.Users
            .OrderBy(u => u.DisplayName)
            .ToListAsync();

        var model = new List<UserOverviewViewModel>();
        foreach (var gebruiker in gebruikers)
        {
            var rollen = await _userManager.GetRolesAsync(gebruiker);
            model.Add(new UserOverviewViewModel
            {
                Id = gebruiker.Id,
                DisplayName = gebruiker.DisplayName,
                Email = gebruiker.Email ?? string.Empty,
                EmailConfirmed = gebruiker.EmailConfirmed,
                IsBlocked = gebruiker.IsBlocked,
                Roles = rollen,
                AssignableRoles = alleRollen.Except(rollen).ToList(),
            });
        }

        return Ok(model);
    }

    [HttpPost("{id}/toggle-block")]
    public async Task<IActionResult> ToggleBlock(string id)
    {
        var gebruiker = await _userManager.FindByIdAsync(id);
        if (gebruiker is null)
        {
            return NotFound();
        }

        // Een beheerder kan zichzelf niet blokkeren (anders sluit die zichzelf buiten).
        if (gebruiker.Id == _userManager.GetUserId(User))
        {
            return BadRequest(new { error = "Je kan jezelf niet blokkeren." });
        }

        gebruiker.IsBlocked = !gebruiker.IsBlocked;
        await _userManager.UpdateAsync(gebruiker);

        // Security stamp vernieuwen zodat een bestaand token/login van de
        // geblokkeerde gebruiker snel ongeldig wordt (zie ook Program.cs).
        await _userManager.UpdateSecurityStampAsync(gebruiker);

        _logger.LogInformation("Beheerder {Admin} zette blokkering van {Email} op {Status} (API).",
            User.Identity?.Name, gebruiker.Email, gebruiker.IsBlocked);

        return Ok(new { gebruiker.IsBlocked });
    }

    [HttpPost("{id}/roles/{rol}")]
    public async Task<IActionResult> AddRole(string id, string rol)
    {
        var gebruiker = await _userManager.FindByIdAsync(id);
        if (gebruiker is null || !await _roleManager.RoleExistsAsync(rol))
        {
            return NotFound();
        }

        await _userManager.AddToRoleAsync(gebruiker, rol);
        _logger.LogInformation("Beheerder {Admin} gaf rol {Rol} aan {Email} (API).",
            User.Identity?.Name, rol, gebruiker.Email);

        return NoContent();
    }

    [HttpDelete("{id}/roles/{rol}")]
    public async Task<IActionResult> RemoveRole(string id, string rol)
    {
        var gebruiker = await _userManager.FindByIdAsync(id);
        if (gebruiker is null)
        {
            return NotFound();
        }

        // Een beheerder kan de eigen Admin-rol niet afnemen.
        if (gebruiker.Id == _userManager.GetUserId(User) && rol == DbSeeder.RolAdmin)
        {
            return BadRequest(new { error = "Je kan je eigen Admin-rol niet afnemen." });
        }

        await _userManager.RemoveFromRoleAsync(gebruiker, rol);
        _logger.LogInformation("Beheerder {Admin} nam rol {Rol} af van {Email} (API).",
            User.Identity?.Name, rol, gebruiker.Email);

        return NoContent();
    }
}
