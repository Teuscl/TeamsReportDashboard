using System.ComponentModel.DataAnnotations;
using TeamsReportDashboard.Entities;

namespace TeamsReportDashboard.Backend.Entities;

public class Department : EntityBase
{
    [Required] [MaxLength(50)] public string Name { get; set; } = string.Empty;
    
    public virtual ICollection<Requester> Requesters { get; set; } = new List<Requester>();

}