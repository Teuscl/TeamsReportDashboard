// Em uma pasta Services/
using System.Text.Json;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Models;
using TeamsReportDashboard.Backend.Models.PythonApiDto;
using TeamsReportDashboard.Backend.Models.ReportDto;
using TeamsReportDashboard.Backend.Services.ProcessCompletedJob;
using TeamsReportDashboard.Backend.Services.Report.Create;
using TeamsReportDashboard.Services.User.Create;

public class ReportProcessorService : IReportProcessorService
{
    private readonly ICreateReportService _createReportService;
    private readonly ILogger<ReportProcessorService> _logger;

    public ReportProcessorService(ICreateReportService createReportService, ILogger<ReportProcessorService> logger)
    {
        _createReportService = createReportService;
        _logger = logger;
    }

    public async Task ProcessAnalysisResult(AnalysisJob job, PythonApiDto.PythonResultResponse result)
    {
        if (result.Results is null)
        {
            _logger.LogWarning($"Job {job.Id} sendo processado, mas a propriedade 'results' está nula.");
            return;
        }

        var resultJson = JsonSerializer.Serialize(result.Results);
        var containers = JsonSerializer.Deserialize<List<AtendimentoContainerDto>>(resultJson);

        if (containers == null || !containers.Any())
        {
            _logger.LogWarning($"Job {job.Id} processado, mas não foi possível deserializar ou não há contêineres de atendimento.");
            return;
        }

        int successCount = 0;
        int failureCount = 0;
        var allAtendimentos = containers.SelectMany(c => c.Atendimentos ?? new List<AtendimentoDto>()).ToList();
        
        _logger.LogInformation($"Job {job.Id}: Encontrados {allAtendimentos.Count} atendimentos para processar.");

        foreach (var atendimento in allAtendimentos)
        {
            try
            {
                // Mapeamento e conversão de dados
                DateTime.TryParse($"{atendimento.DataSolicitacao} {atendimento.HoraPrimeiraMensagem}", out var requestDate);
                TimeSpan.TryParse(atendimento.TempoPrimeiraResposta, out var firstResponseTime);
                TimeSpan.TryParse(atendimento.TempoTotalAtendimento, out var handlingTime);
                
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
                
                // Chamando seu serviço de negócio
                await _createReportService.Execute(createReportDto);
                successCount++;
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(ex, $"FALHA ao processar atendimento para '{atendimento.EmailSolicitante}' do Job {job.Id}.");
            }
        }
        
        _logger.LogInformation($"Processamento do Job {job.Id} finalizado. Relatórios criados: {successCount}. Falhas: {failureCount}.");
        // Opcional: Atualizar o job com um resumo, se necessário.
    }
}