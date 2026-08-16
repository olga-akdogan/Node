using System.ComponentModel.DataAnnotations;

namespace Node.Web.Models.Api.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Valid_EmailRequired")]
    [EmailAddress(ErrorMessage = "Valid_EmailInvalid")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valid_PasswordRequired")]
    public string Password { get; set; } = string.Empty;
}
