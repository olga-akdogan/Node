using System.ComponentModel.DataAnnotations;

namespace Node.Web.Models.Account;

/// <summary>Inlogformulier.</summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "E-mailadres is verplicht.")]
    [EmailAddress(ErrorMessage = "Geef een geldig e-mailadres op.")]
    [Display(Name = "E-mailadres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Wachtwoord is verplicht.")]
    [DataType(DataType.Password)]
    [Display(Name = "Wachtwoord")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Aangemeld blijven")]
    public bool RememberMe { get; set; }
}
