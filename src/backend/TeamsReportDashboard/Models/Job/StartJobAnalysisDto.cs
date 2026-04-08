using Microsoft.AspNetCore.Mvc;

namespace TeamsReportDashboard.Backend.Models.Job;

public class StartJobAnalysisDto
{
    [FromForm(Name = "name")]
    public string Name { get; set; }
    
    [FromForm(Name = "file")]
    public IFormFile File { get; set; }
}