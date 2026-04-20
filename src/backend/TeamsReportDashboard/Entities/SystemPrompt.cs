using System.ComponentModel.DataAnnotations.Schema;
using TeamsReportDashboard.Entities;

namespace TeamsReportDashboard.Backend.Entities;

public class SystemPrompt : EntityBase
{
    public string Content { get; set; } = string.Empty;
    public Guid? CreatedByUserId { get; set; }
    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }
}