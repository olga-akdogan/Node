namespace Node.Web.Models.Admin;

/// <summary>
/// The Admin/Users page: the filtered/sorted user list plus the current
/// filter state, so the form can show what's currently applied.
/// </summary>
public class AdminUsersIndexViewModel
{
    public IReadOnlyList<UserOverviewViewModel> Users { get; set; } = [];

    /// <summary>All roles in the system, for the role filter dropdown.</summary>
    public IReadOnlyList<string> AllRoles { get; set; } = [];

    public string? Search { get; set; }

    public string? Role { get; set; }

    public string? Status { get; set; }

    public string Sort { get; set; } = "name_asc";
}
