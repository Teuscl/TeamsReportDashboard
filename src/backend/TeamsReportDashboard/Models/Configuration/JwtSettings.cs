using System.ComponentModel.DataAnnotations;

namespace TeamsReportDashboard.Backend.Models.Configuration;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required(ErrorMessage = "Jwt:Key is required. Add it to User Secrets.")]
    [MinLength(32, ErrorMessage = "Jwt:Key must be at least 32 characters.")]
    public string Key { get; set; } = string.Empty;
}
