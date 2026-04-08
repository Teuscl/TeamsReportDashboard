using FluentValidation;
using TeamsReportDashboard.Backend.Models.DepartmentDto;
using TeamsReportDashboard.Backend.Models.ReportDto;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Services.User.Create;

namespace TeamsReportDashboard.Backend.Services.Department.Create;

public class CreateDepartmentService : ICreateDepartmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateDepartmentDto> _validator;

    public CreateDepartmentService(IUnitOfWork unitOfWork, IValidator<CreateDepartmentDto> validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<CreateDepartmentDto> Execute(CreateDepartmentDto createDepartmentDto)
    {
        await Validate(createDepartmentDto);

        var dep = new Entities.Department()
        {
            Name = createDepartmentDto.Name,
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.DepartmentRepository.CreateDepartmentAsync(dep);
        await _unitOfWork.SaveChangesAsync();
        return createDepartmentDto;
    }
    
    private async Task Validate(CreateDepartmentDto createDepartmentDto)
    {
        var result = await _validator.ValidateAsync(createDepartmentDto);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(failure => failure.ErrorMessage).ToList();
            throw new ErrorOnValidationException(errors);
        }
    }
}