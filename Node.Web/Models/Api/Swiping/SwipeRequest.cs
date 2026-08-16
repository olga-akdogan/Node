using System.ComponentModel.DataAnnotations;

namespace Node.Web.Models.Api.Swiping;

public class SwipeRequest
{
    [Required]
    public string TargetUserId { get; set; } = string.Empty;

    public bool IsLike { get; set; }
}
