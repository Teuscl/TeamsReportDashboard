using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TeamsReportDashboard.Backend.Models.Configuration;
using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Entities.Enums;
using TeamsReportDashboard.Services;

namespace TeamsReportDashboard.Tests.Unit;

public class TokenServiceTests
{
    // Must be ≥ 32 chars to satisfy JwtSettings validation
    private const string TestKey = "test-secret-key-32-chars-minimum!!";
    private readonly TokenService _sut;

    public TokenServiceTests()
    {
        _sut = new TokenService(Options.Create(new JwtSettings { Key = TestKey }));
    }

    private static User BuildUser(int id = 1, UserRole role = UserRole.Admin) => new()
    {
        Id = id,
        Name = "Test User",
        Email = "test@example.com",
        Role = role,
        IsActive = true,
        Password = "hashed"
    };

    // ── GenerateToken ──────────────────────────────────────────────────────────

    [Fact]
    public void GenerateToken_ReturnsWellFormedJwt()
    {
        var token = _sut.GenerateToken(BuildUser());

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_ContainsUserIdClaim()
    {
        var user = BuildUser(id: 42);
        var parsed = ParseToken(_sut.GenerateToken(user));

        parsed.Claims.Should().Contain(c => c.Type == "id" && c.Value == "42");
    }

    [Fact]
    public void GenerateToken_ContainsEmailClaim()
    {
        var user = BuildUser();
        var parsed = ParseToken(_sut.GenerateToken(user));

        parsed.Claims.Should().Contain(c => c.Type == "email" && c.Value == user.Email);
    }

    [Fact]
    public void GenerateToken_ContainsNameClaim()
    {
        var user = BuildUser();
        var parsed = ParseToken(_sut.GenerateToken(user));

        parsed.Claims.Should().Contain(c => c.Type == "name" && c.Value == user.Name);
    }

    [Fact]
    public void GenerateToken_ContainsRoleClaim()
    {
        var user = BuildUser(role: UserRole.Master);
        var parsed = ParseToken(_sut.GenerateToken(user));

        parsed.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Master");
    }

    [Fact]
    public void GenerateToken_HasCorrectIssuer()
    {
        var parsed = ParseToken(_sut.GenerateToken(BuildUser()));

        parsed.Issuer.Should().Be("TeamsReportDashboard");
    }

    [Fact]
    public void GenerateToken_HasCorrectAudience()
    {
        var parsed = ParseToken(_sut.GenerateToken(BuildUser()));

        parsed.Audiences.Should().Contain("TeamsReportDashboard");
    }

    [Fact]
    public void GenerateToken_ExpiresInApproximately2Hours()
    {
        var before = DateTime.UtcNow;
        var parsed = ParseToken(_sut.GenerateToken(BuildUser()));
        var after = DateTime.UtcNow;

        parsed.ValidTo.Should().BeAfter(before.AddHours(2).AddSeconds(-5));
        parsed.ValidTo.Should().BeBefore(after.AddHours(2).AddSeconds(5));
    }

    [Fact]
    public void GenerateToken_IsSignedWithHmacSha256()
    {
        var parsed = ParseToken(_sut.GenerateToken(BuildUser()));

        parsed.SignatureAlgorithm.Should().Be(SecurityAlgorithms.HmacSha256);
    }

    [Fact]
    public void GenerateToken_CanBeValidatedWithSameKey()
    {
        var token = _sut.GenerateToken(BuildUser());
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "TeamsReportDashboard",
            ValidateAudience = true,
            ValidAudience = "TeamsReportDashboard",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey))
        };

        var handler = new JwtSecurityTokenHandler();
        var act = () => handler.ValidateToken(token, validationParams, out _);

        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateToken_CannotBeValidatedWithDifferentKey()
    {
        var token = _sut.GenerateToken(BuildUser());
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("completely-different-key-32chars!")),
            ValidateIssuer = false,
            ValidateAudience = false
        };

        var handler = new JwtSecurityTokenHandler();
        var act = () => handler.ValidateToken(token, validationParams, out _);

        act.Should().Throw<SecurityTokenSignatureKeyNotFoundException>();
    }

    // ── GenerateRefreshToken ───────────────────────────────────────────────────

    [Fact]
    public void GenerateRefreshToken_IsValidBase64()
    {
        var refreshToken = _sut.GenerateRefreshToken();

        var act = () => Convert.FromBase64String(refreshToken);
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateRefreshToken_Is64Bytes()
    {
        var refreshToken = _sut.GenerateRefreshToken();

        Convert.FromBase64String(refreshToken).Length.Should().Be(64);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsDifferentValueEachCall()
    {
        var t1 = _sut.GenerateRefreshToken();
        var t2 = _sut.GenerateRefreshToken();

        t1.Should().NotBe(t2);
    }

    private static JwtSecurityToken ParseToken(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token);
}
