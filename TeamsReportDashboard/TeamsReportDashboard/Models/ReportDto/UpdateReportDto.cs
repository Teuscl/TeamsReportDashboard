namespace TeamsReportDashboard.Backend.Models.ReportDto;

public class UpdateReportDto 
{
    public string? RequesterName { get; set; }
    public string? RequesterEmail { get; set; }
    public string? TechnicianName { get; set; } 
    public DateTime? RequestDate { get; set; }
    public string? ReportedProblem { get; set; }
    public TimeSpan? FirstResponseTime { get; set; }
    public TimeSpan? AverageHandlingTime { get; set; }
}