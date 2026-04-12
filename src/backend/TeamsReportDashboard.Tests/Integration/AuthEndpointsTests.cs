using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Entities.Enums;

namespace TeamsReportDashboard.Tests.Integration;

public class AuthEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    // BCrypt-hashed "Password123!" — generated once per test class (BCrypt is intentionally slow)
    private static readonly string HashedPassword = BCrypt.Net.BCrypt.HashPassword("Password123!");
    private const string PlainPassword = "Password123!";

    public AuthEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private User BuildUser(
        string email = "integration@example.com",
        string? refreshToken = null,
        DateTime? refreshTokenExpiry = null,
        string? resetToken = null,
        DateTime? resetTokenExpiry = null) => new()
    {
        Name = "Integration User",
        Email = email,
        Password = HashedPassword,
        Role = UserRole.Admin,
        IsActive = true,
        RefreshToken = refreshToken,
        RefreshTokenExpiryTime = refreshTokenExpiry,
        PasswordResetToken = resetToken,
        PasswordResetTokenExpiryTime = resetTokenExpiry
    };

    // ── POST /auth/login ───────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_Returns200()
    {
        await _factory.SeedUserAsync(BuildUser("login-ok@test.com"));

        var response = await _client.PostAsJsonAsync("/auth/login",
            new { email = "login-ok@test.com", password = PlainPassword });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithValidCredentials_SetsAccessTokenCookie()
    {
        await _factory.SeedUserAsync(BuildUser("login-cookie@test.com"));

        var response = await _client.PostAsJsonAsync("/auth/login",
            new { email = "login-cookie@test.com", password = PlainPassword });

        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        cookies.Should().Contain(c => c.StartsWith("accessToken="));
    }

    [Fact]
    public async Task Login_WithValidCredentials_SetsRefreshTokenCookie()
    {
        await _factory.SeedUserAsync(BuildUser("login-refresh@test.com"));

        var response = await _client.PostAsJsonAsync("/auth/login",
            new { email = "login-refresh@test.com", password = PlainPassword });

        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        cookies.Should().Contain(c => c.StartsWith("refreshToken="));
    }

    [Fact]
    public async Task Login_WithValidCredentials_ResponseBodyMatchesSnapshot()
    {
        await _factory.SeedUserAsync(BuildUser("login-snapshot@test.com"));

        var response = await _client.PostAsJsonAsync("/auth/login",
            new { email = "login-snapshot@test.com", password = PlainPassword });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        await Verify(new
        {
            StatusCode = (int)response.StatusCode,
            HasName = body.TryGetProperty("name", out _),
            HasRole = body.TryGetProperty("role", out _),
            HasId = body.TryGetProperty("id", out _),
        });
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        await _factory.SeedUserAsync(BuildUser("login-wrong-pw@test.com"));

        var response = await _client.PostAsJsonAsync("/auth/login",
            new { email = "login-wrong-pw@test.com", password = "WrongPassword!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/auth/login",
            new { email = "nobody@test.com", password = PlainPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ResponseBodyMatchesSnapshot()
    {
        var response = await _client.PostAsJsonAsync("/auth/login",
            new { email = "nobody@test.com", password = "wrong" });

        var body = await response.Content.ReadAsStringAsync();

        await Verify(new
        {
            StatusCode = (int)response.StatusCode,
            Body = body
        });
    }

    // ── POST /auth/refresh ────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_WithValidCookieToken_Returns200()
    {
        await _factory.SeedUserAsync(BuildUser(
            "refresh-ok@test.com",
            refreshToken: "valid-refresh-token",
            refreshTokenExpiry: DateTime.UtcNow.AddDays(7)));

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        request.Headers.Add("Cookie", "refreshToken=valid-refresh-token");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_WithValidToken_RotatesRefreshTokenCookie()
    {
        await _factory.SeedUserAsync(BuildUser(
            "refresh-rotate@test.com",
            refreshToken: "old-refresh-token",
            refreshTokenExpiry: DateTime.UtcNow.AddDays(7)));

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        request.Headers.Add("Cookie", "refreshToken=old-refresh-token");

        var response = await _client.SendAsync(request);

        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        cookies.Should().Contain(c => c.StartsWith("refreshToken="));
    }

    [Fact]
    public async Task Refresh_WithMissingCookie_Returns401()
    {
        var response = await _client.PostAsync("/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_Returns401()
    {
        await _factory.SeedUserAsync(BuildUser(
            "refresh-expired@test.com",
            refreshToken: "expired-refresh-token",
            refreshTokenExpiry: DateTime.UtcNow.AddDays(-1)));

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        request.Headers.Add("Cookie", "refreshToken=expired-refresh-token");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        request.Headers.Add("Cookie", "refreshToken=ghost-token");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /auth/logout ─────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_WithoutAccessToken_Returns200()
    {
        // This validates the [AllowAnonymous] fix — logout must work even when session has fully expired
        var response = await _client.PostAsync("/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_ClearsAccessTokenCookie()
    {
        var response = await _client.PostAsync("/auth/logout", null);

        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        // Expired cookies clear the browser value
        cookies.Should().Contain(c => c.StartsWith("accessToken=") && c.Contains("expires="));
    }

    [Fact]
    public async Task Logout_ClearsRefreshTokenCookie()
    {
        var response = await _client.PostAsync("/auth/logout", null);

        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        cookies.Should().Contain(c => c.StartsWith("refreshToken=") && c.Contains("expires="));
    }

    [Fact]
    public async Task Logout_WithActiveRefreshToken_NullsTokenInDatabase()
    {
        await _factory.SeedUserAsync(BuildUser(
            "logout-db@test.com",
            refreshToken: "token-to-clear",
            refreshTokenExpiry: DateTime.UtcNow.AddDays(7)));

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        request.Headers.Add("Cookie", "refreshToken=token-to-clear");

        await _client.SendAsync(request);

        // Verify token is invalidated — refresh with old token should fail
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        refreshRequest.Headers.Add("Cookie", "refreshToken=token-to-clear");
        var refreshResponse = await _client.SendAsync(refreshRequest);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /auth/forgot-password ────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_WithRegisteredEmail_Returns200()
    {
        await _factory.SeedUserAsync(BuildUser("forgot-ok@test.com"));

        var response = await _client.PostAsJsonAsync("/auth/forgot-password",
            new { email = "forgot-ok@test.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_WithUnknownEmail_AlsoReturns200()
    {
        // Security: must not reveal whether email exists in the system
        var response = await _client.PostAsJsonAsync("/auth/forgot-password",
            new { email = "nobody@test.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_ResponseBodyMatchesSnapshot()
    {
        var response = await _client.PostAsJsonAsync("/auth/forgot-password",
            new { email = "snapshot@test.com" });

        var body = await response.Content.ReadAsStringAsync();
        await Verify(new { StatusCode = (int)response.StatusCode, Body = body });
    }

    // ── POST /auth/reset-password-forgotten ───────────────────────────────────

    [Fact]
    public async Task ResetPasswordForgotten_WithExpiredToken_Returns400()
    {
        // This validates the fix: ArgumentException → ErrorOnValidationException (400, not 500)
        var rawToken = "some-raw-token";
        var hashedToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        await _factory.SeedUserAsync(BuildUser(
            "reset-expired@test.com",
            resetToken: hashedToken,
            resetTokenExpiry: DateTime.UtcNow.AddMinutes(-1)));

        var response = await _client.PostAsJsonAsync("/auth/reset-password-forgotten", new
        {
            token = rawToken,
            newPassword = "NewPassword1!",
            confirmPassword = "NewPassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPasswordForgotten_WithInvalidToken_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/auth/reset-password-forgotten", new
        {
            token = "completely-unknown-token",
            newPassword = "NewPassword1!",
            confirmPassword = "NewPassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPasswordForgotten_WithMismatchedPasswords_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/auth/reset-password-forgotten", new
        {
            token = "any",
            newPassword = "Password1!",
            confirmPassword = "DifferentPassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
