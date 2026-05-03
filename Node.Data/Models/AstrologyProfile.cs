using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Node.Data.Models;

public class AstrologyProfile
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    [MaxLength(50)]
    public string? SunSign { get; set; }

    [MaxLength(50)]
    public string? MoonSign { get; set; }

    [MaxLength(50)]
    public string? RisingSign { get; set; }

    public string? PlanetaryDataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}