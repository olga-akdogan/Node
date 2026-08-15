using System.ComponentModel.DataAnnotations;

namespace Node.Web.Models.Account;

/// <summary>
/// Instellingenformulier waarmee de gebruiker de eigen profielvelden aanpast
/// (gebruikersparametrisatie met de extra eigenschappen).
/// </summary>
public class ManageProfileViewModel
{
    [Required(ErrorMessage = "Weergavenaam is verplicht.")]
    [MaxLength(80, ErrorMessage = "De weergavenaam mag maximaal {1} tekens lang zijn.")]
    [Display(Name = "Weergavenaam")]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "De bio mag maximaal {1} tekens lang zijn.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Bio")]
    public string? Bio { get; set; }

    [Required(ErrorMessage = "Geboortedatum is verplicht.")]
    [DataType(DataType.Date)]
    [Display(Name = "Geboortedatum")]
    public DateOnly? BirthDate { get; set; }

    [Required(ErrorMessage = "Geboortetijd is verplicht voor een correcte horoscoop.")]
    [DataType(DataType.Time)]
    [Display(Name = "Geboortetijd")]
    public TimeOnly? BirthTime { get; set; }

    [Required(ErrorMessage = "Geboorteplaats is verplicht.")]
    [MaxLength(150, ErrorMessage = "De geboorteplaats mag maximaal {1} tekens lang zijn.")]
    [Display(Name = "Geboorteplaats")]
    public string BirthPlace { get; set; } = string.Empty;

    /// <summary>Enkel ingevuld wanneer de gebruiker een nieuwe profielfoto kiest.</summary>
    [Display(Name = "Profielfoto")]
    public IFormFile? ProfilePicture { get; set; }

    /// <summary>Huidige foto-URL, enkel voor weergave (niet uit het formulier).</summary>
    public string? HuidigeProfielFotoUrl { get; set; }
}
