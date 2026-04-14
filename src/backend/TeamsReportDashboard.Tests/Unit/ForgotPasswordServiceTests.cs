using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Backend.Services.User.ForgotPassword;
using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Entities.Enums;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Tests.Fakes;

namespace TeamsReportDashboard.Tests.Unit;

public class ForgotPasswordServiceTests
{
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakeEmailService _emailService = new();
    private readonly IConfiguration _configuration;
    private readonly ForgotPasswordService _sut;

    public ForgotPasswordServiceTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FrontendUrl"] = "http://localhost:60414" })
            .Build();

        var validator = new ForgotPasswordValidator();
        _sut = new ForgotPasswordService(_uow, _emailService, _configuration, validator);
    }

    private User BuildActiveUser(string email = "user@example.com") => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test User",
        Email = email,
        Password = "hashed:password",
        Role = UserRole.Admin,
        IsActive = true
    };

    // ── Happy path ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithRegisteredEmail_SendsOneEmail()
    {
        _uow.UserRepo.Seed(BuildActiveUser());

        await _sut.Execute(new ForgotPasswordDto { Email = "user@example.com" });

        _emailService.SentEmails.Should().HaveCount(1);
    }

    [Fact]
    public async Task Execute_WithRegisteredEmail_SendsToCorrectAddress()
    {
        _uow.UserRepo.Seed(BuildActiveUser("target@example.com"));

        await _sut.Execute(new ForgotPasswordDto { Email = "target@example.com" });

        _emailService.SentEmails[0].ToEmail.Should().Be("target@example.com");
    }

    [Fact]
    public async Task Execute_WithRegisteredEmail_ResetLinkContainsFrontendUrl()
    {
        _uow.UserRepo.Seed(BuildActiveUser());

        await _sut.Execute(new ForgotPasswordDto { Email = "user@example.com" });

        _emailService.SentEmails[0].ResetLink.Should().StartWith("http://localhost:60414");
    }

    [Fact]
    public async Task Execute_WithRegisteredEmail_ResetLinkContainsResetPasswordPath()
    {
        _uow.UserRepo.Seed(BuildActiveUser());

        await _sut.Execute(new ForgotPasswordDto { Email = "user@example.com" });

        _emailService.SentEmails[0].ResetLink.Should().Contain("/reset-password");
    }

    [Fact]
    public async Task Execute_WithRegisteredEmail_StoresHashedTokenNotRawToken()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        await _sut.Execute(new ForgotPasswordDto { Email = "user@example.com" });

        // The link contains the raw token; the DB stores the hash — they must differ
        var rawTokenInLink = ExtractTokenFromLink(_emailService.SentEmails[0].ResetLink);
        user.PasswordResetToken.Should().NotBe(rawTokenInLink, "DB must store the hash, not the plain token");
        user.PasswordResetToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Execute_WithRegisteredEmail_SetsTokenExpiry30Minutes()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        await _sut.Execute(new ForgotPasswordDto { Email = "user@example.com" });

        user.PasswordResetTokenExpiryTime.Should()
            .BeCloseTo(DateTime.UtcNow.AddMinutes(30), precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Execute_WithRegisteredEmail_SavesChanges()
    {
        _uow.UserRepo.Seed(BuildActiveUser());

        await _sut.Execute(new ForgotPasswordDto { Email = "user@example.com" });

        _uow.SaveChangesCallCount.Should().Be(1);
    }

    // ── Security: user enumeration prevention ─────────────────────────────────

    [Fact]
    public async Task Execute_WithUnregisteredEmail_DoesNotThrow()
    {
        var act = () => _sut.Execute(new ForgotPasswordDto { Email = "nobody@example.com" });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Execute_WithUnregisteredEmail_DoesNotSendEmail()
    {
        await _sut.Execute(new ForgotPasswordDto { Email = "nobody@example.com" });

        _emailService.SentEmails.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_WithUnregisteredEmail_DoesNotSaveChanges()
    {
        await _sut.Execute(new ForgotPasswordDto { Email = "nobody@example.com" });

        _uow.SaveChangesCallCount.Should().Be(0);
    }

    // ── Validation ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithInvalidEmail_ThrowsErrorOnValidationException()
    {
        var act = () => _sut.Execute(new ForgotPasswordDto { Email = "not-an-email" });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task Execute_WithEmptyEmail_ThrowsErrorOnValidationException()
    {
        var act = () => _sut.Execute(new ForgotPasswordDto { Email = "" });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    private static string ExtractTokenFromLink(string link)
    {
        var uri = new Uri(link);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query["token"] ?? string.Empty;
    }
}
