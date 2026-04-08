namespace TeamsReportDashboard.Backend.Models.UserDto;

public class ResetPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
    public string NewPasswordConfirm { get; set; } = string.Empty;
}