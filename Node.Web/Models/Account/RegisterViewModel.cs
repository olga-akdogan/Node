using System.ComponentModel.DataAnnotations;

namespace Node.Web.Models.Account;

/// <summary>
/// Registratieformulier met de extra profielvelden van ApplicationUser.
/// De teksten worden in de meertaligheidsfase naar resource-bestanden verplaatst.
/// </summary>
public class RegisterViewModel
{
    [Required(ErrorMessage = "E-mailadres is verplicht.")]
    [EmailAddress(ErrorMessage = "Geef een geldig e-mailadres op.")]
    [Display(Name = "E-mailadres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Wachtwoord is verplicht.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Het wachtwoord moet tussen {2} en {1} tekens lang zijn.")]
    [DataType(DataType.Password)]
    [Display(Name = "Wachtwoord")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Bevestig wachtwoord")]
    [Compare(nameof(Password), ErrorMessage = "De wachtwoorden komen niet overeen.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Weergavenaam is verplicht.")]
    [MaxLength(80, ErrorMessage = "De weergavenaam mag maximaal {1} tekens lang zijn.")]
    [Display(Name = "Weergavenaam")]
    public string DisplayName { get; set; } = string.Empty;

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

    [MaxLength(1000, ErrorMessage = "De bio mag maximaal {1} tekens lang zijn.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Bio (optioneel)")]
    public string? Bio { get; set; }
}
