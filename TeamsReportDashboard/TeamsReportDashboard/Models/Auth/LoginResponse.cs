namespace TeamsReportDashboard.Models.Auth;

public class LoginResponse
{
    public int Id { get; set; }
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }
}