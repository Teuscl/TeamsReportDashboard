using AutoMapper;
using FluentAssertions;
using Moq;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Models.ReportDto;
using TeamsReportDashboard.Backend.Services.Report.Create;
using TeamsReportDashboard.Backend.Services.Report.Delete;
using TeamsReportDashboard.Backend.Services.Report.Read;
using TeamsReportDashboard.Backend.Services.Report.Update;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Tests.Fakes;

namespace TeamsReportDashboard.Tests.Unit;

public class ReportServiceTests
{
    private readonly FakeUnitOfWork _uow = new();

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static CreateReportDto ValidCreateDto(
        string requesterName = "João Silva",
        string requesterEmail = "joao@example.com") => new()
    {
        RequesterName = requesterName,
        RequesterEmail = requesterEmail,
        TechnicianName = "Carlos Técnico",
        RequestDate = DateTime.UtcNow.AddDays(-1),
        ReportedProblem = "Computador não liga",
        Category = "Hardware",
        FirstResponseTime = TimeSpan.FromMinutes(5),
        AverageHandlingTime = TimeSpan.FromMinutes(30),
        AnalysisJobId = Guid.NewGuid()
    };

    private Report SeedReport(Guid? requesterId = null)
    {
        var report = new Report
        {
            Id = Guid.NewGuid(),
            RequesterId = requesterId ?? Guid.NewGuid(),
            ReportedProblem = "Computador não liga",
            Category = "Hardware",
            TechnicianName = "Carlos",
            RequestDate = DateTime.UtcNow.AddDays(-1),
            AnalysisJobId = Guid.NewGuid()
        };
        _uow.ReportRepo.Seed(report);
        return report;
    }

    // Loose mock: Map() returns null (not used by service), enabling test isolation
    // without requiring AutoMapper to be a direct dependency of the test project.
    private static IMapper BuildMapper() => new Mock<IMapper>().Object;

    // ── CreateReportService ─────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WhenRequesterDoesNotExist_CreatesRequesterAndReport()
    {
        var sut = new CreateReportService(_uow, new CreateReportValidator());

        await sut.Execute(ValidCreateDto(requesterEmail: "novo@example.com"));

        // Requester was auto-created in the repository
        var requesters = await _uow.RequesterRepo.GetAllAsync();
        requesters.Should().HaveCount(1);
        requesters[0].Email.Should().Be("novo@example.com");

        // Report was created
        _uow.ReportRepo.CreateCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Create_WhenRequesterAlreadyExists_ReusesSameRequester()
    {
        var existing = new Requester { Id = Guid.NewGuid(), Name = "João", Email = "joao@example.com" };
        _uow.RequesterRepo.Seed(existing);
        var sut = new CreateReportService(_uow, new CreateReportValidator());

        await sut.Execute(ValidCreateDto(requesterEmail: "joao@example.com"));

        // No new requester should be created
        var requesters = await _uow.RequesterRepo.GetAllAsync();
        requesters.Should().HaveCount(1);
        _uow.ReportRepo.CreateCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Create_WithValidDto_CallsSaveChangesOnce()
    {
        var sut = new CreateReportService(_uow, new CreateReportValidator());

        await sut.Execute(ValidCreateDto());

        _uow.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Create_WithEmptyRequesterName_ThrowsErrorOnValidationException()
    {
        var sut = new CreateReportService(_uow, new CreateReportValidator());

        var act = () => sut.Execute(ValidCreateDto(requesterName: string.Empty));

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task Create_WithInvalidRequesterEmail_ThrowsErrorOnValidationException()
    {
        var sut = new CreateReportService(_uow, new CreateReportValidator());

        var act = () => sut.Execute(ValidCreateDto(requesterEmail: "not-an-email"));

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task Create_WithFutureRequestDate_ThrowsErrorOnValidationException()
    {
        var dto = ValidCreateDto();
        dto.RequestDate = DateTime.UtcNow.AddDays(1);
        var sut = new CreateReportService(_uow, new CreateReportValidator());

        var act = () => sut.Execute(dto);

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task Create_WithValidationError_DoesNotSaveChanges()
    {
        var sut = new CreateReportService(_uow, new CreateReportValidator());

        var act = () => sut.Execute(ValidCreateDto(requesterName: string.Empty));

        await act.Should().ThrowAsync<ErrorOnValidationException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }

    // ── GetReportService ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsAllReportsAsDtos()
    {
        SeedReport();
        SeedReport();
        var sut = new GetReportService(_uow);

        var result = await sut.GetAll();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_MapsTechnicianNameCorrectly()
    {
        var report = SeedReport();
        report.TechnicianName = "Carlos Técnico";
        var sut = new GetReportService(_uow);

        var result = await sut.GetAll();

        result.First().TechnicianName.Should().Be("Carlos Técnico");
    }

    [Fact]
    public async Task GetAll_WithRequesterNavigation_MapsRequesterFields()
    {
        var requester = new Requester { Id = Guid.NewGuid(), Name = "João", Email = "joao@example.com" };
        var report = new Report
        {
            Id = Guid.NewGuid(),
            RequesterId = requester.Id,
            Requester = requester,
            ReportedProblem = "Problema",
            Category = "Software",
            AnalysisJobId = Guid.NewGuid()
        };
        _uow.ReportRepo.Seed(report);
        var sut = new GetReportService(_uow);

        var result = await sut.GetAll();

        var dto = result.First();
        dto.RequesterName.Should().Be("João");
        dto.RequesterEmail.Should().Be("joao@example.com");
    }

    [Fact]
    public async Task GetById_WithExistingId_ReturnsCorrectDto()
    {
        var report = SeedReport();
        var sut = new GetReportService(_uow);

        var result = await sut.Get(report.Id);

        result.Id.Should().Be(report.Id);
        result.ReportedProblem.Should().Be(report.ReportedProblem);
    }

    [Fact]
    public async Task GetById_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var sut = new GetReportService(_uow);

        var act = () => sut.Get(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Report not found*");
    }

    // ── DeleteReportService ─────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_RemovesReportAndCallsSaveChanges()
    {
        var report = SeedReport();
        var sut = new DeleteReportService(_uow);

        await sut.Execute(report.Id);

        var all = await _uow.ReportRepo.GetAllAsync();
        all.Should().BeEmpty();
        _uow.SaveChangesCallCount.Should().Be(1);
    }

    // ── UpdateReportService ─────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithValidData_CallsUpdateAndSaveChanges()
    {
        var report = SeedReport();
        var requester = new Requester { Id = Guid.NewGuid(), Name = "Maria", Email = "maria@example.com" };
        _uow.RequesterRepo.Seed(requester);
        var sut = new UpdateReportService(_uow, new UpdateReportValidator(), BuildMapper());

        await sut.Execute(report.Id, new UpdateReportDto
        {
            RequesterId = requester.Id,
            TechnicianName = "Novo Técnico",
            Category = "Software",
            ReportedProblem = "Novo problema",
            RequestDate = DateTime.UtcNow.AddDays(-2)
        });

        _uow.ReportRepo.UpdateCallCount.Should().Be(1);
        _uow.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Update_SetsUpdatedAt()
    {
        var report = SeedReport();
        var requester = new Requester { Id = Guid.NewGuid(), Name = "Maria", Email = "maria@example.com" };
        _uow.RequesterRepo.Seed(requester);
        var sut = new UpdateReportService(_uow, new UpdateReportValidator(), BuildMapper());

        await sut.Execute(report.Id, new UpdateReportDto
        {
            RequesterId = requester.Id,
            TechnicianName = "Técnico",
            Category = "Software",
            ReportedProblem = "Problema",
            RequestDate = DateTime.UtcNow.AddDays(-1)
        });

        report.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Update_WithUnknownReportId_ThrowsErrorOnValidationException()
    {
        var sut = new UpdateReportService(_uow, new UpdateReportValidator(), BuildMapper());

        var act = () => sut.Execute(Guid.NewGuid(), new UpdateReportDto
        {
            RequesterId = Guid.NewGuid(),
            TechnicianName = "Técnico",
            Category = "Software",
            ReportedProblem = "Problema"
        });

        var ex = await act.Should().ThrowAsync<ErrorOnValidationException>();
        ex.Which.GetErrorMessages().Should().Contain("Report not found");
    }

    [Fact]
    public async Task Update_WithEmptyRequesterId_ThrowsErrorOnValidationException()
    {
        SeedReport();
        var sut = new UpdateReportService(_uow, new UpdateReportValidator(), BuildMapper());

        var act = () => sut.Execute(Guid.NewGuid(), new UpdateReportDto
        {
            RequesterId = Guid.Empty,
            TechnicianName = "Técnico"
        });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task Update_WhenRequesterIdChanges_SwapsRequesterNavigationProperty()
    {
        var originalRequester = new Requester { Id = Guid.NewGuid(), Name = "João", Email = "joao@example.com" };
        _uow.RequesterRepo.Seed(originalRequester);

        var report = new Report
        {
            Id = Guid.NewGuid(),
            RequesterId = originalRequester.Id,
            Requester = originalRequester,
            ReportedProblem = "Problema",
            Category = "Software",
            AnalysisJobId = Guid.NewGuid()
        };
        _uow.ReportRepo.Seed(report);

        var newRequester = new Requester { Id = Guid.NewGuid(), Name = "Maria", Email = "maria@example.com" };
        _uow.RequesterRepo.Seed(newRequester);

        var sut = new UpdateReportService(_uow, new UpdateReportValidator(), BuildMapper());

        await sut.Execute(report.Id, new UpdateReportDto
        {
            RequesterId = newRequester.Id,
            TechnicianName = "Técnico",
            Category = "Software",
            ReportedProblem = "Problema"
        });

        report.Requester.Id.Should().Be(newRequester.Id);
    }
}
