using System.ComponentModel.DataAnnotations;

namespace Node.Web.Models.Api.Reports;

public class CreateReportRequest
{
    [Required]
    public string ReportedUserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valid_ReasonRequired")]
    [MaxLength(1000, ErrorMessage = "Valid_ReasonMax")]
    public string Reason { get; set; } = string.Empty;
}
