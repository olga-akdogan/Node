using System.ComponentModel.DataAnnotations;

namespace Node.Web.Models.Account;

/// Registration form
public class LoginViewModel
{
    [Required(ErrorMessage = "Valid_EmailRequired")]
    [EmailAddress(ErrorMessage = "Valid_EmailInvalid")]
    [Display(Name = "Field_Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valid_PasswordRequired")]
    [DataType(DataType.Password)]
    [Display(Name = "Field_Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Field_StayLoggedIn")]
    public bool RememberMe { get; set; }
}
