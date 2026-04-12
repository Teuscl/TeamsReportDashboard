namespace TeamsReportDashboard.Tests.Fakes;

public class FakeEmailService : IEmailService
{
    public record SentEmail(string ToEmail, string UserName, string ResetLink);

    public List<SentEmail> SentEmails { get; } = [];

    public Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink)
    {
        SentEmails.Add(new SentEmail(toEmail, userName, resetLink));
        return Task.CompletedTask;
    }
}
