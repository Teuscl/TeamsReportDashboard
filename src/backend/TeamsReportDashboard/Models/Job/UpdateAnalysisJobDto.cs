using System.ComponentModel.DataAnnotations;

namespace TeamsReportDashboard.Backend.Models.Job;

public class UpdateAnalysisJobDto
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; }
}