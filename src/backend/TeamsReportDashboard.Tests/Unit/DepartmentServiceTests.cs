using FluentAssertions;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Models.DepartmentDto;
using TeamsReportDashboard.Backend.Services.Department.Create;
using TeamsReportDashboard.Backend.Services.Department.Delete;
using TeamsReportDashboard.Backend.Services.Department.Read;
using TeamsReportDashboard.Backend.Services.Department.Update;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Tests.Fakes;

namespace TeamsReportDashboard.Tests.Unit;

public class DepartmentServiceTests
{
    private readonly FakeUnitOfWork _uow = new();

    private Department SeedDepartment(string name = "TI") =>
        SeedDepartment(Guid.NewGuid(), name);

    private Department SeedDepartment(Guid id, string name)
    {
        var dept = new Department { Id = id, Name = name };
        _uow.DepartmentRepo.Seed(dept);
        return dept;
    }

    // ── CreateDepartmentService ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidDto_AddsDepartmentToRepository()
    {
        var sut = new CreateDepartmentService(_uow, new CreateDepartmentValidator());

        await sut.Execute(new CreateDepartmentDto { Name = "TI" });

        var all = await _uow.DepartmentRepo.GetAllAsync();
        all.Should().HaveCount(1);
        all[0].Name.Should().Be("TI");
    }

    [Fact]
    public async Task Create_WithValidDto_CallsSaveChangesOnce()
    {
        var sut = new CreateDepartmentService(_uow, new CreateDepartmentValidator());

        await sut.Execute(new CreateDepartmentDto { Name = "TI" });

        _uow.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Create_WithEmptyName_ThrowsErrorOnValidationException()
    {
        var sut = new CreateDepartmentService(_uow, new CreateDepartmentValidator());

        var act = () => sut.Execute(new CreateDepartmentDto { Name = string.Empty });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task Create_WithNameLongerThan30Chars_ThrowsErrorOnValidationException()
    {
        var sut = new CreateDepartmentService(_uow, new CreateDepartmentValidator());

        var act = () => sut.Execute(new CreateDepartmentDto { Name = new string('a', 31) });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task Create_WithValidationError_DoesNotSaveChanges()
    {
        var sut = new CreateDepartmentService(_uow, new CreateDepartmentValidator());

        var act = () => sut.Execute(new CreateDepartmentDto { Name = string.Empty });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }

    // ── UpdateDepartmentService ─────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithValidData_UpdatesNameAndSaves()
    {
        var dept = SeedDepartment("TI");
        var sut = new UpdateDepartmentService(_uow, new UpdateDepartmentValidator());

        await sut.Execute(dept.Id, new UpdateDepartmentDto { Name = "Infraestrutura" });

        dept.Name.Should().Be("Infraestrutura");
        _uow.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Update_WithValidData_SetsUpdatedAt()
    {
        var dept = SeedDepartment("TI");
        var sut = new UpdateDepartmentService(_uow, new UpdateDepartmentValidator());

        await sut.Execute(dept.Id, new UpdateDepartmentDto { Name = "Infraestrutura" });

        dept.UpdatedAt.Should().NotBeNull()
            .And.BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Update_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var sut = new UpdateDepartmentService(_uow, new UpdateDepartmentValidator());

        var act = () => sut.Execute(Guid.NewGuid(), new UpdateDepartmentDto { Name = "TI" });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Update_WithEmptyName_ThrowsErrorOnValidationException()
    {
        SeedDepartment("TI");
        var sut = new UpdateDepartmentService(_uow, new UpdateDepartmentValidator());

        var act = () => sut.Execute(Guid.NewGuid(), new UpdateDepartmentDto { Name = string.Empty });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task Update_WithValidationError_DoesNotSaveChanges()
    {
        SeedDepartment("TI");
        var sut = new UpdateDepartmentService(_uow, new UpdateDepartmentValidator());

        var act = () => sut.Execute(Guid.NewGuid(), new UpdateDepartmentDto { Name = string.Empty });

        await act.Should().ThrowAsync<ErrorOnValidationException>();
        _uow.SaveChangesCallCount.Should().Be(0);
    }

    // ── DeleteDepartmentService ─────────────────────────────────────────────────

    [Fact]
    public async Task Delete_CallsSaveChangesOnce()
    {
        var dept = SeedDepartment("TI");
        var sut = new DeleteDepartmentService(_uow);

        await sut.Execute(dept.Id);

        _uow.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Delete_RemovesDepartmentFromRepository()
    {
        var dept = SeedDepartment("TI");
        var sut = new DeleteDepartmentService(_uow);

        await sut.Execute(dept.Id);

        var all = await _uow.DepartmentRepo.GetAllAsync();
        all.Should().BeEmpty();
    }

    // ── GetDepartmentsService ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsAllDepartmentsAsDtos()
    {
        SeedDepartment("TI");
        SeedDepartment("Financeiro");
        var sut = new GetDepartmentsService(_uow);

        var result = await sut.GetDepartmentsAsync();

        result.Should().HaveCount(2);
        result.Select(d => d.Name).Should().Contain(["TI", "Financeiro"]);
    }

    [Fact]
    public async Task GetAll_WithNoDepartments_ReturnsEmptyList()
    {
        var sut = new GetDepartmentsService(_uow);

        var result = await sut.GetDepartmentsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_WithExistingId_ReturnsDtoWithCorrectData()
    {
        var id = Guid.NewGuid();
        SeedDepartment(id, "TI");
        var sut = new GetDepartmentsService(_uow);

        var result = await sut.Get(id);

        result.Id.Should().Be(id);
        result.Name.Should().Be("TI");
    }

    [Fact]
    public async Task GetById_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var sut = new GetDepartmentsService(_uow);

        var act = () => sut.Get(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
