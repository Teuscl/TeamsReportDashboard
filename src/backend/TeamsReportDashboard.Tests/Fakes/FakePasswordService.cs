using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Tests.Fakes;

/// <summary>
/// Deterministic password service that avoids BCrypt's intentional slowness in tests.
/// Hash format: "hashed:{plain-text}"
/// </summary>
public class FakePasswordService : IPasswordService
{
    public string HashPassword(string password) => $"hashed:{password}";

    public bool VerifyPassword(string enteredPassword, string storedPassword) =>
        storedPassword == $"hashed:{enteredPassword}";
}
