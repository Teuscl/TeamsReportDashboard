using FluentValidation;
using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;

namespace TeamsReportDashboard.Backend.Services.User.Update;

public class UpdateUserService : IUpdateUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateUserDto> _validator;


    public UpdateUserService(IUnitOfWork unitOfWork, IValidator<UpdateUserDto> validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
    }


    public async Task Execute(UpdateUserDto updateUserDto)
    {
        await Validate(updateUserDto);
        
        var user = await _unitOfWork.UserRepository.GetByIdAsync(updateUserDto.Id);
        
        user.Name = updateUserDto.Name;
        user.Email = updateUserDto.Email;
        user.Role = updateUserDto.Role;
        user.UpdatedAt = DateTime.Now;
        user.IsActive = updateUserDto.IsActive;
        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

    }

    private async Task Validate(UpdateUserDto updateUserDto)
    {
        var user = await _unitOfWork.UserRepository.GetByIdAsync(updateUserDto.Id);
        if(user == null)
             throw new ErrorOnValidationException(new List<string>{"User not found"});
        var result = await _validator.ValidateAsync(updateUserDto);

        if (updateUserDto.Email != user.Email)
        {
            var emailAlreadyExists = await _unitOfWork.UserRepository.ExistsWithEmailAsync(updateUserDto.Email, updateUserDto.Id);
            if (emailAlreadyExists)
                result.Errors.Add(new FluentValidation.Results.ValidationFailure("Email", "Email already exists."));
        }
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(error => error.ErrorMessage).ToList();
            throw new ErrorOnValidationException(errors);
        }
        
    }
}