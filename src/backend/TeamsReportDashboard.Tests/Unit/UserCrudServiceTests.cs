using FluentAssertions;
using Moq;
using Microsoft.Extensions.Configuration;
using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Backend.Services.User.Update;
using TeamsReportDashboard.Backend.Services.User.UpdateMyProfile;
using TeamsReportDashboard.Entities;
using TeamsReportDashboard.Entities.Enums;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Services.User.Create;
using TeamsReportDashboard.Services.User.Delete;
using TeamsReportDashboard.Services.User.Read;
using TeamsReportDashboard.Services.User.Update;
using TeamsReportDashboard.Tests.Fakes;

namespace TeamsReportDashboard.Tests.Unit;

public class UserCrudServiceTests
{
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakePasswordService _passwordService = new();

    private User SeedActiveUser(
        string name = "Test User",
        string email = "user@example.com",
        UserRole role = UserRole.Admin)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Password = _passwordService.HashPassword("Password123!"),
            Role = role,
            IsActive = true
        };
        _uow.UserRepo.Seed(user);
        return user;
    }

    // ── GetUsersService ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsAllUsersAsDtos()
    {
        SeedActiveUser("Alice", "alice@example.com");
        SeedActiveUser("Bob", "bob@example.com");
        var sut = new GetUsersService(_uow);

        var result = await sut.GetAll();

        result.Should().HaveCount(2);
        result.Select(u => u.Name).Should().Contain(["Alice", "Bob"]);
    }

    [Fact]
    public async Task GetAll_MapsRoleCorrectly()
    {
        SeedActiveUser(role: UserRole.Master);
        var sut = new GetUsersService(_uow);

        var result = await sut.GetAll();

        result.First().Role.Should().Be(UserRole.Master);
    }

    [Fact]
    public async Task GetById_WithExistingId_ReturnsDtoWithCorrectData()
    {
        var user = SeedActiveUser("Carlos", "carlos@example.com");
        var sut = new GetUsersService(_uow);

        var result = await sut.Get(user.Id);

        result.Id.Should().Be(user.Id);
        result.Name.Should().Be("Carlos");
        result.Email.Should().Be("carlos@example.com");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var sut = new GetUsersService(_uow);

        var act = () => sut.Get(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── DeleteUserService ───────────────────────────────────────────────────────

    private static Mock<IConfiguration> BuildConfig(string? masterEmail = "master@example.com")
    {
        var mock = new Mock<IConfiguration>();
        mock.Setup(c => c["MasterUser:Email"]).Returns(masterEmail);
        return mock;
    }

    [Fact]
    public async Task Delete_WithValidUser_DeletesAndSaveChanges()
    {
        var user = SeedActiveUser(email: "regular@example.com");
        var sut = new DeleteUserService(_uow, BuildConfig().Object);

        await sut.Execute(user.Id);

        _uow.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Delete_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var sut = new DeleteUserService(_uow, BuildConfig().Object);

        var act = () => sut.Execute(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Delete_WithUnknownId_DoesNotSaveChanges()
    {
        var sut = new DeleteUserService(_uow, BuildConfig().Object);

        var act = () => sut.Execute(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Delete_WhenUserIsProtectedMaster_ThrowsInvalidOperationException()
    {
        var master = SeedActiveUser(email: "master@example.com", role: UserRole.Master);
        var sut = new DeleteUserService(_uow, BuildConfig(masterEmail: "master@example.com").Object);

        var act = () => sut.Execute(master.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be deleted*");
    }

    [Fact]
    public async Task Delete_WhenUserIsProtectedMaster_DoesNotSaveChanges()
    {
        var master = SeedActiveUser(email: "master@example.com", role: UserRole.Master);
        var sut = new DeleteUserService(_uow, BuildConfig(masterEmail: "master@example.com").Object);

        var act = () => sut.Execute(master.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Delete_WhenMasterEmailConfigIsMissing_AllowsDeletion()
    {
        // If no master email is configured, no user is protected
        var user = SeedActiveUser(email: "user@example.com");
        var sut = new DeleteUserService(_uow, BuildConfig(masterEmail: null).Object);

        await sut.Execute(user.Id);

        _uow.SaveChangesCallCount.Should().Be(1);
    }

    // ── UpdateUserService ───────────────────────────────────────────────────────

    private UpdateUserDto ValidUpdateUserDto(User user, string? name = null, string? email = null) => new()
    {
        Id = user.Id,
        Name = name ?? user.Name,
        Email = email ?? user.Email,
        Role = user.Role,
        IsActive = user.IsActive
    };

    [Fact]
    public async Task UpdateUser_WithValidData_UpdatesFieldsAndSaves()
    {
        var user = SeedActiveUser("Alice", "alice@example.com");
        var sut = new UpdateUserService(_uow, new UpdateUserValidator());

        await sut.Execute(ValidUpdateUserDto(user, name: "Alice Nova", email: "alice.nova@example.com"));

        user.Name.Should().Be("Alice Nova");
        user.Email.Should().Be("alice.nova@example.com");
        _uow.SaveChangesCallCount.Should().Be(1);
        _uow.UserRepo.UpdateCallCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateUser_WithValidData_SetsUpdatedAt()
    {
        var user = SeedActiveUser();
        var sut = new UpdateUserService(_uow, new UpdateUserValidator());

        await sut.Execute(ValidUpdateUserDto(user));

        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateUser_WithUnknownId_ThrowsErrorOnValidationException()
    {
        var sut = new UpdateUserService(_uow, new UpdateUserValidator());

        var act = () => sut.Execute(new UpdateUserDto
        {
            Id = Guid.NewGuid(),
            Name = "Qualquer",
            Email = "qualquer@example.com",
            Role = UserRole.Admin,
            IsActive = true
        });

        var ex = await act.Should().ThrowAsync<ErrorOnValidationException>();
        ex.Which.GetErrorMessages().Should().Contain("User not found");
    }

    [Fact]
    public async Task UpdateUser_WithEmailAlreadyTakenByAnotherUser_ThrowsErrorOnValidationException()
    {
        SeedActiveUser("Bob", "bob@example.com");
        var alice = SeedActiveUser("Alice", "alice@example.com");
        var sut = new UpdateUserService(_uow, new UpdateUserValidator());

        var exception = await Assert.ThrowsAsync<ErrorOnValidationException>(() =>
            sut.Execute(ValidUpdateUserDto(alice, email: "bob@example.com")));

        exception.GetErrorMessages().Should().Contain("Email already exists.");
    }

    [Fact]
    public async Task UpdateUser_WithEmptyEmail_ThrowsErrorOnValidationException()
    {
        var user = SeedActiveUser();
        var sut = new UpdateUserService(_uow, new UpdateUserValidator());

        var act = () => sut.Execute(ValidUpdateUserDto(user, email: string.Empty));

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task UpdateUser_WithSameEmailAsCurrentUser_DoesNotThrow()
    {
        // Keeping the same email should not be considered a duplicate
        var user = SeedActiveUser("Alice", "alice@example.com");
        var sut = new UpdateUserService(_uow, new UpdateUserValidator());

        var act = () => sut.Execute(ValidUpdateUserDto(user, email: "alice@example.com"));

        await act.Should().NotThrowAsync();
    }

    // ── UpdateMyProfileService ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateMyProfile_WithValidData_UpdatesNameEmailAndSaves()
    {
        var user = SeedActiveUser("Alice", "alice@example.com");
        var sut = new UpdateMyProfileService(_uow, new UpdateMyProfileValidator());

        await sut.Execute(user.Id, new UpdateMyProfileDto { Name = "Alice Nova", Email = "alice.nova@example.com" });

        user.Name.Should().Be("Alice Nova");
        user.Email.Should().Be("alice.nova@example.com");
        _uow.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateMyProfile_WithUnknownId_ThrowsErrorOnValidationException()
    {
        var sut = new UpdateMyProfileService(_uow, new UpdateMyProfileValidator());

        var exception = await Assert.ThrowsAsync<ErrorOnValidationException>(() =>
            sut.Execute(Guid.NewGuid(), new UpdateMyProfileDto { Name = "Qualquer", Email = "q@example.com" }));

        exception.GetErrorMessages().Should().Contain("User not found");
    }

    [Fact]
    public async Task UpdateMyProfile_WithEmailAlreadyTakenByAnotherUser_ThrowsErrorOnValidationException()
    {
        SeedActiveUser("Bob", "bob@example.com");
        var alice = SeedActiveUser("Alice", "alice@example.com");
        var sut = new UpdateMyProfileService(_uow, new UpdateMyProfileValidator());

        var exception = await Assert.ThrowsAsync<ErrorOnValidationException>(() =>
            sut.Execute(alice.Id, new UpdateMyProfileDto { Name = "Alice", Email = "bob@example.com" }));

        exception.GetErrorMessages().Should().Contain("Email already taken");
    }

    [Fact]
    public async Task UpdateMyProfile_WithEmptyName_ThrowsErrorOnValidationException()
    {
        var user = SeedActiveUser();
        var sut = new UpdateMyProfileService(_uow, new UpdateMyProfileValidator());

        var act = () => sut.Execute(user.Id, new UpdateMyProfileDto { Name = string.Empty, Email = "x@example.com" });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task UpdateMyProfile_WithInvalidEmail_ThrowsErrorOnValidationException()
    {
        var user = SeedActiveUser();
        var sut = new UpdateMyProfileService(_uow, new UpdateMyProfileValidator());

        var act = () => sut.Execute(user.Id, new UpdateMyProfileDto { Name = "Alice", Email = "not-an-email" });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task UpdateMyProfile_WithSameEmailAsCurrentUser_DoesNotThrow()
    {
        var user = SeedActiveUser("Alice", "alice@example.com");
        var sut = new UpdateMyProfileService(_uow, new UpdateMyProfileValidator());

        var act = () => sut.Execute(user.Id, new UpdateMyProfileDto { Name = "Alice", Email = "alice@example.com" });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateMyProfile_WithValidationError_DoesNotSaveChanges()
    {
        var user = SeedActiveUser();
        var sut = new UpdateMyProfileService(_uow, new UpdateMyProfileValidator());

        var act = () => sut.Execute(user.Id, new UpdateMyProfileDto { Name = string.Empty, Email = "x@example.com" });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }
}
