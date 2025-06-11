using System.ComponentModel.DataAnnotations;
using TeamsReportDashboard.Entities;

namespace TeamsReportDashboard.Backend.Entities;

public class Requester : EntityBase
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
        
    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    // Relação com Departamento
    public int? DepartmentId { get; set; } // Chave estrangeira
    public virtual Department? Department { get; set; } // Propriedade de navegação
}