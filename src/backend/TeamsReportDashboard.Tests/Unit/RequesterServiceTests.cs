using FluentAssertions;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Exceptions;
using TeamsReportDashboard.Backend.Models.Requester;
using TeamsReportDashboard.Backend.Services.Requester.Create;
using TeamsReportDashboard.Backend.Services.Requester.Delete;
using TeamsReportDashboard.Backend.Services.Requester.Read;
using TeamsReportDashboard.Backend.Services.Requester.Update;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Tests.Fakes;

namespace TeamsReportDashboard.Tests.Unit;

public class RequesterServiceTests
{
    private readonly FakeUnitOfWork _uow = new();

    private Requester SeedRequester(string name = "João Silva", string email = "joao@example.com")
    {
        var req = new Requester { Id = Guid.NewGuid(), Name = name, Email = email };
        _uow.RequesterRepo.Seed(req);
        return req;
    }

    private CreateRequesterDto ValidCreateDto(string name = "João Silva", string email = "joao@example.com") =>
        new() { Name = name, Email = email, DepartmentId = null };

    private UpdateRequesterDto ValidUpdateDto(string name = "João Atualizado", string email = "joao.novo@example.com") =>
        new() { Name = name, Email = email, DepartmentId = null };

    // ── CreateRequesterService ──────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidDto_AddsRequesterToRepository()
    {
        var sut = new CreateRequesterService(_uow, new CreateRequesterValidator());

        await sut.Execute(ValidCreateDto());

        var all = await _uow.RequesterRepo.GetAllAsync();
        all.Should().HaveCount(1);
        all[0].Name.Should().Be("João Silva");
        all[0].Email.Should().Be("joao@example.com");
    }

    [Fact]
    public async Task Create_WithValidDto_CallsSaveChangesOnce()
    {
        var sut = new CreateRequesterService(_uow, new CreateRequesterValidator());

        await sut.Execute(ValidCreateDto());

        _uow.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ThrowsErrorOnValidationException()
    {
        SeedRequester(email: "joao@example.com");
        var sut = new CreateRequesterService(_uow, new CreateRequesterValidator());

        var exception = await Assert.ThrowsAsync<ErrorOnValidationException>(() =>
            sut.Execute(ValidCreateDto(email: "joao@example.com")));

        exception.GetErrorMessages().Should().Contain("Email already exists");
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_DoesNotSaveChanges()
    {
        SeedRequester(email: "joao@example.com");
        var sut = new CreateRequesterService(_uow, new CreateRequesterValidator());

        var act = () => sut.Execute(ValidCreateDto(email: "joao@example.com"));

        await act.Should().ThrowAsync<ErrorOnValidationException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Create_WithEmptyName_ThrowsErrorOnValidationException()
    {
        var sut = new CreateRequesterService(_uow, new CreateRequesterValidator());

        var act = () => sut.Execute(ValidCreateDto(name: string.Empty));

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task Create_WithInvalidEmail_ThrowsErrorOnValidationException()
    {
        var sut = new CreateRequesterService(_uow, new CreateRequesterValidator());

        var act = () => sut.Execute(ValidCreateDto(email: "not-an-email"));

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    // ── UpdateRequesterService ──────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithValidData_UpdatesFieldsAndSaves()
    {
        var req = SeedRequester();
        var sut = new UpdateRequesterService(_uow, new UpdateRequesterValidator());

        await sut.Execute(req.Id, ValidUpdateDto(name: "Maria Silva", email: "maria@example.com"));

        req.Name.Should().Be("Maria Silva");
        req.Email.Should().Be("maria@example.com");
        _uow.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Update_WithValidData_SetsUpdatedAt()
    {
        var req = SeedRequester();
        var sut = new UpdateRequesterService(_uow, new UpdateRequesterValidator());

        await sut.Execute(req.Id, ValidUpdateDto());

        req.UpdatedAt.Should().NotBeNull()
            .And.BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Update_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var sut = new UpdateRequesterService(_uow, new UpdateRequesterValidator());

        var act = () => sut.Execute(Guid.NewGuid(), ValidUpdateDto());

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Solicitante não encontrado*");
    }

    [Fact]
    public async Task Update_WithEmptyName_ThrowsErrorOnValidationException()
    {
        var req = SeedRequester();
        var sut = new UpdateRequesterService(_uow, new UpdateRequesterValidator());

        var act = () => sut.Execute(req.Id, ValidUpdateDto(name: string.Empty));

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task Update_WithInvalidEmail_ThrowsErrorOnValidationException()
    {
        var req = SeedRequester();
        var sut = new UpdateRequesterService(_uow, new UpdateRequesterValidator());

        var act = () => sut.Execute(req.Id, ValidUpdateDto(email: "not-an-email"));

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task Update_WithValidationError_DoesNotSaveChanges()
    {
        var req = SeedRequester();
        var sut = new UpdateRequesterService(_uow, new UpdateRequesterValidator());

        var act = () => sut.Execute(req.Id, ValidUpdateDto(email: "not-an-email"));

        await act.Should().ThrowAsync<ErrorOnValidationException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }

    // ── DeleteRequesterService ──────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithNoAssociatedReports_DeletesAndSaves()
    {
        var req = SeedRequester();
        var sut = new DeleteRequesterService(_uow);

        await sut.Execute(req.Id);

        var all = await _uow.RequesterRepo.GetAllAsync();
        all.Should().BeEmpty();
        _uow.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Delete_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var sut = new DeleteRequesterService(_uow);

        var act = () => sut.Execute(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Solicitante não encontrado*");
    }

    [Fact]
    public async Task Delete_WhenRequesterHasReports_ThrowsConflictException()
    {
        var req = SeedRequester();
        // Seed a report pointing to this requester
        _uow.ReportRepo.Seed(new Report
        {
            Id = Guid.NewGuid(),
            RequesterId = req.Id,
            ReportedProblem = "Problema",
            Category = "Software",
            AnalysisJobId = Guid.NewGuid()
        });
        var sut = new DeleteRequesterService(_uow);

        var act = () => sut.Execute(req.Id);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Delete_WhenRequesterHasReports_DoesNotSaveChanges()
    {
        var req = SeedRequester();
        _uow.ReportRepo.Seed(new Report
        {
            Id = Guid.NewGuid(),
            RequesterId = req.Id,
            ReportedProblem = "Problema",
            Category = "Software",
            AnalysisJobId = Guid.NewGuid()
        });
        var sut = new DeleteRequesterService(_uow);

        var act = () => sut.Execute(req.Id);

        await act.Should().ThrowAsync<ConflictException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }

    // ── GetRequestersService ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsAllRequestersAsDtos()
    {
        SeedRequester("João", "joao@example.com");
        SeedRequester("Maria", "maria@example.com");
        var sut = new GetRequestersService(_uow);

        var result = await sut.GetAll();

        result.Should().HaveCount(2);
        result.Select(r => r.Name).Should().Contain(["João", "Maria"]);
    }

    [Fact]
    public async Task GetAll_WithNoDepartmentNavigation_MapsNullDepartmentName()
    {
        SeedRequester(); // Department navigation property is null
        var sut = new GetRequestersService(_uow);

        var result = await sut.GetAll();

        result.First().DepartmentName.Should().BeNull();
    }

    [Fact]
    public async Task GetById_WithExistingId_ReturnsDtoWithCorrectData()
    {
        var req = SeedRequester("João", "joao@example.com");
        var sut = new GetRequestersService(_uow);

        var result = await sut.Get(req.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(req.Id);
        result.Name.Should().Be("João");
        result.Email.Should().Be("joao@example.com");
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNull()
    {
        var sut = new GetRequestersService(_uow);

        var result = await sut.Get(Guid.NewGuid());

        result.Should().BeNull();
    }
}
