using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Repositories;
using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Entities.Enums;

namespace TeamsReportDashboard.Tests.Unit;

/// <summary>
/// Tests UserRepository directly against the EF in-memory provider.
/// Focused on the IsActive security filters added in the fix/auth-problems branch.
/// </summary>
public class UserRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserRepository _sut;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // isolated DB per test
            .Options;

        _context = new AppDbContext(options);
        _sut = new UserRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    private static User BuildUser(bool isActive = true, string? refreshToken = null, string? resetToken = null)
    {
        var user = new User
        {
            Name = "Test User",
            Email = $"user-{Guid.NewGuid()}@example.com",
            Password = "hashed",
            Role = UserRole.Admin,
            IsActive = isActive,
            RefreshToken = refreshToken,
            PasswordResetToken = resetToken
        };
        return user;
    }

    private async Task<User> Seed(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    // ── GetByRefreshTokenAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByRefreshTokenAsync_WithActiveUser_ReturnsUser()
    {
        var user = await Seed(BuildUser(isActive: true, refreshToken: "valid-token"));

        var result = await _sut.GetByRefreshTokenAsync("valid-token");

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByRefreshTokenAsync_WithInactiveUser_ReturnsNull()
    {
        // Security fix: inactive users must not renew tokens
        await Seed(BuildUser(isActive: false, refreshToken: "active-token"));

        var result = await _sut.GetByRefreshTokenAsync("active-token");

        result.Should().BeNull("inactive users must not be able to refresh their tokens");
    }

    [Fact]
    public async Task GetByRefreshTokenAsync_WithNonExistentToken_ReturnsNull()
    {
        var result = await _sut.GetByRefreshTokenAsync("ghost-token");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByRefreshTokenAsync_WithNullToken_DoesNotMatchActiveUser()
    {
        await Seed(BuildUser(isActive: true, refreshToken: null));

        var result = await _sut.GetByRefreshTokenAsync("anything");

        result.Should().BeNull();
    }

    // ── GetByPasswordResetToken ────────────────────────────────────────────────

    [Fact]
    public async Task GetByPasswordResetToken_WithActiveUser_ReturnsUser()
    {
        var user = await Seed(BuildUser(isActive: true, resetToken: "hashed-reset-token"));

        var result = await _sut.GetByPasswordResetToken("hashed-reset-token");

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByPasswordResetToken_WithInactiveUser_ReturnsNull()
    {
        // Security fix: inactive users must not reset password
        await Seed(BuildUser(isActive: false, resetToken: "hashed-reset-token"));

        var result = await _sut.GetByPasswordResetToken("hashed-reset-token");

        result.Should().BeNull("inactive users must not be able to reset their passwords");
    }

    [Fact]
    public async Task GetByPasswordResetToken_WithNonExistentToken_ReturnsNull()
    {
        var result = await _sut.GetByPasswordResetToken("unknown-token");

        result.Should().BeNull();
    }

    // ── GetByEmailAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByEmailAsync_WithActiveUser_ReturnsUser()
    {
        var user = await Seed(BuildUser(isActive: true));

        var result = await _sut.GetByEmailAsync(user.Email);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_WithInactiveUser_ReturnsNull()
    {
        var user = await Seed(BuildUser(isActive: false));

        var result = await _sut.GetByEmailAsync(user.Email);

        result.Should().BeNull();
    }
}
