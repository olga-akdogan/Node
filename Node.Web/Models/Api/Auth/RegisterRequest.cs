using System.ComponentModel.DataAnnotations;
using Node.Data.Models.Enums;

namespace Node.Web.Models.Api.Auth;

/// <summary>
/// API equivalent of <see cref="Node.Web.Models.Account.RegisterViewModel"/>
/// for the MAUI app. Same fields and validation as the web registration form,
/// minus ConfirmPassword: a native app can confirm the password client-side
/// without sending it twice.
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Valid_EmailVerplicht")]
    [EmailAddress(ErrorMessage = "Valid_EmailOngeldig")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valid_WachtwoordVerplicht")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Valid_WachtwoordLengte")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valid_WeergavenaamVerplicht")]
    [MaxLength(80, ErrorMessage = "Valid_WeergavenaamMax")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valid_GeboortedatumVerplicht")]
    public DateOnly? BirthDate { get; set; }

    [Required(ErrorMessage = "Valid_GeboortetijdVerplicht")]
    public TimeOnly? BirthTime { get; set; }

    [Required(ErrorMessage = "Valid_GeboorteplaatsVerplicht")]
    [MaxLength(150, ErrorMessage = "Valid_GeboorteplaatsMax")]
    public string BirthPlace { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Valid_BioMax")]
    public string? Bio { get; set; }

    [Required(ErrorMessage = "Valid_GeslachtVerplicht")]
    public Gender? Gender { get; set; }

    /// <summary>Together with <see cref="LooksForWomen"/>, determines who appears in the swipe deck (at least one required).</summary>
    public bool LooksForMen { get; set; }

    public bool LooksForWomen { get; set; }
}
