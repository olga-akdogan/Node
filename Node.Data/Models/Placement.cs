using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Node.Data.Models.Enums;

namespace Node.Data.Models;

/// <summary>
/// Eén positie binnen een geboortehoroscoop: welk hemellichaam staat in welk
/// teken, in welk huis en op welke graad. Per horoscoop komt elk hemellichaam
/// maximaal één keer voor (unieke index in de DbContext).
/// </summary>
public class Placement
{
    public int Id { get; set; }

    /// <summary>De horoscoop waartoe deze positie behoort.</summary>
    public int NatalChartId { get; set; }

    public NatalChart? NatalChart { get; set; }

    /// <summary>Het hemellichaam of punt (Zon, Maan, ..., Ascendant).</summary>
    [Required]
    public CelestialBody Body { get; set; }

    /// <summary>Het dierenriemteken waarin het hemellichaam staat.</summary>
    [Required]
    public ZodiacSign Sign { get; set; }

    /// <summary>Astrologisch huis (1 t.e.m. 12).</summary>
    [Range(1, 12)]
    public int House { get; set; }

    /// <summary>Positie binnen het teken, in graden (0 t.e.m. 29,99...).</summary>
    [Range(0, 30)]
    [Column(TypeName = "decimal(5,2)")]
    public decimal DegreeInSign { get; set; }
}
