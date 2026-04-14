using FluentAssertions;
using TeamsReportDashboard.Backend.Services.User.ChangeMyPassword;
using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Entities.Enums;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Models.Dto;
using TeamsReportDashboard.Tests.Fakes;

namespace TeamsReportDashboard.Tests.Unit;

public class ChangeMyPasswordServiceTests
{
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakePasswordService _passwordService = new();
    private readonly ChangeMyPasswordService _sut;

    public ChangeMyPasswordServiceTests()
    {
        var validator = new ChangeMyPasswordValidator();
        _sut = new ChangeMyPasswordService(_uow, _passwordService, validator);
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

        await _sut.Execute(user.Id, new ChangeMyPasswordDto
        {
            OldPassword = "OldPassword123!",
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

        await _sut.Execute(user.Id, new ChangeMyPasswordDto
        {
            OldPassword = "OldPassword123!",
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

        await _sut.Execute(user.Id, new ChangeMyPasswordDto
        {
            OldPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            NewPasswordConfirm = "NewPassword123!"
        });

        _uow.UserRepo.UpdateCallCount.Should().Be(1);
    }

    // ── Wrong old password ──────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithWrongOldPassword_ThrowsWithCorrectMessage()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        var exception = await Assert.ThrowsAsync<ErrorOnValidationException>(() =>
            _sut.Execute(user.Id, new ChangeMyPasswordDto
            {
                OldPassword = "WrongPassword!1",
                NewPassword = "NewPassword123!",
                NewPasswordConfirm = "NewPassword123!"
            }));

        exception.GetErrorMessages().Should().Contain("Old password is incorrect.");
    }

    [Fact]
    public async Task Execute_WithWrongOldPassword_DoesNotSaveChanges()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        var act = () => _sut.Execute(user.Id, new ChangeMyPasswordDto
        {
            OldPassword = "WrongPassword!1",
            NewPassword = "NewPassword123!",
            NewPasswordConfirm = "NewPassword123!"
        });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }

    // ── User not found ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithUnknownUserId_ThrowsArgumentException()
    {
        var act = () => _sut.Execute(Guid.NewGuid(), new ChangeMyPasswordDto
        {
            OldPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            NewPasswordConfirm = "NewPassword123!"
        });

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("User not found");
    }

    [Fact]
    public async Task Execute_WithUnknownUserId_DoesNotSaveChanges()
    {
        var act = () => _sut.Execute(Guid.NewGuid(), new ChangeMyPasswordDto
        {
            OldPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            NewPasswordConfirm = "NewPassword123!"
        });

        await act.Should().ThrowAsync<ArgumentException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }

    // ── DTO validation ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithEmptyOldPassword_ThrowsErrorOnValidationException()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        var act = () => _sut.Execute(user.Id, new ChangeMyPasswordDto
        {
            OldPassword = string.Empty,
            NewPassword = "NewPassword123!",
            NewPasswordConfirm = "NewPassword123!"
        });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task Execute_WithNewPasswordShorterThan8Chars_ThrowsErrorOnValidationException()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        var act = () => _sut.Execute(user.Id, new ChangeMyPasswordDto
        {
            OldPassword = "OldPassword123!",
            NewPassword = "Short1!",
            NewPasswordConfirm = "Short1!"
        });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task Execute_WithMismatchedNewPasswords_ThrowsWithCorrectMessage()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        var exception = await Assert.ThrowsAsync<ErrorOnValidationException>(() =>
            _sut.Execute(user.Id, new ChangeMyPasswordDto
            {
                OldPassword = "OldPassword123!",
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

        var act = () => _sut.Execute(user.Id, new ChangeMyPasswordDto
        {
            OldPassword = string.Empty,
            NewPassword = "NewPassword123!",
            NewPasswordConfirm = "NewPassword123!"
        });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }
}
