using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Entities;
using TeamsReportDashboard.Backend.Entities.Enums; // Substitua pelo seu namespace
using TeamsReportDashboard.Backend.Models;
using TeamsReportDashboard.Backend.Models.PythonApiDto;
using TeamsReportDashboard.Backend.Services.JobSynchronization;
using TeamsReportDashboard.Backend.Services.ProcessCompletedJob; // Substitua pelo seu namespace

[ApiController]
[Route("[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _context;
    private readonly IJobSynchronizationService _jobSyncService;

    public AnalysisController(IHttpClientFactory httpClientFactory, AppDbContext context, IJobSynchronizationService jobSyncService)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _jobSyncService = jobSyncService;
    }

    [HttpPost("start")]
    // Limite de 200 MB[HttpPost("start")]
    [RequestSizeLimit(200 * 1024 * 1024)]
    public async Task<IActionResult> StartAnalysis(
        [FromForm] IFormFile file, 
        [FromForm] string name)
    {
        
        if ( file == null || !file.FileName.ToLower().EndsWith(".zip"))
            return BadRequest(".zip file is not valid.");
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Job name is required.");
        var tempFilePath = Path.GetTempFileName();
        try
        {
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var pythonApiClient = _httpClientFactory.CreateClient("PythonAnalysisService");

            using var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read);
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", file.FileName);
            content.Add(new StringContent(name), "name");

            HttpResponseMessage pythonResponse;
            try
            {
                pythonResponse = await pythonApiClient.PostAsync("/analyze/start", content);

            }
            catch (HttpRequestException ex)
            {
                return StatusCode(503, $"Analysis service failed: {ex.Message}");
            }
            if (!pythonResponse.IsSuccessStatusCode)
                return StatusCode((int)pythonResponse.StatusCode,
                    $"Analysis service failed: {pythonResponse.StatusCode}");

            var startResponse = await pythonResponse.Content.ReadFromJsonAsync<PythonApiDto.PythonStartResponse>();
            if (string.IsNullOrEmpty(startResponse?.BatchId))
                return StatusCode(500, "Python API did not return a valid batch id.");

            var newJob = new AnalysisJob
            {
                Id = Guid.NewGuid(),
                Name = name,
                PythonBatchId = startResponse.BatchId,
                Status = JobStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            };
            _context.AnalysisJobs.Add(newJob);
            await _context.SaveChangesAsync();

            return Accepted(new { JobId = newJob.Id });

        }
        finally
        {
            // PASSO C: Garante que o arquivo temporário seja sempre deletado.
            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }
        }
        
    }

    [HttpGet("status/{jobId}")]
    public async Task<IActionResult> GetJobStatus(Guid jobId)
    {
        var job = await _context.AnalysisJobs.FindAsync(jobId);
        if (job == null) return NotFound();
        return Ok(new { job.Id, job.Name, Status = job.Status.ToString(), job.CreatedAt, job.CompletedAt, job.ErrorMessage });
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
                Name = j.Name,
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
        try
        {
            var result = await _jobSyncService.ReprocessJobAsync(jobId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            // Captura a exceção de concorrência!
            return Conflict(new { message = "Este job foi modificado por outra operação. Por favor, atualize e tente novamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(503, new { message = ex.Message }); // 503 Service Unavailable
        }
        catch (Exception ex) // Captura geral para erros inesperados
        {
            // Logar o erro aqui é importante
            return StatusCode(500, new { message = "Ocorreu um erro inesperado no servidor.", detail = ex.Message });
        }
    }
}