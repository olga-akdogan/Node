using System.ComponentModel.DataAnnotations;

namespace Node.Web.Models.Account;

/// <summary>Formulier om het eigen wachtwoord te wijzigen.</summary>
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Valid_HuidigWachtwoordVerplicht")]
    [DataType(DataType.Password)]
    [Display(Name = "Veld_HuidigWachtwoord")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valid_NieuwWachtwoordVerplicht")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Valid_WachtwoordLengte")]
    [DataType(DataType.Password)]
    [Display(Name = "Veld_NieuwWachtwoord")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Veld_BevestigNieuwWachtwoord")]
    [Compare(nameof(NewPassword), ErrorMessage = "Valid_WachtwoordenKomenNietOvereen")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
