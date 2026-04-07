using TeamsReportDashboard.Backend.Entities.Enums;
using TeamsReportDashboard.Backend.Services.AnalysisJob.JobSynchronization;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.AnalysisJob;

public class AnalysisJobWorker : BackgroundService
{
    private const int MaxRetries = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnalysisJobWorker> _logger;

    public AnalysisJobWorker(IServiceScopeFactory scopeFactory, ILogger<AnalysisJobWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Analysis Job Worker iniciado em {Time}.", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var orchestrator = scope.ServiceProvider.GetRequiredService<IJobResultOrchestrator>();

            var jobsToProcess = await unitOfWork.AnalysisJobRepository
                .GetPendingJobsAsync(stoppingToken);

            if (!jobsToProcess.Any())
                continue;

            _logger.LogInformation("Travando {Count} job(s) para processamento.", jobsToProcess.Count);
            jobsToProcess.ForEach(job => job.Status = JobStatus.Processing);
            await unitOfWork.SaveChangesAsync(stoppingToken);

            foreach (var job in jobsToProcess)
            {
                try
                {
                    await orchestrator.SyncAndProcessJobResultAsync(job, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Shutdown solicitado — devolver o job para a fila e sair
                    job.Status = JobStatus.Pending;
                    await unitOfWork.SaveChangesAsync(CancellationToken.None);
                    return;
                }
                catch (Exception ex)
                {
                    job.RetryCount++;
                    job.ErrorMessage = $"Tentativa {job.RetryCount}: {ex.Message}";

                    if (job.RetryCount >= MaxRetries)
                    {
                        _logger.LogError(ex,
                            "Job {JobId} atingiu o limite de {MaxRetries} tentativas. Marcando como Failed.",
                            job.Id, MaxRetries);
                        job.Status = JobStatus.Failed;
                    }
                    else
                    {
                        _logger.LogWarning(ex,
                            "Falha no Job {JobId} (tentativa {Retry}/{MaxRetries}). Retornando para a fila.",
                            job.Id, job.RetryCount, MaxRetries);
                        job.Status = JobStatus.Pending;
                    }

                    await unitOfWork.SaveChangesAsync(stoppingToken);
                }
            }
        }
    }
}