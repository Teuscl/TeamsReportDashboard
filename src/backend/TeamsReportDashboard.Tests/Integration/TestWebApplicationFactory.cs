using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Tests.Fakes;

namespace TeamsReportDashboard.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    public FakeEmailService EmailService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // JWT — must be ≥ 32 chars to pass JwtSettings validation
                ["Jwt:Key"] = "test-super-secret-key-exactly-32chars!",

                // Python service — required by Program.cs guard
                ["PythonApi:BaseUrl"] = "http://localhost:8001/",

                // Email — required by EmailSettings ValidateOnStart
                ["EmailSettings:SmtpServer"] = "localhost",
                ["EmailSettings:Port"] = "587",
                ["EmailSettings:SenderName"] = "Test Sender",
                ["EmailSettings:SenderEmail"] = "test@test.com",
                ["EmailSettings:Username"] = "test",
                ["EmailSettings:Password"] = "test",

                // Frontend URL for password reset links
                ["FrontendUrl"] = "http://localhost:60414",
            });
        });

        builder.ConfigureServices(services =>
        {
            // EF Core 9 stores per-context configure actions as IDbContextOptionsConfiguration<TContext>.
            // We must remove ALL of them before adding InMemory, otherwise BOTH Npgsql and InMemory
            // get applied to the same DbContextOptions, which causes an "only one provider allowed" error.
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Replace email service with fake (captures sent emails in-memory)
            services.RemoveAll(typeof(IEmailService));
            services.AddTransient<IEmailService>(_ => EmailService);
        });
    }

    /// <summary>Seeds a user directly into the in-memory database.</summary>
    public async Task SeedUserAsync(TeamsReportDashboard.Entities.User user)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
}
