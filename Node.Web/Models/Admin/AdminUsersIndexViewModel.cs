namespace Node.Web.Models.Admin;

/// <summary>
/// The Admin/Users page: the filtered/sorted user list plus the current
/// filter state, so the form can show what's currently applied.
/// </summary>
public class AdminUsersIndexViewModel
{
    public IReadOnlyList<UserOverviewViewModel> Gebruikers { get; set; } = [];

    /// <summary>All roles in the system, for the role filter dropdown.</summary>
    public IReadOnlyList<string> AlleRollen { get; set; } = [];

    public string? Zoek { get; set; }

    public string? Rol { get; set; }

    public string? Status { get; set; }

    public string Sortering { get; set; } = "naam_asc";
}
