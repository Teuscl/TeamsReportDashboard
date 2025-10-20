using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Entities.Enums;
using TeamsReportDashboard.Backend.Interfaces;
using TeamsReportDashboard.Backend.Services.AnalysisJob.JobSynchronization;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.AnalysisJob
{
    /// <summary>
    /// Um serviço de background que atua como um "trabalhador" (worker),
    /// processando a fila de jobs de análise de forma autônoma e robusta.
    /// </summary>
    public class AnalysisJobWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AnalysisJobWorker> _logger;

        public AnalysisJobWorker(IServiceScopeFactory scopeFactory, ILogger<AnalysisJobWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Analysis Job Worker iniciado em {time}.", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Espera 30 segundos entre cada ciclo de verificação.
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                using (var scope = _scopeFactory.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var orchestrator = scope.ServiceProvider.GetRequiredService<IJobResultOrchestrator>();
                    
                    // PASSO 1: BUSCAR
                    // Pega a lista de jobs que estão na fila esperando para serem processados.
                    var jobsToProcess = await unitOfWork.AnalysisJobRepository.GetPendingJobsAsync();
                    if (!jobsToProcess.Any())
                    {
                        continue; // Se não há jobs, volta a dormir.
                    }

                    // PASSO 2: TRAVAR (LOCK)
                    // Muda o status para "Processing" ANTES de fazer qualquer trabalho pesado.
                    // Isso garante que nenhum outro ciclo deste worker pegue os mesmos jobs.
                    _logger.LogInformation("Travando {Count} job(s) para verificação de status.", jobsToProcess.Count);
                    jobsToProcess.ForEach(job => job.Status = JobStatus.Processing);
                    await unitOfWork.SaveChangesAsync();

                    // PASSO 3: EXECUTAR
                    // Agora, iteramos sobre a lista de jobs que já estão "travados" e delegamos o trabalho.
                    _logger.LogInformation("Iniciando a verificação dos jobs travados.");
                    foreach (var job in jobsToProcess)
                    {
                        try
                        {
                            // Delega toda a lógica complexa para o orquestrador.
                            // A responsabilidade do worker é apenas gerenciar a fila.
                            await orchestrator.SyncAndProcessJobResultAsync(job);
                        }
                        catch (Exception ex)
                        {
                            // Se ocorrer uma exceção inesperada durante a orquestração,
                            // o job é devolvido para a fila para ser tentado novamente.
                            _logger.LogError(ex, "Falha crítica ao orquestrar o Job {JobId}. Retornando para a fila.", job.Id);
                            job.Status = JobStatus.Pending; 
                            job.ErrorMessage = $"Erro inesperado no worker: {ex.Message}";
                            await unitOfWork.SaveChangesAsync();
                        }
                    }
                }
            }
        }
    }
}