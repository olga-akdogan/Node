using System.ComponentModel.DataAnnotations;
using Node.Data.Models.Enums;

namespace Node.Web.Models.Account;

/// <summary>
/// Instellingenformulier waarmee de gebruiker de eigen profielvelden aanpast
/// (gebruikersparametrisatie met de extra eigenschappen).
/// </summary>
public class ManageProfileViewModel
{
    [Required(ErrorMessage = "Valid_WeergavenaamVerplicht")]
    [MaxLength(80, ErrorMessage = "Valid_WeergavenaamMax")]
    [Display(Name = "Veld_Weergavenaam")]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Valid_BioMax")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Veld_Bio")]
    public string? Bio { get; set; }

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

    /// <summary>Enkel ingevuld wanneer de gebruiker een nieuwe profielfoto kiest.</summary>
    [Display(Name = "Veld_Profielfoto")]
    public IFormFile? ProfilePicture { get; set; }

    /// <summary>Huidige foto-URL, enkel voor weergave (niet uit het formulier).</summary>
    public string? HuidigeProfielFotoUrl { get; set; }

    [Required(ErrorMessage = "Valid_GeslachtVerplicht")]
    [Display(Name = "Veld_Geslacht")]
    public Gender? Gender { get; set; }

    /// <summary>Together with <see cref="LooksForWomen"/>, determines who appears in the swipe deck (at least one required).</summary>
    [Display(Name = "Veld_ZoektMannen")]
    public bool LooksForMen { get; set; }

    [Display(Name = "Veld_ZoektVrouwen")]
    public bool LooksForWomen { get; set; }
}
