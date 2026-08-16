using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Node.Data.Data;
using Node.Data.Models;
using Node.Web.Models.Admin;
using Node.Web.Resources;

namespace Node.Web.Controllers;

/// <summary>
/// User management for administrators: assigning and removing roles,
/// blocking and unblocking users. Only the Admin role has access
/// (authorization on the controller and in the menu structure).
/// </summary>
[Authorize(Roles = DbSeeder.RoleAdmin)]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IStringLocalizer<SharedResource> localizer,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _localizer = localizer;
        _logger = logger;
    }

    /// <summary>
    /// Overview of all users with their roles and blocked status, with a
    /// search/role/status filter plus sorting by name or email.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Users(string? search, string? role, string? status, string sort = "name_asc")
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

        // Small number of users (demo scale): filtering and sorting happens
        // here in-memory on the already-built list, not with an extra
        // EF query joining AspNetUserRoles.
        IEnumerable<UserOverviewViewModel> filtered = model;

        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(g =>
                g.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                g.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            filtered = filtered.Where(g => g.Roles.Contains(role));
        }

        filtered = status switch
        {
            "blocked" => filtered.Where(g => g.IsBlocked),
            "active" => filtered.Where(g => !g.IsBlocked),
            _ => filtered,
        };

        filtered = sort switch
        {
            "name_desc" => filtered.OrderByDescending(g => g.DisplayName),
            "email_asc" => filtered.OrderBy(g => g.Email),
            "email_desc" => filtered.OrderByDescending(g => g.Email),
            _ => filtered.OrderBy(g => g.DisplayName),
        };

        return View(new AdminUsersIndexViewModel
        {
            Users = filtered.ToList(),
            AllRoles = allRoles,
            Search = search,
            Role = role,
            Status = status,
            Sort = sort,
        });
    }

    /// <summary>Blocks or unblocks a user.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
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
            TempData["Error"] = _localizer["Admin_CannotBlockYourself"].Value;
            return RedirectToAction(nameof(Users));
        }

        user.IsBlocked = !user.IsBlocked;
        await _userManager.UpdateAsync(user);

        // Refresh the security stamp so an existing login cookie of the
        // blocked user becomes invalid quickly (see also Program.cs).
        await _userManager.UpdateSecurityStampAsync(user);

        _logger.LogInformation("Admin {Admin} set block status of {Email} to {Status}.",
            User.Identity?.Name, user.Email, user.IsBlocked);

        TempData["Message"] = user.IsBlocked
            ? string.Format(_localizer["Admin_UserBlockedMessage"].Value, user.DisplayName)
            : string.Format(_localizer["Admin_UserUnblockedMessage"].Value, user.DisplayName);

        return RedirectToAction(nameof(Users));
    }

    /// <summary>Assigns a role to a user.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRole(string id, string role)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null || !await _roleManager.RoleExistsAsync(role))
        {
            return NotFound();
        }

        await _userManager.AddToRoleAsync(user, role);
        _logger.LogInformation("Admin {Admin} gave role {Role} to {Email}.",
            User.Identity?.Name, role, user.Email);

        TempData["Message"] = string.Format(_localizer["Admin_RoleAssignedMessage"].Value, role, user.DisplayName);
        return RedirectToAction(nameof(Users));
    }

    /// <summary>Removes a role from a user.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
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
            TempData["Error"] = _localizer["Admin_CannotRemoveOwnAdminRole"].Value;
            return RedirectToAction(nameof(Users));
        }

        await _userManager.RemoveFromRoleAsync(user, role);
        _logger.LogInformation("Admin {Admin} removed role {Role} from {Email}.",
            User.Identity?.Name, role, user.Email);

        TempData["Message"] = string.Format(_localizer["Admin_RoleRemovedMessage"].Value, role, user.DisplayName);
        return RedirectToAction(nameof(Users));
    }
}
