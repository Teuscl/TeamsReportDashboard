using System.ComponentModel.DataAnnotations;
using TeamsReportDashboard.Entities.Enums;

namespace TeamsReportDashboard.Entities;

public class User : EntityBase
{
    [Required][MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    [Required][MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    [Required][MaxLength(255)]
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } 
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

}