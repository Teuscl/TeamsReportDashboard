using TeamsReportDashboard.Interfaces;
using BCrypt.Net;

namespace TeamsReportDashboard.Services;

public class PasswordService : IPasswordService
{
    public bool VerifyPassword(string enteredPassword, string storedPassword) => BCrypt.Net.BCrypt.Verify(enteredPassword, storedPassword);
    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    
}