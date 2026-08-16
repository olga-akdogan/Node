using System.ComponentModel.DataAnnotations;

namespace Node.Web.Models.Api.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Valid_EmailVerplicht")]
    [EmailAddress(ErrorMessage = "Valid_EmailOngeldig")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valid_WachtwoordVerplicht")]
    public string Password { get; set; } = string.Empty;
}
