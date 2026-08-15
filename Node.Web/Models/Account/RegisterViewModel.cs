using System.ComponentModel.DataAnnotations;

namespace Node.Web.Models.Account;

/// <summary>
/// Registratieformulier met de extra profielvelden van ApplicationUser.
/// De teksten zelf komen uit de gedeelde resource-bestanden (meertaligheid).
/// </summary>
public class RegisterViewModel
{
    [Required(ErrorMessage = "Valid_EmailVerplicht")]
    [EmailAddress(ErrorMessage = "Valid_EmailOngeldig")]
    [Display(Name = "Veld_Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valid_WachtwoordVerplicht")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Valid_WachtwoordLengte")]
    [DataType(DataType.Password)]
    [Display(Name = "Veld_Wachtwoord")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Veld_BevestigWachtwoord")]
    [Compare(nameof(Password), ErrorMessage = "Valid_WachtwoordenKomenNietOvereen")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valid_WeergavenaamVerplicht")]
    [MaxLength(80, ErrorMessage = "Valid_WeergavenaamMax")]
    [Display(Name = "Veld_Weergavenaam")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valid_GeboortedatumVerplicht")]
    [DataType(DataType.Date)]
    [Display(Name = "Veld_Geboortedatum")]
    public DateOnly? BirthDate { get; set; }

    [Required(ErrorMessage = "Valid_GeboortetijdVerplicht")]
    [DataType(DataType.Time)]
    [Display(Name = "Veld_Geboortetijd")]
    public TimeOnly? BirthTime { get; set; }

    [Required(ErrorMessage = "Valid_GeboorteplaatsVerplicht")]
    [MaxLength(150, ErrorMessage = "Valid_GeboorteplaatsMax")]
    [Display(Name = "Veld_Geboorteplaats")]
    public string BirthPlace { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Valid_BioMax")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Veld_BioOptioneel")]
    public string? Bio { get; set; }
}
