using System.ComponentModel.DataAnnotations;

namespace Node.Web.Models.Account;

/// <summary>Inlogformulier.</summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Valid_EmailVerplicht")]
    [EmailAddress(ErrorMessage = "Valid_EmailOngeldig")]
    [Display(Name = "Veld_Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valid_WachtwoordVerplicht")]
    [DataType(DataType.Password)]
    [Display(Name = "Veld_Wachtwoord")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Veld_AangemeldBlijven")]
    public bool RememberMe { get; set; }
}
