namespace TeamsReportDashboard.Backend.Interfaces;

public interface IPromptService
{
    Task<string> GetPromptAsync();
    Task UpdatePromptAsync(string prompt);
}
