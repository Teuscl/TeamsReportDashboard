using FluentValidation;
using TeamsReportDashboard.Backend.Models.Requester;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.Requester.Update;

public class UpdateRequesterService : IUpdateRequesterService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateRequesterDto> _validator;

    public UpdateRequesterService(IUnitOfWork unitOfWork, IValidator<UpdateRequesterDto> validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
    }
    public async Task Execute(int id, UpdateRequesterDto dto)
    {
        var requester = await _unitOfWork.RequesterRepository.GetRequesterAsync(id);
        if (requester == null)
        {
            throw new KeyNotFoundException("Solicitante não encontrado.");
        }

        requester.Name = dto.Name;
        requester.Email = dto.Email;
        requester.DepartmentId = dto.DepartmentId;
        requester.UpdatedAt = DateTime.Now;
        
        _unitOfWork.RequesterRepository.UpdateRequester(requester);
        await _unitOfWork.CommitAsync();
    }
    
    private async Task Validate(UpdateRequesterDto updateReportDto)
    {
        var result = await _validator.ValidateAsync(updateReportDto);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(failure => failure.ErrorMessage).ToList();

            throw new ErrorOnValidationException(errors);
        }
    }
}