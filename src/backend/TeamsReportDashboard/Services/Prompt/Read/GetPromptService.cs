using System.Net.Http.Json;
using System.Text.Json;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Models.PromptDto;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Prompt.Read;

public class GetPromptService : IGetPromptService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GetPromptService> _logger;

    public GetPromptService(
        IUnitOfWork unitOfWork,
        IHttpClientFactory httpClientFactory,
        ILogger<GetPromptService> logger)
    {
        _unitOfWork = unitOfWork;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<PromptResponseDto> ExecuteAsync(CancellationToken ct = default)
    {
        var history = await _unitOfWork.SystemPromptRepository.GetHistoryAsync(10);

        if (history.Count > 0)
        {
            var latest = history[0];
            var historyEntries = history
                .Select(p => new PromptHistoryEntryDto(
                    p.Id,
                    BuildPreview(p.Content),
                    p.CreatedAt,
                    p.CreatedByUser?.Email))
                .ToList();

            return new PromptResponseDto(
                latest.Content,
                latest.CreatedAt,
                latest.CreatedByUser?.Email,
                historyEntries);
        }

        // --- LÓGICA DE AUTO-SEED ---
        // Se o banco estiver vazio, buscamos do Python (prompt.txt) e salvamos no SQL como v1.
        _logger.LogInformation("Nenhum prompt no banco. Sincronizando prompt inicial do serviço de análise para o SQL.");
        
        var content = await FetchFromPythonAsync(ct);

        if (!string.IsNullOrWhiteSpace(content))
        {
            var initialRecord = new SystemPrompt
            {
                Content = content,
                CreatedByUserId = null, // Versão de sistema/inicial
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.SystemPromptRepository.AddAsync(initialRecord);
            await _unitOfWork.SaveChangesAsync(ct);

            return new PromptResponseDto(
                content,
                initialRecord.CreatedAt,
                null,
                History: []);
        }

        return new PromptResponseDto(
            string.Empty,
            LastUpdatedAt: null,
            LastUpdatedBy: null,
            History: []);
    }

    private async Task<string> FetchFromPythonAsync(CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("PythonAnalysisService");
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var response = await client.GetFromJsonAsync<PythonPromptResponse>("/analyze/prompt", options, ct);            
            return response?.Prompt ?? string.Empty;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("O serviço de análise Python não está acessível no momento (localhost:8001). Usando prompt vazio temporariamente.");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao buscar o prompt do Python service.");
            return string.Empty;
        }
    }

    private static string BuildPreview(string content)
    {
        const int maxPreviewLength = 120;
        return content.Length <= maxPreviewLength
            ? content
            : string.Concat(content.AsSpan(0, maxPreviewLength), "…");
    }

    private sealed record PythonPromptResponse(string Prompt);
}
