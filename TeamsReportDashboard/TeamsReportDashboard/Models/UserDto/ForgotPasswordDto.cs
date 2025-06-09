using System.ComponentModel.DataAnnotations;

namespace TeamsReportDashboard.Backend.Models.UserDto;

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}