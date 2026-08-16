using System.ComponentModel.DataAnnotations;
using Node.Data.Models.Enums;

namespace Node.Web.Models.Account;

// Registration form
public class RegisterViewModel
{
    [Required(ErrorMessage = "Valid_EmailRequired")]
    [EmailAddress(ErrorMessage = "Valid_EmailInvalid")]
    [Display(Name = "Field_Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valid_PasswordRequired")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Valid_PasswordLength")]
    [DataType(DataType.Password)]
    [Display(Name = "Field_Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Field_ConfirmPassword")]
    [Compare(nameof(Password), ErrorMessage = "Valid_PasswordsDoNotMatch")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valid_DisplayNameRequired")]
    [MaxLength(80, ErrorMessage = "Valid_DisplayNameMax")]
    [Display(Name = "Field_DisplayName")]
    public string DisplayName { get; set; } = string.Empty;

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

    [MaxLength(1000, ErrorMessage = "Valid_BioMax")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Field_BioOptional")]
    public string? Bio { get; set; }

    [Required(ErrorMessage = "Valid_GenderRequired")]
    [Display(Name = "Field_Gender")]
    public Gender? Gender { get; set; }

    /// <summary>Together with <see cref="LooksForWomen"/>, determines who appears in the swipe deck (at least one required).</summary>
    [Display(Name = "Field_SeekingMen")]
    public bool LooksForMen { get; set; }

    [Display(Name = "Field_SeekingWomen")]
    public bool LooksForWomen { get; set; }
}
