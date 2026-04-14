using FluentAssertions;
using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Backend.Services.User.ResetPassword;
using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Entities.Enums;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Tests.Fakes;

namespace TeamsReportDashboard.Tests.Unit;

public class ResetPasswordServiceTests
{
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakePasswordService _passwordService = new();
    private readonly ResetPasswordService _sut;

    public ResetPasswordServiceTests()
    {
        var validator = new ResetPasswordValidator();
        _sut = new ResetPasswordService(_uow, _passwordService, validator);
    }

    private User BuildActiveUser(string password = "OldPassword123!") => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test User",
        Email = "user@example.com",
        Password = _passwordService.HashPassword(password),
        Role = UserRole.Admin,
        IsActive = true
    };

    // ── Happy path ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithValidData_HashesAndSavesNewPassword()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        await _sut.Execute(user.Id, new ResetPasswordDto
        {
            NewPassword = "NewPassword123!",
            NewPasswordConfirm = "NewPassword123!"
        });

        user.Password.Should().Be(_passwordService.HashPassword("NewPassword123!"));
    }

    [Fact]
    public async Task Execute_WithValidData_CallsSaveChangesOnce()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        await _sut.Execute(user.Id, new ResetPasswordDto
        {
            NewPassword = "NewPassword123!",
            NewPasswordConfirm = "NewPassword123!"
        });

        _uow.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Execute_WithValidData_CallsUpdateOnRepository()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        await _sut.Execute(user.Id, new ResetPasswordDto
        {
            NewPassword = "NewPassword123!",
            NewPasswordConfirm = "NewPassword123!"
        });

        _uow.UserRepo.UpdateCallCount.Should().Be(1);
    }

    // ── User not found ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithUnknownUserId_ThrowsArgumentException()
    {
        var act = () => _sut.Execute(Guid.NewGuid(), new ResetPasswordDto
        {
            NewPassword = "NewPassword123!",
            NewPasswordConfirm = "NewPassword123!"
        });

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("User not found");
    }

    [Fact]
    public async Task Execute_WithUnknownUserId_DoesNotSaveChanges()
    {
        var act = () => _sut.Execute(Guid.NewGuid(), new ResetPasswordDto
        {
            NewPassword = "NewPassword123!",
            NewPasswordConfirm = "NewPassword123!"
        });

        await act.Should().ThrowAsync<ArgumentException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }

    // ── Validation ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithPasswordShorterThan8Chars_ThrowsErrorOnValidationException()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        var act = () => _sut.Execute(user.Id, new ResetPasswordDto
        {
            NewPassword = "Short1!",
            NewPasswordConfirm = "Short1!"
        });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task Execute_WithMismatchedPasswords_ThrowsWithCorrectMessage()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        var exception = await Assert.ThrowsAsync<ErrorOnValidationException>(() =>
            _sut.Execute(user.Id, new ResetPasswordDto
            {
                NewPassword = "NewPassword123!",
                NewPasswordConfirm = "DifferentPassword123!"
            }));

        exception.GetErrorMessages().Should().Contain("Passwords do not match");
    }

    [Fact]
    public async Task Execute_WithValidationError_DoesNotSaveChanges()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        var act = () => _sut.Execute(user.Id, new ResetPasswordDto
        {
            NewPassword = "Short1!",
            NewPasswordConfirm = "Short1!"
        });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Execute_WithEmptyPassword_ThrowsErrorOnValidationException()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        var act = () => _sut.Execute(user.Id, new ResetPasswordDto
        {
            NewPassword = string.Empty,
            NewPasswordConfirm = string.Empty
        });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }
}
