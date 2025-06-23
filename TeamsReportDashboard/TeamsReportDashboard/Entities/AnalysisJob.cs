using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TeamsReportDashboard.Backend.Entities.Enums;

namespace TeamsReportDashboard.Backend.Entities;

public class AnalysisJob
{
    [Key] public Guid Id { get; set; }
    [Required] public string PythonBatchId { get; set; }
    [Required] public JobStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    [Column(TypeName = "nvarchar(max)")] public string? ResultData { get; set; }
    public string? ErrorMessage { get; set; }
}