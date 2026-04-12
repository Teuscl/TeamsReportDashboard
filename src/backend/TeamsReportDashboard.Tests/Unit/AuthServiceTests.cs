using FluentAssertions;
using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Entities.Enums;
using TeamsReportDashboard.Models.Auth;
using TeamsReportDashboard.Services;
using TeamsReportDashboard.Tests.Fakes;

namespace TeamsReportDashboard.Tests.Unit;

public class AuthServiceTests
{
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakeTokenService _tokenService = new();
    private readonly FakePasswordService _passwordService = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_uow, _tokenService, _passwordService);
    }

    private User BuildActiveUser(int id = 1, string email = "user@example.com", string password = "password123") =>
        new()
        {
            Id = id,
            Name = "Test User",
            Email = email,
            Password = _passwordService.HashPassword(password),
            Role = UserRole.Admin,
            IsActive = true
        };

    // ── Happy path ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsCorrectUserInfo()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        var result = await _sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "password123" });

        result.Id.Should().Be(user.Id);
        result.Name.Should().Be(user.Name);
        result.Role.Should().Be(user.Role.ToString());
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokensFromTokenService()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        var result = await _sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "password123" });

        result.Token.Should().Be(_tokenService.LastGeneratedToken);
        result.RefreshToken.Should().Be(_tokenService.LastGeneratedRefreshToken);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_PersistsRefreshTokenToUser()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        var result = await _sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "password123" });

        user.RefreshToken.Should().Be(result.RefreshToken);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_SetsRefreshTokenExpiry7Days()
    {
        var user = BuildActiveUser();
        _uow.UserRepo.Seed(user);

        await _sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "password123" });

        user.RefreshTokenExpiryTime.Should()
            .BeCloseTo(DateTime.UtcNow.AddDays(7), precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_CallsSaveChangesOnce()
    {
        _uow.UserRepo.Seed(BuildActiveUser());

        await _sut.LoginAsync(new LoginRequest { Email = "user@example.com", Password = "password123" });

        _uow.SaveChangesCallCount.Should().Be(1);
    }

    // ── Invalid credentials ────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ThrowsUnauthorizedAccessException()
    {
        var act = () => _sut.LoginAsync(new LoginRequest { Email = "ghost@example.com", Password = "any" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        _uow.UserRepo.Seed(BuildActiveUser());

        var act = () => _sut.LoginAsync(new LoginRequest { Email = "user@example.com", Password = "wrong!" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials");
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ThrowsUnauthorizedAccessException()
    {
        var inactive = BuildActiveUser();
        inactive.IsActive = false;
        _uow.UserRepo.Seed(inactive);

        var act = () => _sut.LoginAsync(new LoginRequest { Email = inactive.Email, Password = "password123" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_DoesNotCallSaveChanges()
    {
        var act = () => _sut.LoginAsync(new LoginRequest { Email = "ghost@example.com", Password = "any" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_DoesNotCallGenerateToken()
    {
        var act = () => _sut.LoginAsync(new LoginRequest { Email = "ghost@example.com", Password = "any" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _tokenService.GenerateTokenCallCount.Should().Be(0);
    }
}
