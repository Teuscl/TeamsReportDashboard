using System.Net.Http.Json;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Extensions;

public static class PromptSyncExtensions
{
    public static async Task SyncPromptToPythonAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var unitOfWork = services.GetRequiredService<IUnitOfWork>();
            var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();

            var latestPrompt = await unitOfWork.SystemPromptRepository.GetLatestAsync();

            if (latestPrompt != null)
            {
                logger.LogInformation("Sincronizando prompt do banco para o serviço de análise no startup.");
                
                var client = httpClientFactory.CreateClient("PythonAnalysisService");
                var response = await client.PostAsJsonAsync("/analyze/prompt", new { prompt = latestPrompt.Content });

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("Prompt sincronizado com sucesso.");
                }
                else
                {
                    logger.LogWarning("Falha ao sincronizar prompt no startup: {StatusCode}", response.StatusCode);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado ao sincronizar prompt no startup.");
        }
    }
}
