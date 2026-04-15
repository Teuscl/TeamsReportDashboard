// Local: TeamsReportDashboard.Backend/Services/ProcessCompletedJob/ReportProcessorService.cs

using System.Text.Json;
using System.Text.RegularExpressions;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Models.PythonApiDto;
using TeamsReportDashboard.Backend.Models.ReportDto;
using TeamsReportDashboard.Backend.Services;
using TeamsReportDashboard.Backend.Services.AnalysisJob.ProcessCompletedJob;
using TeamsReportDashboard.Backend.Services.Report.Create;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.ProcessCompletedJob
{
    
     public class ReportProcessorService : IReportProcessorService
    {
        private readonly ICreateReportService _createReportService;
        private readonly ILogger<ReportProcessorService> _logger;
        private readonly IUnitOfWork _unitOfWork; // << NOVO: Injetando o DbContext
        
        public ReportProcessorService(
            ICreateReportService createReportService, 
            ILogger<ReportProcessorService> logger,
            IUnitOfWork unitOfWork) // << NOVO: Adicionado aqui
        {
            _createReportService = createReportService;
            _logger = logger;
            _unitOfWork = unitOfWork; // << NOVO: Atribuição
        }

        public async Task ProcessAnalysisResult(Entities.AnalysisJob job, PythonApiDto.PythonResultResponse result)
        {
            // Caso: batch concluído na OpenAI mas sem output_file_id (todas as requisições falharam
            // ou o arquivo não foi gerado). Deve salvar o job para não ficar preso em Processing.
            if (result.Results is null)
            {
                _logger.LogWarning(
                    "Job {JobId}: API Python retornou 'results' nulo. O batch pode ter concluído sem output_file_id.",
                    job.Id);
                job.ErrorMessage =
                    "O lote da OpenAI foi concluído mas não gerou relatórios (campo 'results' ausente). " +
                    "Verifique a dashboard da OpenAI para detalhes do batch_id.";
                _unitOfWork.AnalysisJobRepository.Update(job);
                await _unitOfWork.SaveChangesAsync();
                return;
            }

            List<AtendimentoContainerDto> containers;
            try
            {
                var resultsJson = JsonSerializer.Serialize(result.Results);
                containers = JsonSerializer.Deserialize<List<AtendimentoContainerDto>>(resultsJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job {JobId}: Falha ao deserializar 'results'.", job.Id);
                job.ErrorMessage = "Erro de sistema: falha ao deserializar o resultado do batch.";
                _unitOfWork.AnalysisJobRepository.Update(job);
                await _unitOfWork.SaveChangesAsync();
                return;
            }

            if (containers == null || !containers.Any())
            {
                _logger.LogWarning("Job {JobId}: Nenhum container de atendimento encontrado no resultado.", job.Id);
                job.ErrorMessage = "O lote foi processado mas não retornou atendimentos válidos para salvar.";
                _unitOfWork.AnalysisJobRepository.Update(job);
                await _unitOfWork.SaveChangesAsync();
                return;
            }
            
            var allAtendimentos = containers
                .Where(c => c.Atendimentos != null && c.Atendimentos.Any())
                .SelectMany(c => c.Atendimentos)
                .ToList();
            
            _logger.LogInformation($"Job {job.Id}: Encontrados {allAtendimentos.Count} atendimentos para processar.");

            int successCount = 0;
            int failureCount = 0;
            
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var atendimento in allAtendimentos)
                {
                    try
                    {
                        // ... (lógica de parse e criação do createReportDto permanece a mesma) ...
                        if (!DateTime.TryParse($"{atendimento.DataSolicitacao} {atendimento.HoraPrimeiraMensagem}",
                                out var requestDate))
                        {
                            _logger.LogWarning(
                                $"Job {job.Id}: Data/hora inválida para '{atendimento.EmailSolicitante}'. Pulando.");
                            failureCount++;
                            continue;
                        }

                        var firstResponseTime = ParseTimeSpanRobust(atendimento.TempoPrimeiraResposta);
                        var handlingTime = TimeSpan.FromMinutes(atendimento.TempoTotalAtendimento);
                        var createReportDto = new CreateReportDto
                        {
                            RequesterName = atendimento.QuemSolicitouAtendimento,
                            RequesterEmail = atendimento.EmailSolicitante,
                            TechnicianName = atendimento.QuemRespondeu,
                            ReportedProblem = atendimento.ProblemaRelatado,
                            Category = atendimento.Categoria,
                            RequestDate = requestDate,
                            FirstResponseTime = firstResponseTime,
                            AverageHandlingTime = handlingTime,
                            AnalysisJobId = job.Id
                        };

                        // A chamada para o serviço de criação não muda
                        await _createReportService.Execute(createReportDto);
                        successCount++;
                    }
                    catch (ErrorOnValidationException valEx)
                    {
                        failureCount++;
                        // Este log agora mostrará exatamente quais campos falharam na validação!
                        var errorString = string.Join("; ", valEx.GetErrorMessages());
                        _logger.LogError("FALHA DE VALIDAÇÃO para '{Email}': {ValidationErrors}",
                            atendimento.EmailSolicitante, errorString);
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _logger.LogError(ex, "FALHA CRÍTICA ao processar atendimento para '{Email}' do Job {JobId}.",
                            atendimento.EmailSolicitante, job.Id);
                    }
                }

                job.ErrorMessage = failureCount > 0
                    ? $"Processamento concluído com {failureCount} falhas de um total de {allAtendimentos.Count}."
                    : null;

                // Job e relatórios são salvos e commitados atomicamente na mesma transação.
                _unitOfWork.AnalysisJobRepository.Update(job);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation($"Job {job.Id}: Transação comitada com sucesso. {successCount} relatórios salvos.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();

                job.ErrorMessage = "Falha no processamento em lote. Nenhum relatório foi salvo. Verifique os logs.";
                _logger.LogError(ex, "Falha crítica no processamento do job {JobId}. Transação revertida.", job.Id);

                // Salva o status de erro do job fora da transação revertida.
                _unitOfWork.AnalysisJobRepository.Update(job);
                await _unitOfWork.SaveChangesAsync();
            }
            
            
        }
        
        // ... (A função ParseTimeSpanRobust permanece a mesma) ...
        private TimeSpan ParseTimeSpanRobust(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return TimeSpan.Zero;
            if (TimeSpan.TryParse(input, out var timeSpan)) return timeSpan;
            var numbers = Regex.Matches(input, @"\d+").OfType<Match>().Select(m => int.Parse(m.Value)).ToList();
            if (numbers.Count == 0) return TimeSpan.Zero;
            if (numbers.Count == 1) return TimeSpan.FromSeconds(numbers[0]);
            if (numbers.Count == 2) return new TimeSpan(0, numbers[0], numbers[1]);
            if (numbers.Count >= 3) return new TimeSpan(numbers[0], numbers[1], numbers[2]);
            return TimeSpan.Zero;
        }
    }
   
}