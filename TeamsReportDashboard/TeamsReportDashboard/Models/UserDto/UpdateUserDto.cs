using TeamsReportDashboard.Entities.Enums;

namespace TeamsReportDashboard.Models.Dto;

public class UpdateUserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}