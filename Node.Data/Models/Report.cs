using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Node.Data.Models;

public class Report
{
    public int Id { get; set; }

    [Required]
    public string ReporterUserId { get; set; } = string.Empty;

    public ApplicationUser? ReporterUser { get; set; }

    [Required]
    public string ReportedUserId { get; set; } = string.Empty;

    public ApplicationUser? ReportedUser { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    public bool IsResolved { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}