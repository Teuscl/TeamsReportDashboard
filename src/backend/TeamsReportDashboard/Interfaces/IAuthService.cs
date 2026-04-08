using TeamsReportDashboard.Models.Auth;

namespace TeamsReportDashboard.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest loginRequest);
}