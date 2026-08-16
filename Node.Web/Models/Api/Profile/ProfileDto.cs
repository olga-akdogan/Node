using Node.Data.Models.Enums;

namespace Node.Web.Models.Api.Profile;

/// <summary>Own-profile response for the MAUI app; the API equivalent of ManageController's Index page.</summary>
public class ProfileDto
{
    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public DateOnly BirthDate { get; set; }

    public TimeOnly BirthTime { get; set; }

    public string BirthPlace { get; set; } = string.Empty;

    public string? ProfilePictureUrl { get; set; }

    public Gender Gender { get; set; }

    public bool LooksForMen { get; set; }

    public bool LooksForWomen { get; set; }
}
