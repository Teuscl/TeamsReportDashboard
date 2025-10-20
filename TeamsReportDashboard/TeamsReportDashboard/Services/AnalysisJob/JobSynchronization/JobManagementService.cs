using System.Text.Json;
using TeamsReportDashboard.Backend.Entities.Enums;
using TeamsReportDashboard.Backend.Interfaces;
using TeamsReportDashboard.Backend.Models.PythonApiDto;
using TeamsReportDashboard.Backend.Models.ReprocessResponseDto;
using TeamsReportDashboard.Backend.Services.AnalysisJob.JobSynchronization;
using TeamsReportDashboard.Backend.Services.AnalysisJob.ProcessCompletedJob;
using TeamsReportDashboard.Backend.Services.JobSynchronization;
using TeamsReportDashboard.Backend.Services.ProcessCompletedJob;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.AnalysisJob.JobSynchronization
{
    public class JobManagementService : IJobManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJobResultOrchestrator _orchestrator;
        private readonly IReportProcessorService _reportProcessor;
        private readonly ILogger<JobManagementService> _logger;

        // Note as dependências corrigidas: sai AppDbContext, entra IUnitOfWork e IJobResultOrchestrator
        public JobManagementService(
            IUnitOfWork unitOfWork,
            IJobResultOrchestrator orchestrator,
            IReportProcessorService reportProcessor,
            ILogger<JobManagementService> logger)
        {
            _unitOfWork = unitOfWork;
            _orchestrator = orchestrator;
            _reportProcessor = reportProcessor;
            _logger = logger;
        }

        public async Task<ReprocessResponseDto> ReprocessJobAsync(Guid jobId)
        {
            // Usa o repositório para consistência arquitetural
            var job = await _unitOfWork.AnalysisJobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                // Lança uma exceção mais específica para o controller retornar 404 Not Found
                throw new KeyNotFoundException("Job de análise não encontrado.");
            }

            // CASO 1: Reprocessar localmente os relatórios de um job já concluído.
            // Útil quando a lógica de criação de relatórios é alterada e precisa ser re-executada.
            if (job.Status == JobStatus.Completed && !string.IsNullOrEmpty(job.ResultData))
            {
                _logger.LogInformation("Iniciando reprocessamento local dos relatórios do job {JobId}", job.Id);
                var storedResult = JsonSerializer.Deserialize<PythonApiDto.PythonResultResponse>(job.ResultData);
                await _reportProcessor.ProcessAnalysisResult(job, storedResult!);
                return new ReprocessResponseDto { Message = "Reprocessamento local dos relatórios concluído com sucesso." };
            }

            // CASO 2: Forçar uma nova sincronização com a API externa para qualquer outro status.
            // Delega a lógica complexa para o orquestrador, eliminando duplicação de código.
            _logger.LogInformation("Forçando sincronização do job {JobId} com a API externa.", job.Id);
            await _orchestrator.SyncAndProcessJobResultAsync(job);
            return new ReprocessResponseDto { Message = $"Sincronização forçada para o job '{job.Name}' foi concluída." };
        }
    }
}