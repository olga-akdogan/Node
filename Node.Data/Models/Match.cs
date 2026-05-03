using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Node.Data.Models;

public class Match
{
    public int Id { get; set; }

    [Required]
    public string User1Id { get; set; } = string.Empty;

    public ApplicationUser? User1 { get; set; }

    [Required]
    public string User2Id { get; set; } = string.Empty;

    public ApplicationUser? User2 { get; set; }

    public DateTime MatchedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}