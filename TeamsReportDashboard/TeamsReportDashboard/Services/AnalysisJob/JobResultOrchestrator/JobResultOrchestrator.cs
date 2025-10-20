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

    public async Task SyncAndProcessJobResultAsync(Entities.AnalysisJob job)
    {
        var pythonApiClient = _httpClientFactory.CreateClient("PythonAnalysisService");
        var response = await pythonApiClient.GetAsync($"/analyze/results/{job.PythonBatchId}");
        
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PythonApiDto.PythonResultResponse>();
        var internalStatus = ParseStatusFromApi(result?.Status);

        switch (internalStatus)
        {
            case JobStatus.Completed:
                _logger.LogInformation($"Job {job.Id} is completed.");
                job.Status = JobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                job.ResultData = JsonSerializer.Serialize(result);
                job.ErrorMessage = null;
                await _unitOfWork.SaveChangesAsync();
                await _reportProcessor.ProcessAnalysisResult(job, result);
                break;
            case JobStatus.Failed:
                _logger.LogWarning($"Job {job.Id} is failed.");
                job.Status = JobStatus.Failed;
                job.CompletedAt = DateTime.UtcNow;
                job.ResultData = result?.Errors ?? $"API Python marcou job como '{result?.Status}'.";
                await _unitOfWork.SaveChangesAsync();
                break;
            default: // Pending ou desconhecido
                _logger.LogInformation("Job {JobId} ainda em andamento na API. Retornando para a fila.", job.Id);
                job.Status = JobStatus.Pending; // Devolve para a fila
                await _unitOfWork.SaveChangesAsync();
                break;
        }

    }

    private object ParseStatusFromApi(string? apiStatus)
    {
        return apiStatus?.ToLowerInvariant() switch
        {
            "completed" => JobStatus.Completed,
            "failed" => JobStatus.Failed,
            "cancelled" => JobStatus.Failed,
            _ => JobStatus.Pending
        };
    }
}