namespace Node.Web.Models.Admin;

/// <summary>
/// Eén rij in het gebruikersbeheerscherm: gebruiker + rollen + blokkeerstatus.
/// </summary>
public class UserOverviewViewModel
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool EmailConfirmed { get; set; }

    public bool IsBlocked { get; set; }

    public IList<string> Roles { get; set; } = new List<string>();

    /// <summary>Rollen die de gebruiker nog niet heeft (voor de toekennen-dropdown).</summary>
    public IList<string> AssignableRoles { get; set; } = new List<string>();
}
