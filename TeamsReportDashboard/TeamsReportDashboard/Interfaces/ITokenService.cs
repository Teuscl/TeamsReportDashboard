using TeamsReportDashboard.Entities;

namespace TeamsReportDashboard.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
    public string GenerateRefreshToken();
}