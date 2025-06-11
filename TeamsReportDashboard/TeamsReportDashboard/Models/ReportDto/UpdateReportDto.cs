namespace TeamsReportDashboard.Backend.Models.ReportDto;

public class UpdateReportDto 
{
    public int RequesterId { get; set; }
    public string? TechnicianName { get; set; } 
    public DateTime? RequestDate { get; set; }
    public string? ReportedProblem { get; set; }
    public string? Category { get; set; } = string.Empty;
    public TimeSpan? FirstResponseTime { get; set; }
    public TimeSpan? AverageHandlingTime { get; set; }
}