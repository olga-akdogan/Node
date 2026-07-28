using System.ComponentModel.DataAnnotations;

namespace Node.Web.Models.Account;

/// <summary>Formulier om het eigen wachtwoord te wijzigen.</summary>
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Huidig wachtwoord is verplicht.")]
    [DataType(DataType.Password)]
    [Display(Name = "Huidig wachtwoord")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nieuw wachtwoord is verplicht.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Het wachtwoord moet tussen {2} en {1} tekens lang zijn.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nieuw wachtwoord")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Bevestig nieuw wachtwoord")]
    [Compare(nameof(NewPassword), ErrorMessage = "De wachtwoorden komen niet overeen.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
