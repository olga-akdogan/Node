using System.ComponentModel.DataAnnotations;

namespace Node.Web.Models.Account;

/// <summary>Form to change your own password.</summary>
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Valid_CurrentPasswordRequired")]
    [DataType(DataType.Password)]
    [Display(Name = "Field_CurrentPassword")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valid_NewPasswordRequired")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Valid_PasswordLength")]
    [DataType(DataType.Password)]
    [Display(Name = "Field_NewPassword")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Field_ConfirmNewPassword")]
    [Compare(nameof(NewPassword), ErrorMessage = "Valid_PasswordsDoNotMatch")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
