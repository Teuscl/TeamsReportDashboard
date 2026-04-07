using System.ComponentModel.DataAnnotations;

namespace TeamsReportDashboard.Backend.Models.Requester;

public class UpdateRequesterDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
        
    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }
}