using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Node.Data.Models;

public class LanguagePreference
{
    public int Id { get; set; }

    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    [Required]
    [MaxLength(10)]
    public string CultureCode { get; set; } = "en";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}