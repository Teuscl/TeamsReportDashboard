using System.Text.Json;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Entities.Enums;
using TeamsReportDashboard.Backend.Models.PythonApiDto;
using TeamsReportDashboard.Backend.Models.ReprocessResponseDto;
using TeamsReportDashboard.Backend.Services.ProcessCompletedJob;

namespace TeamsReportDashboard.Backend.Services.JobSynchronization;

public class JobSynchronizationService : IJobSynchronizationService
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IReportProcessorService _reportProcessor;
    private readonly ILogger<JobSynchronizationService> _logger;
    
    public JobSynchronizationService(
        AppDbContext context, 
        IHttpClientFactory httpClientFactory, 
        IReportProcessorService reportProcessor, 
        ILogger<JobSynchronizationService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _reportProcessor = reportProcessor;
        _logger = logger;
    }
    public async Task<ReprocessResponseDto> ReprocessJobAsync(Guid jobId)
    {
        var job = await _context.AnalysisJobs.FindAsync(jobId);
        if (job == null)
        {
            throw new Exception("Job not found");
        }

        if (job.Status == JobStatus.Completed && !string.IsNullOrEmpty(job.ResultData))
        {
            _logger.LogInformation($"Starting local reprocess job {job.Id}");
            var storedResult = JsonSerializer.Deserialize<PythonApiDto.PythonResultResponse>(job.ResultData);
            if (storedResult != null)
            {
                await _reportProcessor.ProcessAnalysisResult(job,storedResult);
                return new ReprocessResponseDto {  Message = "Processing job completed"};
                
            }
            else
            {
                throw new InvalidOperationException($"Error while reading job results");
            }
        }

        if (string.IsNullOrEmpty(job.PythonBatchId))
        {
            throw new InvalidOperationException($"Job doesn't have a python batch id");
        }
        
        _logger.LogInformation($"Starting forced local reprocess job {job.Id}");
        var pythonApiClient = _httpClientFactory.CreateClient("PythonAnalysisService");
        var pythonResponse = await pythonApiClient.GetAsync($"/analyze/results/{job.PythonBatchId}");

        if (!pythonResponse.IsSuccessStatusCode)
        {
            job.ErrorMessage = "Manual sync error. Checker will be try again";
            await _context.SaveChangesAsync();
            // Lança uma exceção para o controller traduzir para um status code de erro
            throw new HttpRequestException($"Error consuming Analysis API. Status: {pythonResponse.StatusCode}");
        }

        var result = await pythonResponse.Content.ReadFromJsonAsync<PythonApiDto.PythonResultResponse>();
        
        if (result?.Status?.ToLower() == "completed")
        {
            _logger.LogInformation($"Forced sync detected that {job.Id} is completed. Changing status to {JobStatus.Completed}.");
            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.ResultData = JsonSerializer.Serialize(result);
            job.ErrorMessage = null;
            
            await _context.SaveChangesAsync(); // Protegido pela RowVersion!
            
            await _reportProcessor.ProcessAnalysisResult(job, result);
            return new ReprocessResponseDto { Message = "Sync successes. Job was tagged as completed and data was computed" };
        }

        job.ErrorMessage = $"API Status: '{result?.Status}'. No action was made.";
        await _context.SaveChangesAsync(); // Protegido pela RowVersion!
        return new ReprocessResponseDto { Message = $"Sync completed. API Status '{result?.Status}'." };

    }
}