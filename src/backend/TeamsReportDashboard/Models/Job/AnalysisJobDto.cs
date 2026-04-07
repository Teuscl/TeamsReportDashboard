namespace TeamsReportDashboard.Backend.Models.Job;

public class AnalysisJobDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}