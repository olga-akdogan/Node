using System.ComponentModel.DataAnnotations;

namespace Node.Web.Models.Api.Reports;

public class CreateReportRequest
{
    [Required]
    public string ReportedUserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Valid_RedenVerplicht")]
    [MaxLength(1000, ErrorMessage = "Valid_RedenMax")]
    public string Reason { get; set; } = string.Empty;
}
