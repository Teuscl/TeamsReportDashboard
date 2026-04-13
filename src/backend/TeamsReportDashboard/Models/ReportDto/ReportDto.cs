namespace TeamsReportDashboard.Backend.Models.ReportDto;

public class ReportDto
{
    public Guid Id { get; set; }
    public Guid RequesterId { get; set; }
    public string? RequesterName { get; set; } // Propriedade achatada
    public string? RequesterEmail { get; set; } // Propriedade achatada
    public string? TechnicianName { get; set; }
    public DateTime RequestDate { get; set; }
    public string ReportedProblem { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public TimeSpan FirstResponseTime { get; set; }
    public TimeSpan AverageHandlingTime { get; set; }
}