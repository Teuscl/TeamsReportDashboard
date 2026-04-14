using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Services.Requester.BulkCreate;
using TeamsReportDashboard.Backend.Services.Requester.Create;
using TeamsReportDashboard.Tests.Fakes;

namespace TeamsReportDashboard.Tests.Unit;

public class BulkCreateRequesterServiceTests
{
    private readonly FakeUnitOfWork _uow = new();
    private readonly BulkCreateRequesterService _sut;

    public BulkCreateRequesterServiceTests()
    {
        var validator = new CreateRequesterValidator();
        _sut = new BulkCreateRequesterService(_uow, validator);
    }

    /// <summary>
    /// Creates a fake IFormFile backed by an in-memory CSV byte array.
    /// Headers must follow the RequesterCsvMap: Nome;Departamento;E-mail
    /// </summary>
    private static IFormFile CreateCsvFile(string csvContent)
    {
        var bytes = Encoding.UTF8.GetBytes(csvContent);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "requesters.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };
    }

    private Department SeedDepartment(string name = "TI")
    {
        var dept = new Department { Id = Guid.NewGuid(), Name = name };
        _uow.DepartmentRepo.Seed(dept);
        return dept;
    }

    // ── Regression: NullReferenceException com coluna Department vazia ──────────

    [Fact]
    public async Task Execute_WhenCsvRowHasEmptyDepartment_DoesNotThrowNullReferenceException()
    {
        // Bug: record.Department.ToLowerInvariant() lançava NullReferenceException quando
        // a coluna Departamento estava vazia. Corrected com IsNullOrWhiteSpace check.
        var csv = "Nome;Departamento;E-mail\nJoão Silva;;joao@example.com";
        var file = CreateCsvFile(csv);

        var act = () => _sut.Execute(file);

        await act.Should().NotThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task Execute_WhenCsvRowHasEmptyDepartment_AddsFailureWithVazioLabel()
    {
        var csv = "Nome;Departamento;E-mail\nJoão Silva;;joao@example.com";
        var file = CreateCsvFile(csv);

        var result = await _sut.Execute(file);

        result.HasErrors.Should().BeTrue();
        result.Failures.Should().HaveCount(1);
        result.Failures[0].ErrorMessage.Should().Contain("(vazio)");
    }

    [Fact]
    public async Task Execute_WhenCsvRowHasEmptyDepartment_ReturnsNoSuccessfulInserts()
    {
        var csv = "Nome;Departamento;E-mail\nJoão Silva;;joao@example.com";
        var file = CreateCsvFile(csv);

        var result = await _sut.Execute(file);

        result.SuccessfulInserts.Should().Be(0);
    }

    // ── Departamento desconhecido ────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WhenCsvRowHasUnknownDepartment_AddsFailureWithDepartmentName()
    {
        // Nenhum departamento cadastrado no fake
        var csv = "Nome;Departamento;E-mail\nMaria Silva;Financeiro;maria@example.com";
        var file = CreateCsvFile(csv);

        var result = await _sut.Execute(file);

        result.HasErrors.Should().BeTrue();
        result.Failures.Should().HaveCount(1);
        result.Failures[0].ErrorMessage.Should().Contain("Financeiro");
    }

    [Fact]
    public async Task Execute_WhenCsvRowHasUnknownDepartment_RowNumberIsCorrect()
    {
        var csv = "Nome;Departamento;E-mail\nMaria Silva;Inexistente;maria@example.com";
        var file = CreateCsvFile(csv);

        var result = await _sut.Execute(file);

        result.Failures[0].RowNumber.Should().Be(2); // linha 1 = cabeçalho, linha 2 = primeiro dado
    }

    // ── Happy path ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithValidCsv_InsertsAllRecords()
    {
        SeedDepartment("TI");
        var csv = "Nome;Departamento;E-mail\nJoão Silva;TI;joao@example.com\nMaria Santos;TI;maria@example.com";
        var file = CreateCsvFile(csv);

        var result = await _sut.Execute(file);

        result.HasErrors.Should().BeFalse();
        result.SuccessfulInserts.Should().Be(2);
    }

    [Fact]
    public async Task Execute_WithValidCsv_DepartmentLookupIsCaseInsensitive()
    {
        SeedDepartment("TI");
        var csv = "Nome;Departamento;E-mail\nJoão Silva;ti;joao@example.com"; // minúsculas
        var file = CreateCsvFile(csv);

        var result = await _sut.Execute(file);

        result.HasErrors.Should().BeFalse();
        result.SuccessfulInserts.Should().Be(1);
    }

    // ── Duplicidade de e-mail ────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithDuplicateEmailInSameFile_AddsFailureForSecondOccurrence()
    {
        SeedDepartment("TI");
        var csv = "Nome;Departamento;E-mail\nJoão Silva;TI;joao@example.com\nJoão Clone;TI;joao@example.com";
        var file = CreateCsvFile(csv);

        var result = await _sut.Execute(file);

        result.HasErrors.Should().BeTrue();
        result.Failures.Should().HaveCount(1);
        result.Failures[0].ErrorMessage.Should().Contain("joao@example.com");
    }

    [Fact]
    public async Task Execute_WhenEmailAlreadyExistsInDatabase_AddsFailure()
    {
        SeedDepartment("TI");
        _uow.RequesterRepo.Seed(new Requester
        {
            Id = Guid.NewGuid(),
            Name = "Existing User",
            Email = "existing@example.com"
        });

        var csv = "Nome;Departamento;E-mail\nNew User;TI;existing@example.com";
        var file = CreateCsvFile(csv);

        var result = await _sut.Execute(file);

        result.HasErrors.Should().BeTrue();
        result.Failures[0].ErrorMessage.Should().Contain("existing@example.com");
    }

    [Fact]
    public async Task Execute_WhenEmailAlreadyExistsInDatabase_DoesNotInsertAnyRecord()
    {
        SeedDepartment("TI");
        _uow.RequesterRepo.Seed(new Requester
        {
            Id = Guid.NewGuid(),
            Name = "Existing User",
            Email = "existing@example.com"
        });

        var csv = "Nome;Departamento;E-mail\nNew User;TI;existing@example.com";
        var file = CreateCsvFile(csv);

        await _sut.Execute(file);

        _uow.RequesterRepo.CreateRangeCallCount.Should().Be(0);
    }

    // ── CSV inválido ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithEmptyCsvFile_ReturnsFailure()
    {
        var csv = "Nome;Departamento;E-mail"; // só cabeçalho, sem dados
        var file = CreateCsvFile(csv);

        var result = await _sut.Execute(file);

        // Zero records parsed → failures reported, zero inserts
        result.SuccessfulInserts.Should().Be(0);
    }
}
