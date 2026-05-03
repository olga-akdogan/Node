using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Node.Data.Models;

public class CookieConsentLog
{
    public int Id { get; set; }

    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public bool HasAcceptedCookies { get; set; }

    [MaxLength(100)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}