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
            List<Guid> jobIdsToProcess;

            // Escopo dedicado apenas para buscar e travar os jobs atomicamente.
            // Descartado antes de processar para evitar DbContext compartilhado entre jobs.
            using (var fetchScope = _scopeFactory.CreateScope())
            {
                var fetchUow = fetchScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var pendingJobs = await fetchUow.AnalysisJobRepository.GetPendingJobsAsync(stoppingToken);

                if (!pendingJobs.Any())
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    continue;
                }

                jobIdsToProcess = pendingJobs.Select(j => j.Id).ToList();
                _logger.LogInformation("Travando {Count} job(s) para processamento.", jobIdsToProcess.Count);

                // ExecuteUpdateAsync: atualização atômica sem race conditions (EF Core 7+)
                await fetchUow.AnalysisJobRepository.UpdateJobsStatusAtomicAsync(
                    jobIdsToProcess, JobStatus.Processing, stoppingToken);
            }

            // Um escopo isolado por job garante que um DbContext corrompido de um job
            // não afete o processamento dos demais no mesmo batch.
            foreach (var jobId in jobIdsToProcess)
            {
                using var jobScope = _scopeFactory.CreateScope();
                var unitOfWork = jobScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var orchestrator = jobScope.ServiceProvider.GetRequiredService<IJobResultOrchestrator>();

                var job = await unitOfWork.AnalysisJobRepository.GetByIdAsync(jobId);
                if (job is null)
                {
                    _logger.LogWarning("Job {JobId} não encontrado ao tentar processar. Ignorando.", jobId);
                    continue;
                }

                job.Status = JobStatus.Processing; // sincroniza estado em memória com o DB

                try
                {
                    await orchestrator.SyncAndProcessJobResultAsync(job, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Shutdown solicitado — devolver o job atual para a fila e sair
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