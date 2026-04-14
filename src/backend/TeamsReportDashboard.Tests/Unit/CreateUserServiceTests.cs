using FluentAssertions;
using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Entities.Enums;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Services.User.Create;
using TeamsReportDashboard.Tests.Fakes;

namespace TeamsReportDashboard.Tests.Unit;

public class CreateUserServiceTests
{
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakePasswordService _passwordService = new();
    private readonly CreateUserService _sut;

    public CreateUserServiceTests()
    {
        var validator = new CreateUserValidator();
        _sut = new CreateUserService(_uow, validator, _passwordService);
    }

    private CreateUserDto BuildValidDto(
        string name = "Test User",
        string email = "user@example.com",
        string password = "Password123!") =>
        new() { Name = name, Email = email, Password = password, Role = UserRole.Viewer };

    // ── Happy path ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithValidDto_AddsUserToRepository()
    {
        var dto = BuildValidDto();

        await _sut.Execute(dto);

        var users = await _uow.UserRepo.GetAllAsync();
        users.Should().HaveCount(1);
        users.First().Email.Should().Be(dto.Email);
        users.First().Name.Should().Be(dto.Name);
        users.First().Role.Should().Be(dto.Role);
    }

    [Fact]
    public async Task Execute_WithValidDto_HashesPassword()
    {
        var dto = BuildValidDto(password: "Password123!");

        await _sut.Execute(dto);

        var users = await _uow.UserRepo.GetAllAsync();
        users.First().Password.Should().Be(_passwordService.HashPassword("Password123!"));
        users.First().Password.Should().NotBe("Password123!");
    }

    [Fact]
    public async Task Execute_WithValidDto_CallsSaveChanges()
    {
        await _sut.Execute(BuildValidDto());

        _uow.SaveChangesCallCount.Should().Be(1);
    }

    // ── Email uniqueness ────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithDuplicateEmail_ThrowsErrorOnValidationException()
    {
        _uow.UserRepo.Seed(new User
        {
            Id = Guid.NewGuid(),
            Name = "Existing User",
            Email = "user@example.com",
            Password = "some-hash",
            IsActive = true
        });

        var exception = await Assert.ThrowsAsync<ErrorOnValidationException>(() =>
            _sut.Execute(BuildValidDto(email: "user@example.com")));

        exception.GetErrorMessages().Should().Contain("Email is already taken");
    }

    [Fact]
    public async Task Execute_WithDuplicateEmail_DoesNotSaveChanges()
    {
        _uow.UserRepo.Seed(new User
        {
            Id = Guid.NewGuid(),
            Name = "Existing User",
            Email = "user@example.com",
            Password = "some-hash",
            IsActive = true
        });

        var act = () => _sut.Execute(BuildValidDto(email: "user@example.com"));

        await act.Should().ThrowAsync<ErrorOnValidationException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }

    // ── Validation ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithEmptyName_ThrowsErrorOnValidationException()
    {
        var act = () => _sut.Execute(BuildValidDto(name: string.Empty));

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task Execute_WithInvalidEmail_ThrowsErrorOnValidationException()
    {
        var act = () => _sut.Execute(BuildValidDto(email: "not-a-valid-email"));

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task Execute_WithEmailLongerThan100Chars_ThrowsErrorOnValidationException()
    {
        var email = new string('a', 90) + "@example.com"; // 102 chars total

        var act = () => _sut.Execute(BuildValidDto(email: email));

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    // Regression: MaxLength was incorrectly set to 50; corrected to 100.
    // An email between 51-100 chars must be accepted.
    [Fact]
    public async Task Execute_WithEmail60Chars_DoesNotThrow()
    {
        var email = new string('a', 48) + "@example.com"; // 60 chars — above old 50-char limit

        var act = () => _sut.Execute(BuildValidDto(email: email));

        await act.Should().NotThrowAsync();
    }

    // Regression: Error message said "6 characters"; corrected to "8 characters".
    [Fact]
    public async Task Execute_WithPasswordShorterThan8Chars_ThrowsWithCorrectMessage()
    {
        var act = () => _sut.Execute(BuildValidDto(password: "Abc123!")); // 7 chars

        var ex = await act.Should().ThrowAsync<ErrorOnValidationException>();
        ex.Which.GetErrorMessages().Should().Contain("Password must be at least 8 characters");
    }

    [Fact]
    public async Task Execute_WithPasswordExactly8Chars_DoesNotThrow()
    {
        var act = () => _sut.Execute(BuildValidDto(password: "Abcd123!")); // 8 chars

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Execute_WithPasswordShorterThan8Chars_DoesNotSaveChanges()
    {
        var act = () => _sut.Execute(BuildValidDto(password: "short!1")); // 7 chars

        await act.Should().ThrowAsync<ErrorOnValidationException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }
}
