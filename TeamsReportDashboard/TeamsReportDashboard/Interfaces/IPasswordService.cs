namespace TeamsReportDashboard.Interfaces;

public interface IPasswordService
{
    public bool VerifyPassword(string enteredPassword, string storedPassword);
    public string HashPassword(string password);
}