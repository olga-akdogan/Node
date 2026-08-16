using System.ComponentModel.DataAnnotations;
using Node.Data.Models.Enums;

namespace Node.Web.Models.Account;

/// <summary>
/// Settings form the user uses to edit their own profile fields
/// (user parametrization with the extra properties).
/// </summary>
public class ManageProfileViewModel
{
    [Required(ErrorMessage = "Valid_DisplayNameRequired")]
    [MaxLength(80, ErrorMessage = "Valid_DisplayNameMax")]
    [Display(Name = "Field_DisplayName")]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Valid_BioMax")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Field_Bio")]
    public string? Bio { get; set; }

    [Required(ErrorMessage = "Valid_BirthDateRequired")]
    [DataType(DataType.Date)]
    [Display(Name = "Field_BirthDate")]
    public DateOnly? BirthDate { get; set; }

    [Required(ErrorMessage = "Valid_BirthTimeRequired")]
    [DataType(DataType.Time)]
    [Display(Name = "Field_BirthTime")]
    public TimeOnly? BirthTime { get; set; }

    [Required(ErrorMessage = "Valid_BirthPlaceRequired")]
    [MaxLength(150, ErrorMessage = "Valid_BirthPlaceMax")]
    [Display(Name = "Field_BirthPlace")]
    public string BirthPlace { get; set; } = string.Empty;

    /// <summary>Only set when the user picks a new profile picture.</summary>
    [Display(Name = "Field_ProfilePicture")]
    public IFormFile? ProfilePicture { get; set; }

    /// <summary>Current photo URL, for display only (not from the form).</summary>
    public string? CurrentProfilePictureUrl { get; set; }

    [Required(ErrorMessage = "Valid_GenderRequired")]
    [Display(Name = "Field_Gender")]
    public Gender? Gender { get; set; }

    /// <summary>Together with <see cref="LooksForWomen"/>, determines who appears in the swipe deck (at least one required).</summary>
    [Display(Name = "Field_SeekingMen")]
    public bool LooksForMen { get; set; }

    [Display(Name = "Field_SeekingWomen")]
    public bool LooksForWomen { get; set; }
}
