using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Node.Data.Models;

public class BlockedUser
{
    public int Id { get; set; }

    [Required]
    public string BlockedUserId { get; set; } = string.Empty;

    public ApplicationUser? BlockedUserAccount { get; set; }

    [Required]
    public string BlockedByAdminId { get; set; } = string.Empty;

    public ApplicationUser? BlockedByAdmin { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }

    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}