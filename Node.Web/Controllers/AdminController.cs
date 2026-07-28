using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Models.Admin;

namespace Node.Web.Controllers;

/// <summary>
/// Gebruikersbeheer voor beheerders: rollen toekennen en
/// afnemen, gebruikers blokkeren en deblokkeren. Alleen de rol Admin heeft
/// toegang (autorisatie op de controller én in de menustructuur).
/// </summary>
[Authorize(Roles = DbSeeder.RolAdmin)]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>Overzicht van alle gebruikers met hun rollen en blokkeerstatus.</summary>
    [HttpGet]
    public async Task<IActionResult> Users()
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

        return View(model);
    }

    /// <summary>Blokkeert of deblokkeert een gebruiker.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
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
            TempData["Fout"] = "Je kan jezelf niet blokkeren.";
            return RedirectToAction(nameof(Users));
        }

        gebruiker.IsBlocked = !gebruiker.IsBlocked;
        await _userManager.UpdateAsync(gebruiker);

        // Security stamp vernieuwen zodat een bestaande login-cookie van de
        // geblokkeerde gebruiker snel ongeldig wordt (zie ook Program.cs).
        await _userManager.UpdateSecurityStampAsync(gebruiker);

        _logger.LogInformation("Beheerder {Admin} zette blokkering van {Email} op {Status}.",
            User.Identity?.Name, gebruiker.Email, gebruiker.IsBlocked);

        TempData["Melding"] = gebruiker.IsBlocked
            ? $"{gebruiker.DisplayName} is geblokkeerd."
            : $"{gebruiker.DisplayName} is gedeblokkeerd.";

        return RedirectToAction(nameof(Users));
    }

    /// <summary>Kent een rol toe aan een gebruiker.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRole(string id, string rol)
    {
        var gebruiker = await _userManager.FindByIdAsync(id);
        if (gebruiker is null || !await _roleManager.RoleExistsAsync(rol))
        {
            return NotFound();
        }

        await _userManager.AddToRoleAsync(gebruiker, rol);
        _logger.LogInformation("Beheerder {Admin} gaf rol {Rol} aan {Email}.",
            User.Identity?.Name, rol, gebruiker.Email);

        TempData["Melding"] = $"Rol {rol} toegekend aan {gebruiker.DisplayName}.";
        return RedirectToAction(nameof(Users));
    }

    /// <summary>Neemt een rol af van een gebruiker.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
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
            TempData["Fout"] = "Je kan je eigen Admin-rol niet afnemen.";
            return RedirectToAction(nameof(Users));
        }

        await _userManager.RemoveFromRoleAsync(gebruiker, rol);
        _logger.LogInformation("Beheerder {Admin} nam rol {Rol} af van {Email}.",
            User.Identity?.Name, rol, gebruiker.Email);

        TempData["Melding"] = $"Rol {rol} afgenomen van {gebruiker.DisplayName}.";
        return RedirectToAction(nameof(Users));
    }
}
