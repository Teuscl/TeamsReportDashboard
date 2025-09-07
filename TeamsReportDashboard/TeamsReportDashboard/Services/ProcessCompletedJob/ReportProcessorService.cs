// Local: TeamsReportDashboard.Backend/Services/ProcessCompletedJob/ReportProcessorService.cs

using System.Text.Json;
using System.Text.RegularExpressions;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Models.PythonApiDto;
using TeamsReportDashboard.Backend.Models.ReportDto;
using TeamsReportDashboard.Backend.Services;
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

        public async Task ProcessAnalysisResult(AnalysisJob job, PythonApiDto.PythonResultResponse result)
        {
            if (result.Results is null)
            {
                _logger.LogWarning($"Job {job.Id}: Propriedade 'results' está nula.");
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
                _logger.LogError(ex, $"Job {job.Id}: Falha ao deserializar 'results'.");
                job.ErrorMessage = "Erro de sistema: Falha ao deserializar resultado.";
                _unitOfWork.AnalysisJobRepository.Update(job); // << ALTERADO: Usando o _context injetado
                await _unitOfWork.SaveChangesAsync();
                return;
            }

            if (containers == null || !containers.Any())
            {
                _logger.LogWarning($"Job {job.Id}: Não há contêineres de atendimento para processar.");
                job.ErrorMessage = null;
                _unitOfWork.AnalysisJobRepository.Update(job); // << ALTERADO: Usando o _context injetado
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
                            AverageHandlingTime = handlingTime
                        };

                        Console.WriteLine("===== Relatório de Atendimento =====");
                        Console.WriteLine($"Solicitante: {createReportDto.RequesterName}");
                        Console.WriteLine($"Email do Solicitante: {createReportDto.RequesterEmail}");
                        Console.WriteLine($"Técnico Responsável: {createReportDto.TechnicianName}");
                        Console.WriteLine($"Problema Reportado: {createReportDto.ReportedProblem}");
                        Console.WriteLine($"Categoria: {createReportDto.Category}");
                        Console.WriteLine($"Data da Solicitação: {createReportDto.RequestDate}");
                        Console.WriteLine($"Tempo para Primeira Resposta: {createReportDto.FirstResponseTime}");
                        Console.WriteLine($"Tempo Médio de Atendimento: {createReportDto.AverageHandlingTime}");
                        Console.WriteLine("====================================");
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
                // Se chegamos até aqui, o loop terminou.
                // Agora vamos comitar todas as inserções de relatórios de uma vez.
                job.ErrorMessage = failureCount > 0 
                    ? $"Processamento concluído com {failureCount} falhas de um total de {allAtendimentos.Count}." 
                    : null;

                _unitOfWork.AnalysisJobRepository.Update(job);

                // ✅ PASSO CRÍTICO: ADICIONAR O COMMIT AQUI!
                await _unitOfWork.CommitAsync();

                _logger.LogInformation($"Job {job.Id}: Transação comitada com sucesso. {successCount} relatórios salvos.");

            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                
                job.ErrorMessage = "Falha no processamento em lote. Nenhum relatório foi salvo. Verifique os logs.";
            }
            finally
            {
                // PASSO 4 (FINALLY): Garante que o status do Job seja sempre atualizado.
                // Esta operação ocorre fora da transação principal, salvando o resultado final do job.
                _logger.LogInformation("Atualizando status final do Job {JobId}.", job.Id);
                // Usamos o novo repositório para atualizar o job.
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