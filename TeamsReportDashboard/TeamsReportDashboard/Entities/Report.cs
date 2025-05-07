using System.ComponentModel.DataAnnotations;

namespace TeamsReportDashboard.Entities;

public class Report : EntityBase
{
    [Required][MaxLength(55)]
    public string RequesterName { get; set; } = string.Empty;
    [Required][MaxLength(100)]
    public string RequesterEmail { get; set; } = string.Empty;
    [Required][MaxLength(50)]
    public string TechnicianName { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    [Required][MaxLength(255)]
    public string ReportedProblem { get; set; } = string.Empty;
    
    //Indicates the time that the IT Support took to respond the first message
    public TimeSpan FirstResponseTime { get; set; }
    //Indicates the average time of the ticket support
    public TimeSpan AverageHandlingTime { get; set; }
}