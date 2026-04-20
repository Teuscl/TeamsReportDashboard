using FluentValidation;
using TeamsReportDashboard.Backend.Entities.Enums;
using TeamsReportDashboard.Backend.Models.Job;
using TeamsReportDashboard.Backend.Models.PythonApiDto;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.AnalysisJob.Start;

public class StartAnalysisService : IStartAnalysisService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<StartJobAnalysisDto> _validator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StartAnalysisService> _logger;

    public StartAnalysisService(
        IUnitOfWork unitOfWork,
        IValidator<StartJobAnalysisDto> validator,
        IHttpClientFactory httpClientFactory,
        ILogger<StartAnalysisService> logger)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }
    public async Task<Guid> ExecuteAsync(StartJobAnalysisDto dto, Guid userId)
    {
        await Validate(dto);

        var activePrompt = await _unitOfWork.SystemPromptRepository.GetLatestAsync()
            ?? throw new InvalidOperationException(
                "Nenhum prompt de IA configurado. Configure o prompt antes de iniciar uma análise.");

        var tempFilePath = Path.GetTempFileName();
        try
        {
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
                await dto.File.CopyToAsync(stream);

            var pythonApiClient = _httpClientFactory.CreateClient("PythonAnalysisService");
            using var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read);
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", dto.File.FileName);
            content.Add(new StringContent(activePrompt.Content), "prompt");

            var pythonResponse = await pythonApiClient.PostAsync("/analyze/start", content);

            if (!pythonResponse.IsSuccessStatusCode)
            {
                var errorBody = await pythonResponse.Content.ReadAsStringAsync();
                _logger.LogError("Python API retornou erro {StatusCode}: {ResponseBody}",
                    pythonResponse.StatusCode, errorBody);
            }

            pythonResponse.EnsureSuccessStatusCode();

            var startResponse = await pythonResponse.Content.ReadFromJsonAsync<PythonApiDto.PythonStartResponse>();
            if (string.IsNullOrEmpty(startResponse?.BatchId))
                throw new InvalidOperationException("Python API didn't return a valid batch id.");

            var newJob = new Entities.AnalysisJob
            {
                Name = dto.Name,
                PythonBatchId = startResponse.BatchId,
                Status = JobStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                SystemPromptId = activePrompt.Id,
            };

            await _unitOfWork.AnalysisJobRepository.AddAsync(newJob);
            await _unitOfWork.SaveChangesAsync();

            return newJob.Id;
        }
        finally
        {
            if (System.IO.File.Exists(tempFilePath))
                System.IO.File.Delete(tempFilePath);
        }
    }

    private async Task Validate(StartJobAnalysisDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            throw new ErrorOnValidationException(errors);
        }
    }
}