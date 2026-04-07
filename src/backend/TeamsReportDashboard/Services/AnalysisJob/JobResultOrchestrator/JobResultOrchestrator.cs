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
        var internalStatus = ParseStatusFromApi(result?.Status);

        switch (internalStatus)
        {
            case JobStatus.Completed:
                _logger.LogInformation("Job {JobId} concluído.", job.Id);
                job.Status = JobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                job.ResultData = JsonSerializer.Serialize(result);
                job.ErrorMessage = null;
                await _unitOfWork.SaveChangesAsync(ct);
                await _reportProcessor.ProcessAnalysisResult(job, result);
                break;
            case JobStatus.Failed:
                _logger.LogWarning("Job {JobId} falhou na API Python.", job.Id);
                job.Status = JobStatus.Failed;
                job.CompletedAt = DateTime.UtcNow;
                job.ResultData = result?.Errors ?? $"API Python marcou job como '{result?.Status}'.";
                await _unitOfWork.SaveChangesAsync(ct);
                break;
            default:
                _logger.LogInformation("Job {JobId} ainda em andamento na API. Retornando para a fila.", job.Id);
                job.Status = JobStatus.Pending;
                await _unitOfWork.SaveChangesAsync(ct);
                break;
        }
    }

    private static JobStatus ParseStatusFromApi(string? apiStatus) =>
        apiStatus?.ToLowerInvariant() switch
        {
            "completed" => JobStatus.Completed,
            "failed" or "cancelled" => JobStatus.Failed,
            _ => JobStatus.Pending
        };
}