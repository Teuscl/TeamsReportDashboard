using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Tests.Fakes;

public class FakeTokenService : ITokenService
{
    public string LastGeneratedToken { get; private set; } = string.Empty;
    public string LastGeneratedRefreshToken { get; private set; } = string.Empty;
    public int GenerateTokenCallCount { get; private set; }

    public string GenerateToken(User user)
    {
        GenerateTokenCallCount++;
        LastGeneratedToken = $"access-token-for-user-{user.Id}";
        return LastGeneratedToken;
    }

    public string GenerateRefreshToken()
    {
        LastGeneratedRefreshToken = $"refresh-{Guid.NewGuid():N}";
        return LastGeneratedRefreshToken;
    }
}
