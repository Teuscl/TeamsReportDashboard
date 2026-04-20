using FluentValidation;
using System.Net.Http.Json;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Models.PromptDto;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Prompt.Update;

public class UpdatePromptService : IUpdatePromptService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<PromptDto> _validator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UpdatePromptService> _logger;

    public UpdatePromptService(
        IUnitOfWork unitOfWork,
        IValidator<PromptDto> validator,
        IHttpClientFactory httpClientFactory,
        ILogger<UpdatePromptService> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(PromptDto request, Guid updatedByUserId, CancellationToken ct = default)
    {
        await ValidateAsync(request);

        // 1. Envia para o Python service antes de persistir no DB.
        //    Se o serviço de análise falhar, não registramos uma versão que nunca entrou em vigor.
        await PushToPythonAsync(request.Content, ct);

        // 2. Persiste o histórico no banco.
        var record = new SystemPrompt
        {
            Content = request.Content,
            CreatedByUserId = updatedByUserId,
            CreatedAt = DateTime.UtcNow,
        };

        await _unitOfWork.SystemPromptRepository.AddAsync(record);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Prompt atualizado pelo usuário {UserId}. Nova versão: {PromptId}",
            updatedByUserId, record.Id);
    }

    private async Task PushToPythonAsync(string content, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("PythonAnalysisService");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("/analyze/prompt", new { prompt = content }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha de comunicação com o Python service ao atualizar o prompt.");
            throw new InvalidOperationException(
                "Não foi possível conectar ao serviço de análise para atualizar o prompt.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Python service retornou {StatusCode} ao atualizar o prompt: {Detail}",
                response.StatusCode, detail);
            throw new InvalidOperationException(
                $"O serviço de análise recusou a atualização do prompt (HTTP {(int)response.StatusCode}).");
        }
    }

    private async Task ValidateAsync(PromptDto request)
    {
        var result = await _validator.ValidateAsync(request);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            throw new ErrorOnValidationException(errors);
        }
    }
}
