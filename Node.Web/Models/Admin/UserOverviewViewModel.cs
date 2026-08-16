namespace Node.Web.Models.Admin;

/// <summary>
/// One row in the user management screen: user + roles + blocked status.
/// </summary>
public class UserOverviewViewModel
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool EmailConfirmed { get; set; }

    public bool IsBlocked { get; set; }

    public IList<string> Roles { get; set; } = new List<string>();

    /// <summary>Roles the user doesn't have yet (for the assign dropdown).</summary>
    public IList<string> AssignableRoles { get; set; } = new List<string>();
}
