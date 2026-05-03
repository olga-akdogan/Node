using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Node.Data.Models;

public class Swipe
{
    public int Id { get; set; }

    [Required]
    public string SwiperUserId { get; set; } = string.Empty;

    public ApplicationUser? SwiperUser { get; set; }

    [Required]
    public string TargetUserId { get; set; } = string.Empty;

    public ApplicationUser? TargetUser { get; set; }

    public bool IsLike { get; set; }

    public int? CompatibilityScore { get; set; }

    [MaxLength(2000)]
    public string? CompatibilityExplanation { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}