namespace TeamsReportDashboard.Models.Dto;

public class CreateReportDto
{
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;
    public string? TechnicianName { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public string ReportedProblem { get; set; } = string.Empty;
    public TimeSpan FirstResponseTime { get; set; }
    public TimeSpan AverageHandlingTime { get; set; }

}