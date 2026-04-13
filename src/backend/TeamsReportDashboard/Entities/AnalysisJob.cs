// TeamsReportDashboard.Backend/Entities/AnalysisJob.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TeamsReportDashboard.Backend.Entities.Enums;
using TeamsReportDashboard.Entities;

namespace TeamsReportDashboard.Backend.Entities;

public class AnalysisJob
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required] public string PythonBatchId { get; set; } = string.Empty;
    [Required] public JobStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? ResultData { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }

    public uint RowVersion { get; set; }

    // ─── Relacionamento com o usuário que criou o job ───
    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
}