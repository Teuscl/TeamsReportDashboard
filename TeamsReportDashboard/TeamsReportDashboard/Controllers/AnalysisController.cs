using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Entities.Enums; // Substitua pelo seu namespace
using TeamsReportDashboard.Backend.Models;
using TeamsReportDashboard.Backend.Models.PythonApiDto;
using TeamsReportDashboard.Backend.Services.ProcessCompletedJob; // Substitua pelo seu namespace

[ApiController]
[Route("[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _context;

    public AnalysisController(IHttpClientFactory httpClientFactory, AppDbContext context)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
    }

    [HttpPost("start")]
    [RequestSizeLimit(200 * 1024 * 1024)] // Limite de 200 MB
    public async Task<IActionResult> StartAnalysis(IFormFile file)
    {
        if (file == null || !file.FileName.ToLower().EndsWith(".zip")) return BadRequest("Arquivo .zip é obrigatório.");
        var pythonApiClient = _httpClientFactory.CreateClient("PythonAnalysisService");
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(file.OpenReadStream()), "file", file.FileName);
        HttpResponseMessage pythonResponse;
        try { pythonResponse = await pythonApiClient.PostAsync("/analyze/start", content); }
        catch (HttpRequestException ex) { return StatusCode(503, $"Serviço de análise indisponível: {ex.Message}"); }
        if (!pythonResponse.IsSuccessStatusCode) return StatusCode((int)pythonResponse.StatusCode, $"Erro na API Python: {await pythonResponse.Content.ReadAsStringAsync()}");
        
        var startResponse = await pythonResponse.Content.ReadFromJsonAsync<PythonApiDto.PythonStartResponse>();
        if (string.IsNullOrEmpty(startResponse?.BatchId)) return StatusCode(500, "API Python não retornou um BatchId válido.");
        
        var newJob = new AnalysisJob { Id = Guid.NewGuid(), PythonBatchId = startResponse.BatchId, Status = JobStatus.Pending, CreatedAt = DateTime.UtcNow };
        _context.AnalysisJobs.Add(newJob);
        await _context.SaveChangesAsync();
        
        return Accepted(new { JobId = newJob.Id });
    }

    [HttpGet("status/{jobId}")]
    public async Task<IActionResult> GetJobStatus(Guid jobId)
    {
        var job = await _context.AnalysisJobs.FindAsync(jobId);
        if (job == null) return NotFound();
        return Ok(new { job.Id, Status = job.Status.ToString(), job.CreatedAt, job.CompletedAt, job.ErrorMessage });
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAllJobs()
    {
        var jobs = await _context.AnalysisJobs
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new 
            {
                j.Id,
                Status = j.Status.ToString(),
                j.CreatedAt,
                j.CompletedAt,
                j.ErrorMessage
            })
            .ToListAsync();

        return Ok(jobs);
    }
    
    [HttpPost("reprocess/{jobId}")]
    [Authorize(Roles = "Admin, Master")]
    public async Task<IActionResult> ReprocessJob(Guid jobId)
    {
        var job = await _context.AnalysisJobs.FindAsync(jobId);
        if (job == null)
        {
            return NotFound("Job não encontrado.");
        }

        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AnalysisController>>();

        // CENÁRIO 1: O job foi concluído, mas o processamento local dos relatórios falhou.
        if (job.Status == JobStatus.Completed && !string.IsNullOrEmpty(job.ResultData))
        {
            logger.LogInformation($"Iniciando reprocessamento LOCAL do Job {job.Id}.");
            using (var scope = HttpContext.RequestServices.CreateScope())
            {
                var reportProcessor = scope.ServiceProvider.GetRequiredService<IReportProcessorService>();
                var storedResult = JsonSerializer.Deserialize<PythonApiDto.PythonResultResponse>(job.ResultData);
                if(storedResult != null)
                {
                    await reportProcessor.ProcessAnalysisResult(job, storedResult);
                    return Ok(new { message = "Reprocessamento dos dados locais concluído." });
                }
                else 
                {
                    return StatusCode(500, "Falha ao ler os dados de resultado armazenados no banco.");
                }
            }
        }

        // CENÁRIO 2: O job está 'Failed' ou 'Pending'. Força uma sincronização.
        if (string.IsNullOrEmpty(job.PythonBatchId))
        {
            return BadRequest("Job não possui um BatchId para verificar na API de análise.");
        }
        
        logger.LogInformation($"Iniciando sincronização forçada do Job {job.Id} (Batch: {job.PythonBatchId}).");
        
        var pythonApiClient = _httpClientFactory.CreateClient("PythonAnalysisService");
        HttpResponseMessage pythonResponse;
        try 
        {
            pythonResponse = await pythonApiClient.GetAsync($"/analyze/results/{job.PythonBatchId}");
        }
        catch(HttpRequestException ex)
        {
            return StatusCode(503, $"Serviço de análise indisponível ao tentar sincronizar: {ex.Message}");
        }

        if (!pythonResponse.IsSuccessStatusCode)
        {
            job.ErrorMessage = "Tentativa de sincronização manual falhou. O checker tentará novamente.";
            await _context.SaveChangesAsync();
            return StatusCode((int)pythonResponse.StatusCode, "Falha ao consultar a API de análise. O job continuará na fila para o checker.");
        }

        var result = await pythonResponse.Content.ReadFromJsonAsync<PythonApiDto.PythonResultResponse>();
        
        if (result?.Status?.ToLower() == "completed")
        {
            logger.LogInformation($"Sincronização forçada detectou que o Job {job.Id} está concluído. Corrigindo estado local.");
            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.ResultData = JsonSerializer.Serialize(result);
            job.ErrorMessage = null; // Limpa erro antigo
            _context.Update(job);
            await _context.SaveChangesAsync();
            
            using (var scope = HttpContext.RequestServices.CreateScope())
            {
                var reportProcessor = scope.ServiceProvider.GetRequiredService<IReportProcessorService>();
                await reportProcessor.ProcessAnalysisResult(job, result);
            }

            return Ok(new { message = "Sincronização bem-sucedida. O job foi marcado como concluído e os dados foram processados." });
        }
        else
        {
            job.ErrorMessage = $"Status na API de análise: '{result?.Status}'. Nenhuma ação adicional realizada.";
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Sincronização concluída. Status na API de análise é '{result?.Status}'. O checker continuará monitorando se aplicável." });
        }
    }
}