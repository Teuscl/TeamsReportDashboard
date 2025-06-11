namespace TeamsReportDashboard.Backend.Models.ReportDto;

public class CreateReportDto
{
    public int RequesterId { get; set; }
    public string? TechnicianName { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public string ReportedProblem { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public TimeSpan FirstResponseTime { get; set; }
    public TimeSpan AverageHandlingTime { get; set; }
    

}