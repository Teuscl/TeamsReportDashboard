using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Models.Job;
using TeamsReportDashboard.Backend.Services.AnalysisJob.Start;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Tests.Fakes;

// ReSharper disable AccessToDisposedClosure

namespace TeamsReportDashboard.Tests.Unit;

public class StartAnalysisServiceTests
{
    // ── Helpers ─────────────────────────────────────────────────────────────

    private static IHttpClientFactory CreateFactory(HttpResponseMessage response)
    {
        var handler = new FakeHttpMessageHandler(response);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8001") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("PythonAnalysisService")).Returns(client);
        return factory.Object;
    }

    private static HttpResponseMessage PythonOk(string batchId = "batch_abc123") =>
        new(HttpStatusCode.Accepted)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { batch_id = batchId }),
                Encoding.UTF8,
                "application/json")
        };

    private static StartJobAnalysisDto MakeDto(string name = "Análise Junho")
    {
        var stream = new MemoryStream(new byte[] { 0x50, 0x4B, 0x05, 0x06 }); // ZIP empty header
        var formFile = new FormFile(stream, 0, stream.Length, "file", "chats.zip")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/zip"
        };
        return new StartJobAnalysisDto { Name = name, File = formFile };
    }

    private static SystemPrompt MakePrompt() => new()
    {
        Id = Guid.NewGuid(),
        Content = "Você é um analista de helpdesk.",
        CreatedAt = DateTime.UtcNow,
    };

    private static Mock<IValidator<StartJobAnalysisDto>> PassingValidator()
    {
        var v = new Mock<IValidator<StartJobAnalysisDto>>();
        v.Setup(x => x.ValidateAsync(It.IsAny<StartJobAnalysisDto>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(new ValidationResult());
        return v;
    }

    private static Mock<IValidator<StartJobAnalysisDto>> FailingValidator(string error = "Nome inválido")
    {
        var v = new Mock<IValidator<StartJobAnalysisDto>>();
        v.Setup(x => x.ValidateAsync(It.IsAny<StartJobAnalysisDto>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(new ValidationResult([new ValidationFailure("Name", error)]));
        return v;
    }

    // ── Testes ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenPromptExistsAndPythonSucceeds_ReturnsJobId()
    {
        var uow = new FakeUnitOfWork();
        var prompt = MakePrompt();
        uow.SystemPromptRepo.Seed(prompt);

        var sut = new StartAnalysisService(
            uow, PassingValidator().Object, CreateFactory(PythonOk()), NullLogger<StartAnalysisService>.Instance);

        var result = await sut.ExecuteAsync(MakeDto(), Guid.NewGuid());

        result.Should().NotBeEmpty();
        uow.AnalysisJobRepo.Store.Should().HaveCount(1);
        uow.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPromptExists_SetsSystemPromptIdOnJob()
    {
        var uow = new FakeUnitOfWork();
        var prompt = MakePrompt();
        uow.SystemPromptRepo.Seed(prompt);

        var sut = new StartAnalysisService(
            uow, PassingValidator().Object, CreateFactory(PythonOk()), NullLogger<StartAnalysisService>.Instance);

        await sut.ExecuteAsync(MakeDto(), Guid.NewGuid());

        var savedJob = uow.AnalysisJobRepo.Store.Single();
        savedJob.SystemPromptId.Should().Be(prompt.Id);
        savedJob.PythonBatchId.Should().Be("batch_abc123");
    }

    [Fact]
    public async Task ExecuteAsync_WhenPromptExists_SendsPromptContentToPython()
    {
        var uow = new FakeUnitOfWork();
        var prompt = MakePrompt();
        uow.SystemPromptRepo.Seed(prompt);

        string? capturedBody = null;
        var handler = new FakeHttpMessageHandler(async req =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync();
            return PythonOk();
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8001") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("PythonAnalysisService")).Returns(client);

        var sut = new StartAnalysisService(
            uow, PassingValidator().Object, factory.Object, NullLogger<StartAnalysisService>.Instance);

        await sut.ExecuteAsync(MakeDto(), Guid.NewGuid());

        capturedBody.Should().Contain(prompt.Content);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoPromptConfigured_ThrowsInvalidOperationException()
    {
        var uow = new FakeUnitOfWork();
        // SystemPromptRepo está vazio — nenhum prompt cadastrado

        var sut = new StartAnalysisService(
            uow, PassingValidator().Object, CreateFactory(PythonOk()), NullLogger<StartAnalysisService>.Instance);

        var act = () => sut.ExecuteAsync(MakeDto(), Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*prompt*");
    }

    [Fact]
    public async Task ExecuteAsync_WhenPythonReturnsError_ThrowsHttpRequestException()
    {
        var uow = new FakeUnitOfWork();
        uow.SystemPromptRepo.Seed(MakePrompt());

        var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal error")
        };

        var sut = new StartAnalysisService(
            uow, PassingValidator().Object, CreateFactory(errorResponse), NullLogger<StartAnalysisService>.Instance);

        var act = () => sut.ExecuteAsync(MakeDto(), Guid.NewGuid());

        await act.Should().ThrowAsync<HttpRequestException>();
        uow.AnalysisJobRepo.Store.Should().BeEmpty();
        uow.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPythonReturnsMissingBatchId_ThrowsInvalidOperationException()
    {
        var uow = new FakeUnitOfWork();
        uow.SystemPromptRepo.Seed(MakePrompt());

        var emptyResponse = new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { batch_id = (string?)null }),
                Encoding.UTF8,
                "application/json")
        };

        var sut = new StartAnalysisService(
            uow, PassingValidator().Object, CreateFactory(emptyResponse), NullLogger<StartAnalysisService>.Instance);

        var act = () => sut.ExecuteAsync(MakeDto(), Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*batch id*");
        uow.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidationFails_ThrowsErrorOnValidationException()
    {
        var uow = new FakeUnitOfWork();
        uow.SystemPromptRepo.Seed(MakePrompt());

        var sut = new StartAnalysisService(
            uow, FailingValidator().Object, CreateFactory(PythonOk()), NullLogger<StartAnalysisService>.Instance);

        var act = () => sut.ExecuteAsync(MakeDto(), Guid.NewGuid());

        await act.Should().ThrowAsync<ErrorOnValidationException>();
        uow.AnalysisJobRepo.Store.Should().BeEmpty();
        uow.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidationFails_DoesNotCallPython()
    {
        var uow = new FakeUnitOfWork();
        uow.SystemPromptRepo.Seed(MakePrompt());

        var factoryMock = new Mock<IHttpClientFactory>();

        var sut = new StartAnalysisService(
            uow, FailingValidator().Object, factoryMock.Object, NullLogger<StartAnalysisService>.Instance);

        var act = () => sut.ExecuteAsync(MakeDto(), Guid.NewGuid());

        await act.Should().ThrowAsync<ErrorOnValidationException>();
        factoryMock.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }
}
