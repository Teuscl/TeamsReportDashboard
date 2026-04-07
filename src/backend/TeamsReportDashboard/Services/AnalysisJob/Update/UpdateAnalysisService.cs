using FluentValidation;
using TeamsReportDashboard.Backend.Entities.Enums;
using TeamsReportDashboard.Backend.Models.Job;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.AnalysisJob.Update;

public class UpdateAnalysisService : IUpdateAnalysisService
{
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<UpdateAnalysisJobDto> _validator;
        private readonly ILogger<UpdateAnalysisService> _logger;

        public UpdateAnalysisService(IUnitOfWork unitOfWork, IValidator<UpdateAnalysisJobDto> validator, ILogger<UpdateAnalysisService> logger)
        {
            _unitOfWork = unitOfWork;
            _validator = validator;
            _logger = logger;
        }

        public async Task ExecuteAsync(Guid jobId, UpdateAnalysisJobDto dto)
        {
            // 1. Validar os dados de entrada (o DTO)
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Falha na validação ao tentar atualizar o job {JobId}. Erros: {Errors}", jobId, string.Join(", ", errors));
                throw new ErrorOnValidationException(errors);
            }

            // 2. Buscar a entidade no banco de dados
            var job = await _unitOfWork.AnalysisJobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                _logger.LogWarning("Tentativa de atualizar um job não existente com ID: {JobId}", jobId);
                throw new KeyNotFoundException("Job de análise não encontrado.");
            }

            // 3. Aplicar a regra de negócio: Não permitir edição de jobs em andamento ou já concluídos.
            //    Isso evita que o nome de um job seja alterado enquanto ele está sendo processado.
            if (job.Status == JobStatus.Processing)
            {
                _logger.LogWarning("Tentativa de alterar o job {JobId} que possui o status '{Status}', o que não é permitido.", job.Id, job.Status);
                throw new InvalidOperationException($"Não é possível alterar o job, pois seu status é '{job.Status}");
            }

            // 4. Atualizar o nome da entidade com o valor do DTO
            _logger.LogInformation("Atualizando o nome do job {JobId} de '{OldName}' para '{NewName}'.", job.Id, job.Name, dto.Name);
            job.Name = dto.Name;

            _unitOfWork.AnalysisJobRepository.Update(job);

            // 5. Persistir a alteração no banco de dados
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Job {JobId} atualizado com sucesso.", job.Id);
        }
}
