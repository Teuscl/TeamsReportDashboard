using System.Text.Json;
using TeamsReportDashboard.Backend.Entities.Enums;
using TeamsReportDashboard.Backend.Models.PythonApiDto;
using TeamsReportDashboard.Backend.Services.AnalysisJob.ProcessCompletedJob;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.AnalysisJob.JobSynchronization;

public class JobResultOrchestrator : IJobResultOrchestrator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IReportProcessorService _reportProcessor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<JobResultOrchestrator> _logger;
    
    public JobResultOrchestrator(
        IHttpClientFactory httpClientFactory,
        IReportProcessorService reportProcessor,
        IUnitOfWork unitOfWork,
        ILogger<JobResultOrchestrator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _reportProcessor = reportProcessor;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SyncAndProcessJobResultAsync(Entities.AnalysisJob job, CancellationToken ct = default)
    {
        var pythonApiClient = _httpClientFactory.CreateClient("PythonAnalysisService");
        var response = await pythonApiClient.GetAsync($"/analyze/results/{job.PythonBatchId}", ct);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PythonApiDto.PythonResultResponse>(cancellationToken: ct);

        // Guard: corpo de resposta nulo não deveria ocorrer com EnsureSuccessStatusCode,
        // mas protege contra deserialização vazia.
        if (result is null)
        {
            _logger.LogError("Job {JobId}: API Python retornou corpo de resposta nulo para o batch '{BatchId}'.", job.Id, job.PythonBatchId);
            job.Status = JobStatus.Failed;
            job.ErrorMessage = "A API de análise retornou uma resposta vazia.";
            _unitOfWork.AnalysisJobRepository.Update(job);
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        var internalStatus = ParseStatusFromApi(result.Status);

        switch (internalStatus)
        {
            case JobStatus.Completed:
                _logger.LogInformation("Job {JobId} concluído pela API Python. Iniciando processamento dos relatórios.", job.Id);
                job.Status = JobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                job.ResultData = JsonSerializer.Serialize(result);
                job.ErrorMessage = null;
                // ProcessAnalysisResult é responsável por persistir job + relatórios
                // atomicamente. Se retornar sem salvar, o job fica preso — veja ReportProcessorService.
                await _reportProcessor.ProcessAnalysisResult(job, result);
                break;

            case JobStatus.Failed:
                _logger.LogWarning("Job {JobId} falhou/expirou na API Python. Status: '{ApiStatus}'.", job.Id, result.Status);
                job.Status = JobStatus.Failed;
                job.CompletedAt = DateTime.UtcNow;
                job.ErrorMessage = result.Errors ?? $"OpenAI Batch API marcou o job como '{result.Status}'.";
                _unitOfWork.AnalysisJobRepository.Update(job);
                await _unitOfWork.SaveChangesAsync(ct);
                break;

            default:
                _logger.LogInformation("Job {JobId} ainda em andamento na API (status: '{ApiStatus}'). Retornando para a fila.", job.Id, result.Status);
                job.Status = JobStatus.Pending;
                _unitOfWork.AnalysisJobRepository.Update(job);
                await _unitOfWork.SaveChangesAsync(ct);
                break;
        }
    }

    /// <summary>
    /// Mapeia os status retornados pela OpenAI Batch API para o enum interno.
    /// Status possíveis da OpenAI: validating, in_progress, finalizing, completed,
    /// expired, cancelling, cancelled, failed.
    /// </summary>
    private static JobStatus ParseStatusFromApi(string? apiStatus) =>
        apiStatus?.ToLowerInvariant() switch
        {
            "completed" => JobStatus.Completed,
            // "expired": OpenAI encerra jobs que excedem a janela de 24h — deve ser tratado como falha,
            // não como pendente, caso contrário o worker fica consultando para sempre.
            "failed" or "cancelled" or "expired" => JobStatus.Failed,
            // validating, in_progress, finalizing, cancelling → ainda em andamento
            _ => JobStatus.Pending
        };
}