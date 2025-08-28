using System.Runtime.InteropServices.JavaScript;
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

       var requester = await _unitOfWork.RequesterRepository.GetByEmailAsync(createReportDto.RequesterEmail);
       if (requester == null)
       {
           requester = new Entities.Requester()
           {
               Name = createReportDto.RequesterName,
               Email = createReportDto.RequesterEmail,
               CreatedAt = DateTime.Now,
           };
           // Adicionamos ao repositório para ser salvo depois
           await _unitOfWork.RequesterRepository.CreateRequesterAsync(requester);
       }
       
       
       var report = new Entities.Report()
       {
           Requester = requester,
           ReportedProblem = createReportDto.ReportedProblem,
           Category = createReportDto.Category,
           TechnicianName = createReportDto.TechnicianName,
           AverageHandlingTime = createReportDto.AverageHandlingTime,
           CreatedAt = DateTime.Now,
           RequestDate = createReportDto.RequestDate,
           FirstResponseTime = createReportDto.FirstResponseTime
       };
       
       await _unitOfWork.ReportRepository.CreateReportAsync(report);
       await _unitOfWork.SaveChangesAsync();
       
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