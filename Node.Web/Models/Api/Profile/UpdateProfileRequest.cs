using System.ComponentModel.DataAnnotations;
using Node.Data.Models.Enums;

namespace Node.Web.Models.Api.Profile;

/// <summary>
/// API equivalent of ManageProfileViewModel. Sent as multipart/form-data so
/// the optional profile picture can travel alongside the other fields.
/// </summary>
public class UpdateProfileRequest
{
    [Required(ErrorMessage = "Valid_WeergavenaamVerplicht")]
    [MaxLength(80, ErrorMessage = "Valid_WeergavenaamMax")]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Valid_BioMax")]
    public string? Bio { get; set; }

    [Required(ErrorMessage = "Valid_GeboortedatumVerplicht")]
    public DateOnly? BirthDate { get; set; }

    [Required(ErrorMessage = "Valid_GeboortetijdVerplicht")]
    public TimeOnly? BirthTime { get; set; }

    [Required(ErrorMessage = "Valid_GeboorteplaatsVerplicht")]
    [MaxLength(150, ErrorMessage = "Valid_GeboorteplaatsMax")]
    public string BirthPlace { get; set; } = string.Empty;

    /// <summary>Only set when the user picks a new profile picture.</summary>
    public IFormFile? ProfilePicture { get; set; }

    [Required(ErrorMessage = "Valid_GeslachtVerplicht")]
    public Gender? Gender { get; set; }

    public bool LooksForMen { get; set; }

    public bool LooksForWomen { get; set; }
}
