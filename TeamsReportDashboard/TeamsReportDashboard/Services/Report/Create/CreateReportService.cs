using FluentValidation;
using TeamsReportDashboard.Backend.Models.ReportDto;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Models.Dto;
using TeamsReportDashboard.Services.User.Create;

namespace TeamsReportDashboard.Backend.Services.Report.Create;

public class CreateReportService : ICreateReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateReportDto> _validator;


    public CreateReportService(IUnitOfWork unitOfWork, IValidator<CreateReportDto> validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<CreateReportDto> Execute(CreateReportDto createReportDto)
    {
       await Validate(createReportDto);
       
       var report = new Entities.Report()
       {
           RequesterId = createReportDto.RequesterId,
           ReportedProblem = createReportDto.ReportedProblem,
           Category = createReportDto.Category,
           TechnicianName = createReportDto.TechnicianName,
           AverageHandlingTime = createReportDto.AverageHandlingTime,
           CreatedAt = DateTime.Now,
           RequestDate = createReportDto.RequestDate,
           FirstResponseTime = createReportDto.FirstResponseTime
       };
       
       await _unitOfWork.ReportRepository.CreateReportAsync(report);
       await _unitOfWork.CommitAsync();
       
       return createReportDto;

    }

    private async Task Validate(CreateReportDto createReportDto)
    {
        var result = await _validator.ValidateAsync(createReportDto);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(failure => failure.ErrorMessage).ToList();

            throw new ErrorOnValidationException(errors);
        }
    }
    
    
    
    
}