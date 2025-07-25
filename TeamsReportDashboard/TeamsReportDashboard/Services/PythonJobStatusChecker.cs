using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Entities.Enums;
using TeamsReportDashboard.Backend.Models.PythonApiDto;
using TeamsReportDashboard.Backend.Services.ProcessCompletedJob;

namespace TeamsReportDashboard.Backend.Services;

public class PythonJobStatusChecker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PythonJobStatusChecker> _logger;

    public PythonJobStatusChecker(IServiceScopeFactory factory, IHttpClientFactory clientFactory, ILogger<PythonJobStatusChecker> logger)
    {
        _scopeFactory = factory;
        _httpClientFactory = clientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de Verificação de Jobs da IA iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Espera antes de começar a trabalhar

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var pythonApiClient = _httpClientFactory.CreateClient("PythonAnalysisService");
                var reportProcessor = scope.ServiceProvider.GetRequiredService<IReportProcessorService>();

                var pendingJobs = await dbContext.AnalysisJobs
                    .Where(j => j.Status == JobStatus.Pending)
                    .ToListAsync(stoppingToken);

                if (!pendingJobs.Any()) continue;
                
                _logger.LogInformation($"Verificando status de {pendingJobs.Count} job(s) pendente(s).");

                foreach (var job in pendingJobs)
                {
                    try
                    {
                        var response = await pythonApiClient.GetAsync($"/analyze/results/{job.PythonBatchId}", stoppingToken);
                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogWarning($"Falha ao buscar status do job {job.Id}. API Python respondeu com {response.StatusCode}.");
                            continue;
                        }

                        var result = await response.Content.ReadFromJsonAsync<PythonApiDto.PythonResultResponse>(cancellationToken: stoppingToken);

                        if (result?.Status?.ToLower() == "completed")
                        {
                            // =================================================================
                            //  FLUXO CORRIGIDO: PRIMEIRO SALVA, DEPOIS PROCESSA
                            // =================================================================

                            // PASSO 1: SALVAR O RESULTADO IMEDIATAMENTE
                            _logger.LogInformation($"Job {job.Id} CONCLUÍDO na API Python. Salvando resultado no banco...");
                            job.Status = JobStatus.Completed;
                            job.CompletedAt = DateTime.UtcNow;
                            job.ResultData = JsonSerializer.Serialize(result); // A linha crucial!
                            job.ErrorMessage = null;

                            dbContext.Update(job);
                            // Salva o estado imediatamente para garantir que o resultado não se perca.
                            await dbContext.SaveChangesAsync(stoppingToken); 
                            _logger.LogInformation($"Resultado do Job {job.Id} salvo com sucesso.");

                            // PASSO 2: TENTAR PROCESSAR OS DADOS (AGORA SEPARADAMENTE)
                            _logger.LogInformation($"Iniciando processamento para criar relatórios do Job {job.Id}...");
                            try
                            {
                                await reportProcessor.ProcessAnalysisResult(job, result);
                                _logger.LogInformation($"Processamento dos relatórios do Job {job.Id} finalizado com sucesso.");
                            }
                            catch (Exception procEx)
                            {
                                // Se o processamento falhar, o job já está salvo como "Completed" com os dados.
                                // Apenas registramos o erro de processamento.
                                _logger.LogError(procEx, $"Ocorreu um erro DURANTE o processamento dos dados do Job {job.Id}. Os dados brutos foram salvos, mas os relatórios podem não ter sido criados.");
                                job.ErrorMessage = "Falha na criação dos relatórios a partir do resultado. Verifique os logs.";
                                dbContext.Update(job);
                                await dbContext.SaveChangesAsync(stoppingToken); // Salva a mensagem de erro
                            }
                        }
                        else if (result?.Status?.ToLower() == "failed" || result?.Status?.ToLower() == "cancelled")
                        {
                            // ... (lógica para jobs que falham no lado do Python) ...
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        // Apenas registra que não conseguiu se conectar, mas NÃO muda o status do job.
                        // Ele continuará como "Pending" e será tentado novamente no próximo ciclo.
                        _logger.LogWarning(httpEx, $"Não foi possível conectar à API Python para verificar o job {job.Id}. Tentando novamente em breve.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Exceção fatal ao processar o job {job.Id}.");
                        job.Status = JobStatus.Failed;
                        job.ErrorMessage = $"Erro crítico no checker: {ex.Message}";
                        job.CompletedAt = DateTime.UtcNow;
                        dbContext.Update(job);
                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                }
            }
        }
    }
}