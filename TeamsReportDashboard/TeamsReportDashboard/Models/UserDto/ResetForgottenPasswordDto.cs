using System.ComponentModel.DataAnnotations;

namespace TeamsReportDashboard.Backend.Models.UserDto;

public class ResetForgottenPasswordDto
{
    [Required]
    public string Token { get; set; }

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; }

    [Required]
    [Compare("NewPassword", ErrorMessage = "As senhas não coincidem.")]
    public string ConfirmPassword { get; set; }
}