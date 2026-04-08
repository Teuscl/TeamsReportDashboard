// TeamsReportDashboard.Backend/Entities/AnalysisJob.cs

using System.ComponentModel.DataAnnotations;
using TeamsReportDashboard.Backend.Entities.Enums;

namespace TeamsReportDashboard.Backend.Entities;

public class AnalysisJob
{
    [Key] public Guid Id { get; set; }

    // ✨ NOVO CAMPO
    [Required] 
    [MaxLength(100)] // Boa prática para definir um limite de tamanho
    public string Name { get; set; } 

    [Required] public string PythonBatchId { get; set; }
    [Required] public JobStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public string? ResultData { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }

    public uint RowVersion { get; set; }
    
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

}