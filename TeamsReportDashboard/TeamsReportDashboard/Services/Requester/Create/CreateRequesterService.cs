using FluentValidation;
using TeamsReportDashboard.Backend.Models.Requester;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Requester.Create;

public class CreateRequesterService : ICreateRequesterService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateRequesterDto> _validator;

    public CreateRequesterService(IUnitOfWork unitOfWork, IValidator<CreateRequesterDto> validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
    }


    public async Task<CreateRequesterDto> Execute(CreateRequesterDto dto)
    {
        await Validate(dto);
        
        var existingRequester = await _unitOfWork.RequesterRepository.GetByEmailAsync(dto.Email);
        if (existingRequester != null)
        {
            throw new ErrorOnValidationException(new List<string>(){"Email already exists"});
        }

        var requester = new Entities.Requester()
        {
            Name = dto.Name,
            Email = dto.Email,
            DepartmentId = dto.DepartmentId,
            CreatedAt = DateTime.Now
        };
        
        await _unitOfWork.RequesterRepository.CreateRequesterAsync(requester);
        await _unitOfWork.CommitAsync();

        return dto;

    }
    
    private async Task Validate(CreateRequesterDto createRequesterDto)
    {
        var result = await _validator.ValidateAsync(createRequesterDto);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(failure => failure.ErrorMessage).ToList();

            throw new ErrorOnValidationException(errors);
        }
    }
}