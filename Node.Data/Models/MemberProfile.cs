using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Node.Data.Models;

public class MemberProfile
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    [Required]
    [MaxLength(80)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Bio { get; set; }

    public string? ProfilePictureUrl { get; set; }

    [Required]
    public DateOnly BirthDate { get; set; }

    [Required]
    public TimeOnly BirthTime { get; set; }

    [Required]
    [MaxLength(150)]
    public string BirthLocation { get; set; } = string.Empty;

    [Column(TypeName = "decimal(9,6)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    public decimal? Longitude { get; set; }

    public bool IsBlocked { get; set; }

    public bool HasAcceptedBehaviorConduct { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public AstrologyProfile? AstrologyProfile { get; set; }
}