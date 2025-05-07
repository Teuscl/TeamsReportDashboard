using TeamsReportDashboard.Exceptions;
using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Models.Dto;

namespace TeamsReportDashboard.Services.User.ChangePassword;
 
public class ChangePasswordService : IChangePasswordService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    
    public ChangePasswordService(IUnitOfWork unitOfWork, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
    }
    public async Task Execute(int userId, ChangePasswordDto changePasswordDto)
    {
        var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
        if (user == null)
            throw new ArgumentException("User not found");
        
        Validate(user, changePasswordDto);
        
        user.Password = _passwordService.HashPassword(changePasswordDto.NewPassword);
        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.CommitAsync();
    }

    private void Validate(Entities.User user, ChangePasswordDto changePasswordDto)
    {
        var result = new ChangePasswordValidator().Validate(changePasswordDto);
        
        if(!_passwordService.VerifyPassword(changePasswordDto.OldPassword, user.Password))
            result.Errors.Add(new FluentValidation.Results.ValidationFailure("OldPassword", "Old password is incorrect."));
        
        if(!result.IsValid)
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
    }
}