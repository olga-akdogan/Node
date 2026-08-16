using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Node.Data.Models.Enums;

namespace Node.Data.Models;

/// <summary>
/// One position within a natal chart: which celestial body sits in which
/// sign, in which house, and at what degree. Each celestial body occurs at
/// most once per chart (unique index in the DbContext).
/// </summary>
public class Placement
{
    public int Id { get; set; }

    /// <summary>The natal chart this position belongs to.</summary>
    public int NatalChartId { get; set; }

    public NatalChart? NatalChart { get; set; }

    /// <summary>The celestial body or point (Sun, Moon, ..., Ascendant).</summary>
    [Required]
    public CelestialBody Body { get; set; }

    /// <summary>The zodiac sign the celestial body is in.</summary>
    [Required]
    public ZodiacSign Sign { get; set; }

    /// <summary>Astrological house (1 through 12).</summary>
    [Range(1, 12)]
    public int House { get; set; }

    /// <summary>Position within the sign, in degrees (0 through 29.99...).</summary>
    [Range(0, 30)]
    [Column(TypeName = "decimal(5,2)")]
    public decimal DegreeInSign { get; set; }
}
