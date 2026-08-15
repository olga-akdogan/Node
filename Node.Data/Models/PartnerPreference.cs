using System.ComponentModel.DataAnnotations;
using Node.Data.Models.Enums;

namespace Node.Data.Models;

/// <summary>
/// One gender a user is interested in (multiple rows per user are possible,
/// e.g. both Male and Female checked). The swipe deck only shows candidates
/// that satisfy the mutual preference: my preference contains the
/// candidate's gender, and the candidate's preference contains my gender.
/// </summary>
public class PartnerPreference
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    [Required]
    public Gender Gender { get; set; }
}
