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

    public StartAnalysisService(IUnitOfWork unitOfWork, IValidator<StartJobAnalysisDto> validator, IHttpClientFactory httpClientFactory)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _httpClientFactory = httpClientFactory;
    }
    public async Task<Guid> ExecuteAsync(StartJobAnalysisDto dto)
    {
        await Validate(dto);
        
        var tempFilePath = Path.GetTempFileName();
        try
        {
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }
            
            var pythonApiClient =  _httpClientFactory.CreateClient("PythonAnalysisService");
            using var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read);
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", dto.File.FileName);
            
            
            var pythonResponse = await pythonApiClient.PostAsync("/analyze/start", content);

            // DEBUG: Bloco para inspecionar a resposta antes de falhar
            if (!pythonResponse.IsSuccessStatusCode)
            {
                // Lê o corpo da resposta de erro vinda da API Python
                var errorBody = await pythonResponse.Content.ReadAsStringAsync();
    
                // Loga o erro detalhado no seu console ou sistema de log do C#
                // Se estiver rodando em modo Debug no Visual Studio, isso aparecerá no "Output" window.
                System.Diagnostics.Debug.WriteLine($"[HTTP DEBUG] Python API retornou erro: {pythonResponse.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[HTTP DEBUG] Corpo da Resposta: {errorBody}");

                // Você também pode usar seu ILogger aqui
                // _logger.LogError("Python API Error: {StatusCode} - {ResponseBody}", pythonResponse.StatusCode, errorBody);
            }

            // Esta linha vai agora lançar a mesma exceção de antes, mas já teremos o log detalhado.
            pythonResponse.EnsureSuccessStatusCode(); 

            
            var startResponse = await pythonResponse.Content.ReadFromJsonAsync<PythonApiDto.PythonStartResponse>();
            if(string.IsNullOrEmpty(startResponse?.BatchId))
                throw new InvalidOperationException("Python API didnt return a valid batch id");

            var newJob = new Entities.AnalysisJob
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                PythonBatchId = startResponse.BatchId,
                Status = JobStatus.Pending,
                CreatedAt = DateTime.Now,
            };
            
            await _unitOfWork.AnalysisJobRepository.AddAsync(newJob);
            await _unitOfWork.SaveChangesAsync();
            
            return newJob.Id;
        }
        finally
        {
            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }
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