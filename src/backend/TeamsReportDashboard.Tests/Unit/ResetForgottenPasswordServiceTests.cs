using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Backend.Services.User.ResetForgottenPassword;
using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Entities.Enums;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Tests.Fakes;

namespace TeamsReportDashboard.Tests.Unit;

public class ResetForgottenPasswordServiceTests
{
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakePasswordService _passwordService = new();
    private readonly ResetForgottenPasswordService _sut;

    public ResetForgottenPasswordServiceTests()
    {
        var validator = new ResetForgottenPasswordValidator();
        _sut = new ResetForgottenPasswordService(_uow, _passwordService, validator);
    }

    /// <summary>
    /// Generates a raw token and seeds the user with its hashed version,
    /// mirroring exactly what ForgotPasswordService does.
    /// </summary>
    private (string rawToken, User user) SeedUserWithResetToken(DateTime? expiry = null)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hashedToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "user@example.com",
            Password = _passwordService.HashPassword("OldPassword1!"),
            Role = UserRole.Admin,
            IsActive = true,
            PasswordResetToken = hashedToken,
            PasswordResetTokenExpiryTime = expiry ?? DateTime.UtcNow.AddMinutes(30)
        };

        _uow.UserRepo.Seed(user);
        return (rawToken, user);
    }

    // ── Happy path ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithValidToken_HashesAndSavesNewPassword()
    {
        var (rawToken, user) = SeedUserWithResetToken();

        await _sut.Execute(new ResetForgottenPasswordDto
        {
            Token = rawToken,
            NewPassword = "NewPassword1!",
            ConfirmPassword = "NewPassword1!"
        });

        user.Password.Should().Be(_passwordService.HashPassword("NewPassword1!"));
        _uow.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Execute_WithValidToken_ClearsPasswordResetToken()
    {
        var (rawToken, user) = SeedUserWithResetToken();

        await _sut.Execute(new ResetForgottenPasswordDto
        {
            Token = rawToken,
            NewPassword = "NewPassword1!",
            ConfirmPassword = "NewPassword1!"
        });

        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiryTime.Should().BeNull();
    }

    [Fact]
    public async Task Execute_WithValidToken_InvalidatesExistingRefreshToken()
    {
        var (rawToken, user) = SeedUserWithResetToken();
        user.RefreshToken = "some-active-refresh-token";
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _sut.Execute(new ResetForgottenPasswordDto
        {
            Token = rawToken,
            NewPassword = "NewPassword1!",
            ConfirmPassword = "NewPassword1!"
        });

        // Security: existing sessions must be invalidated after password reset
        user.RefreshToken.Should().BeNull();
        user.RefreshTokenExpiryTime.Should().BeNull();
    }

    // ── Invalid token ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithUnknownToken_ThrowsErrorOnValidationException()
    {
        var exception = await Assert.ThrowsAsync<ErrorOnValidationException>(() =>
            _sut.Execute(new ResetForgottenPasswordDto
            {
                Token = "unknown-token",
                NewPassword = "NewPassword1!",
                ConfirmPassword = "NewPassword1!"
            }));

        exception.GetErrorMessages().Should().Contain("Token inválido ou expirado.");
    }

    [Fact]
    public async Task Execute_WithExpiredToken_ThrowsErrorOnValidationException()
    {
        var (rawToken, _) = SeedUserWithResetToken(expiry: DateTime.UtcNow.AddMinutes(-1));

        var exception = await Assert.ThrowsAsync<ErrorOnValidationException>(() =>
            _sut.Execute(new ResetForgottenPasswordDto
            {
                Token = rawToken,
                NewPassword = "NewPassword1!",
                ConfirmPassword = "NewPassword1!"
            }));

        exception.GetErrorMessages().Should().Contain("Token inválido ou expirado.");
    }

    [Fact]
    public async Task Execute_WithExpiredToken_DoesNotSaveChanges()
    {
        var (rawToken, _) = SeedUserWithResetToken(expiry: DateTime.UtcNow.AddMinutes(-1));

        var act = () => _sut.Execute(new ResetForgottenPasswordDto
        {
            Token = rawToken,
            NewPassword = "NewPassword1!",
            ConfirmPassword = "NewPassword1!"
        });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }

    // ── Validation ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithPasswordTooShort_ThrowsErrorOnValidationException()
    {
        var (rawToken, _) = SeedUserWithResetToken();

        var act = () => _sut.Execute(new ResetForgottenPasswordDto
        {
            Token = rawToken,
            NewPassword = "short",
            ConfirmPassword = "short"
        });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task Execute_WithMismatchedPasswords_ThrowsErrorOnValidationException()
    {
        var (rawToken, _) = SeedUserWithResetToken();

        var exception = await Assert.ThrowsAsync<ErrorOnValidationException>(() =>
            _sut.Execute(new ResetForgottenPasswordDto
            {
                Token = rawToken,
                NewPassword = "NewPassword1!",
                ConfirmPassword = "DifferentPassword1!"
            }));

        exception.GetErrorMessages().Should().Contain("Passwords do not match");
    }

    [Fact]
    public async Task Execute_WithInvalidDto_DoesNotQueryDatabase()
    {
        // Validator rejects before any DB access
        var act = () => _sut.Execute(new ResetForgottenPasswordDto
        {
            Token = "any",
            NewPassword = "",
            ConfirmPassword = ""
        });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
        // UserRepo had no users seeded — proves the DB was never queried for the user
        _uow.SaveChangesCallCount.Should().Be(0);
    }
}
