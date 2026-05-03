using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Node.Data.Models;

public class ChatMessage
{
    public int Id { get; set; }

    public int MatchId { get; set; }

    public Match? Match { get; set; }

    [Required]
    public string SenderUserId { get; set; } = string.Empty;

    public ApplicationUser? SenderUser { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; }
}