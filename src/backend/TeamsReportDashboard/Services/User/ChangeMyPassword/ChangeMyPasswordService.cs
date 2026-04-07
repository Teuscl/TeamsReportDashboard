using FluentValidation;
using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Models.Dto;

namespace TeamsReportDashboard.Backend.Services.User.ChangeMyPassword;

public class ChangeMyPasswordService : IChangeMyPasswordService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IValidator<ChangeMyPasswordDto> _validator;

    public ChangeMyPasswordService(IUnitOfWork unitOfWork, IPasswordService passwordService, IValidator<ChangeMyPasswordDto> validator)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _validator = validator;
    }
    public async Task Execute(int userId, ChangeMyPasswordDto changeMyPasswordDto)
    {
        var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
        if (user == null)
            throw new ArgumentException("User not found");
        
        Validate(user, changeMyPasswordDto);
        
        user.Password = _passwordService.HashPassword(changeMyPasswordDto.NewPassword);
        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    private void Validate(TeamsReportDashboard.Entities.User user, ChangeMyPasswordDto changeMyPasswordDto)
    {
        var result = _validator.Validate(changeMyPasswordDto);
        
        if(!_passwordService.VerifyPassword(changeMyPasswordDto.OldPassword, user.Password))
            result.Errors.Add(new FluentValidation.Results.ValidationFailure("OldPassword", "Old password is incorrect."));
        
        if(!result.IsValid)
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
    }
}