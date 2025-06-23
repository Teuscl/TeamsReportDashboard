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
    [Authorize(Roles = "Admin, Master")] // Proteja este endpoint de depuração
    public async Task<IActionResult> ReprocessJob(Guid jobId)
    {
        var job = await _context.AnalysisJobs.FindAsync(jobId);
        if (job == null)
        {
            return NotFound("Job não encontrado.");
        }
        if (job.Status != JobStatus.Completed || string.IsNullOrEmpty(job.ResultData))
        {
            return BadRequest("Este job não está concluído ou não contém dados para reprocessar.");
        }

        // Usamos um service scope para injetar o serviço de processamento
        using (var scope = HttpContext.RequestServices.CreateScope())
        {
            var reportProcessor = scope.ServiceProvider.GetRequiredService<IReportProcessorService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AnalysisController>>();

            logger.LogInformation($"==== INICIANDO REPROCESSAMENTO MANUAL DO JOB {jobId} ====");

            var storedResult = JsonSerializer.Deserialize<PythonApiDto.PythonResultResponse>(job.ResultData);
            if (storedResult == null)
            {
                return StatusCode(500, "Falha ao deserializar os dados do job armazenado.");
            }

            // Chama a mesma lógica de processamento, mas com os dados já salvos!
            await reportProcessor.ProcessAnalysisResult(job, storedResult);

            logger.LogInformation($"==== REPROCESSAMENTO MANUAL DO JOB {jobId} CONCLUÍDO ====");
        }

        return Ok("Reprocessamento concluído. Verifique os logs para detalhes.");
    }
}